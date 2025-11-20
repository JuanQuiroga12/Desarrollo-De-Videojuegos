using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using System.Collections;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerMovementSystem : MonoBehaviour
{
    [Header("Referencias")]
    private PlayerController playerController;
    private Camera mainCamera;

    [Header("Estado de Selección")]
    private Vector2Int? selectedTile = null;
    private List<Vector2Int> currentPath = null;
    private bool isWaitingForConfirmation = false;

    [Header("Colores de Visualización")]
    [SerializeField] private Color reachableColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private Color unreachableColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 0f, 0.9f);

    [Header("Configuración Touch")]
    [SerializeField] private float doubleTapTime = 0.5f;
    private float lastTapTime = 0f;
    private Vector2Int lastTappedTile = Vector2Int.zero;

    void Awake()
    {
        // Habilitar el sistema de touch mejorado
        EnhancedTouchSupport.Enable();
    }

    void OnDestroy()
    {
        // Deshabilitar cuando se destruye
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        mainCamera = Camera.main;

        if (playerController == null)
        {
            Debug.LogError($"[PlayerMovementSystem] No se encontró PlayerController en {gameObject.name}");
        }
    }

    void Update()
    {
        // Solo procesar input si es nuestro jugador local y es su turno
        if (!playerController.GetPlayerData().isMyTurn)
        {
            ClearVisualization();
            return;
        }

        if (!playerController.isLocalPlayer)
        {
            return;
        }

        // Detectar input táctil o mouse
        HandleInput();
    }

    void HandleInput()
    {
        // Verificar si estamos sobre UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Para touch en móvil, verificar si estamos sobre UI
        if (Touch.activeTouches.Count > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject(Touch.activeTouches[0].touchId))
            {
                return;
            }
        }

        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        // Detectar input táctil usando el nuevo Input System
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                inputDetected = true;
                inputPosition = touch.screenPosition;
            }
        }
        // Detectar click del mouse usando el nuevo Input System
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Mouse.current.position.ReadValue();
        }

        if (inputDetected)
        {
            ProcessTap(inputPosition);
        }
    }

    void ProcessTap(Vector2 screenPosition)
    {
        // Convertir posición de pantalla a tile del grid
        Vector2Int? tappedTile = GetTileFromScreenPosition(screenPosition);

        if (!tappedTile.HasValue)
        {
            ClearVisualization();
            return;
        }

        Vector2Int tile = tappedTile.Value;

        // Verificar si es el mismo tile que el tap anterior (doble tap)
        if (tile == lastTappedTile && Time.time - lastTapTime < doubleTapTime)
        {
            // Doble tap detectado - confirmar movimiento
            if (isWaitingForConfirmation && currentPath != null)
            {
                ConfirmMovement(tile);
            }
        }
        else
        {
            // Primer tap - mostrar preview del camino
            ShowPathPreview(tile);
            lastTappedTile = tile;
            lastTapTime = Time.time;
        }
    }

    Vector2Int? GetTileFromScreenPosition(Vector2 screenPos)
    {
        if (mainCamera == null || MapGenerator.Instance == null)
            return null;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hit.point);

            if (MapGenerator.Instance.IsWalkable(gridPos.x, gridPos.y))
            {
                return gridPos;
            }
        }

        return null;
    }

    void ShowPathPreview(Vector2Int targetTile)
    {
        if (PathfindingSystem.Instance == null || GridVisualizer.Instance == null)
            return;

        // Limpiar visualización anterior
        ClearVisualization();

        Vector2Int currentPos = playerController.GetPlayerData().gridPosition;
        int availableMP = playerController.GetPlayerData().currentMovementPoints;

        // Calcular camino
        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(currentPos, targetTile);

        if (path == null || path.Count == 0)
        {
            Debug.Log($"[PlayerMovementSystem] No hay camino válido a {targetTile}");
            return;
        }

        currentPath = path;
        selectedTile = targetTile;
        isWaitingForConfirmation = true;

        // Visualizar el camino
        for (int i = 0; i < path.Count; i++)
        {
            Color tileColor;

            if (i < availableMP)
            {
                // Tiles alcanzables con PM actuales - VERDE
                tileColor = reachableColor;
            }
            else
            {
                // Tiles que requieren más PM - ROJO
                tileColor = unreachableColor;
            }

            GridVisualizer.Instance.SetTileColor(path[i], tileColor);
        }

        // Resaltar el tile objetivo
        GridVisualizer.Instance.SetTileColor(targetTile, selectedColor);

        Debug.Log($"[PlayerMovementSystem] Path preview: {path.Count} tiles, MP disponible: {availableMP}");
    }

    void ConfirmMovement(Vector2Int targetTile)
    {
        if (currentPath == null || !isWaitingForConfirmation)
            return;

        int availableMP = playerController.GetPlayerData().currentMovementPoints;

        // Solo mover si tenemos suficientes PM
        if (currentPath.Count <= availableMP)
        {
            Debug.Log($"[PlayerMovementSystem] ✅ Confirmando movimiento a {targetTile}");
            playerController.TryMoveTo(targetTile);

            // Si es juego online, sincronizar
            if (NetworkManager.Instance != null && NetworkManager.Instance.IsOnlineMode())
            {
                StartCoroutine(SyncMovement(targetTile));
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerMovementSystem] ❌ No hay suficientes PM. Necesario: {currentPath.Count}, Disponible: {availableMP}");

            // Efecto visual de rechazo
            StartCoroutine(ShowRejectionEffect());
        }

        ClearVisualization();
    }

    IEnumerator ShowRejectionEffect()
    {
        // Parpadeo rojo para indicar que no se puede mover
        for (int i = 0; i < 3; i++)
        {
            if (selectedTile.HasValue)
            {
                GridVisualizer.Instance.SetTileColor(selectedTile.Value, Color.white);
                yield return new WaitForSeconds(0.1f);
                GridVisualizer.Instance.SetTileColor(selectedTile.Value, unreachableColor);
                yield return new WaitForSeconds(0.1f);
            }
        }

        ClearVisualization();
    }

    IEnumerator SyncMovement(Vector2Int targetPos)
    {
        // Esperar un frame para asegurar que el movimiento local se procese
        yield return null;

        // Sincronizar con Firebase
        if (GameManager.Instance != null)
        {
            var gameState = GameManager.Instance.GetGameState();
            if (NetworkManager.Instance != null && NetworkManager.Instance.currentRoomRef != null)
            {
                _ = NetworkManager.Instance.SendGameState(gameState);
            }
        }
    }

    void ClearVisualization()
    {
        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }

        selectedTile = null;
        currentPath = null;
        isWaitingForConfirmation = false;
    }

    void OnDisable()
    {
        ClearVisualization();
    }
}