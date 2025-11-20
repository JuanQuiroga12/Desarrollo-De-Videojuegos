using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Sistema de visualización de movimiento para PC y móvil.
/// PC: Hover muestra tiles verdes/rojas, click para mover
/// Móvil: Primer tap selecciona, segundo tap confirma/cancela
/// </summary>
public class PlayerMovementVisualizer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera mainCamera;

    [Header("Mobile Settings")]
    [SerializeField] private float doubleTapDelay = 0.5f;
    [SerializeField] private float doubleTapRadius = 30f; // píxeles

    [Header("Colors")]
    [SerializeField] private Color reachableColor = new Color(0f, 1f, 0f, 0.8f); // Verde
    [SerializeField] private Color unreachableColor = new Color(1f, 0f, 0f, 0.8f); // Rojo
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 0f, 0.9f); // Amarillo

    // Estado
    private Vector2Int? hoveredTile = null;
    private Vector2Int? selectedTile = null;
    private float lastTapTime = 0f;
    private Vector2 lastTapPosition = Vector2.zero;
    private bool isMobile = false;

    void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Detectar plataforma
        isMobile = Application.isMobilePlatform;

        Debug.Log($"[MovementVisualizer] Plataforma detectada: {(isMobile ? "MÓVIL" : "PC")}");
    }

    void Update()
    {
        // Solo procesar si es el turno del jugador
        if (playerController == null || !playerController.GetPlayerData().isMyTurn)
        {
            ClearVisualization();
            return;
        }

        if (isMobile)
        {
            HandleMobileInput();
        }
        else
        {
            HandlePCInput();
        }
    }

    /// <summary>
    /// Manejo de input para PC (hover + click)
    /// </summary>
    void HandlePCInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Verificar que no estamos sobre UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearVisualization();
            return;
        }

        // Obtener tile bajo el cursor
        Vector2Int? tile = GetTileUnderPointer(mouse.position.ReadValue());

        if (tile.HasValue)
        {
            // Si cambió el tile, actualizar visualización
            if (hoveredTile != tile.Value)
            {
                hoveredTile = tile.Value;
                ShowPathPreview(tile.Value);
            }

            // Click para mover
            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryMoveToTile(tile.Value);
            }
        }
        else
        {
            ClearVisualization();
            hoveredTile = null;
        }
    }

    /// <summary>
    /// Manejo de input para móvil (tap para seleccionar, segundo tap para confirmar)
    /// </summary>
    void HandleMobileInput()
    {
        // Usar el Input System para touch
        var touchScreen = Touchscreen.current;
        if (touchScreen == null) return;

        // Detectar tap (primaryTouch.press.wasPressedThisFrame)
        if (touchScreen.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPos = touchScreen.primaryTouch.position.ReadValue();

            // Verificar que no estamos sobre UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2Int? tile = GetTileUnderPointer(touchPos);

            if (tile.HasValue)
            {
                float timeSinceLastTap = Time.time - lastTapTime;
                float distanceFromLastTap = Vector2.Distance(touchPos, lastTapPosition);

                // Detectar doble tap en la misma zona
                if (timeSinceLastTap < doubleTapDelay && distanceFromLastTap < doubleTapRadius)
                {
                    // Segundo tap: confirmar movimiento
                    if (selectedTile.HasValue && selectedTile.Value == tile.Value)
                    {
                        TryMoveToTile(tile.Value);
                        selectedTile = null;
                    }
                }
                else
                {
                    // Primer tap: seleccionar tile
                    selectedTile = tile.Value;
                    ShowPathPreview(tile.Value);

                    lastTapTime = Time.time;
                    lastTapPosition = touchPos;

                    Debug.Log($"[MovementVisualizer] Tile seleccionado: {tile.Value}. Tap de nuevo para confirmar.");
                }
            }
        }
    }

    /// <summary>
    /// Obtiene el tile del grid bajo el puntero
    /// </summary>
    Vector2Int? GetTileUnderPointer(Vector2 screenPosition)
    {
        if (mainCamera == null || MapGenerator.Instance == null)
            return null;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Raycast para detectar el grid
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hit.point);

            // Verificar que el tile sea válido
            if (MapGenerator.Instance.IsWalkable(gridPos.x, gridPos.y))
            {
                return gridPos;
            }
        }

        return null;
    }

    /// <summary>
    /// Muestra preview del path al tile objetivo
    /// </summary>
    void ShowPathPreview(Vector2Int targetTile)
    {
        if (PathfindingSystem.Instance == null || GridVisualizer.Instance == null)
            return;

        Vector2Int currentPos = playerController.GetPlayerData().gridPosition;
        int availablePM = playerController.GetPlayerData().currentMovementPoints;

        // Obtener path
        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(currentPos, targetTile);

        if (path == null || path.Count == 0)
        {
            GridVisualizer.Instance.ResetGridColors();
            return;
        }

        // Limpiar colores previos
        GridVisualizer.Instance.ResetGridColors();

        // Pintar path
        for (int i = 0; i < path.Count; i++)
        {
            Color tileColor;

            if (i < availablePM)
            {
                // Tiles alcanzables: verde
                tileColor = reachableColor;
            }
            else
            {
                // Tiles fuera de rango: rojo
                tileColor = unreachableColor;
            }

            GridVisualizer.Instance.SetTileColor(path[i], tileColor);
        }

        // Si hay un tile seleccionado (móvil), resaltarlo
        if (selectedTile.HasValue)
        {
            GridVisualizer.Instance.SetTileColor(selectedTile.Value, selectedColor);
        }
    }

    /// <summary>
    /// Intenta mover al jugador al tile especificado
    /// </summary>
    void TryMoveToTile(Vector2Int targetTile)
    {
        if (PathfindingSystem.Instance == null)
            return;

        Vector2Int currentPos = playerController.GetPlayerData().gridPosition;
        int availablePM = playerController.GetPlayerData().currentMovementPoints;

        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(currentPos, targetTile);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[MovementVisualizer] No hay path a {targetTile}");
            return;
        }

        // Verificar si el tile es alcanzable
        if (path.Count <= availablePM)
        {
            // ✅ VERDE: Mover
            Debug.Log($"[MovementVisualizer] ✅ Moviendo a {targetTile} (costo: {path.Count} PM)");
            playerController.TryMoveTo(targetTile);
            ClearVisualization();
        }
        else
        {
            // ❌ ROJO: No alcanzable
            Debug.LogWarning($"[MovementVisualizer] ❌ Tile {targetTile} fuera de rango (costo: {path.Count}, disponible: {availablePM})");

            // Feedback visual: parpadear el tile rojo
            if (GridVisualizer.Instance != null)
            {
                StartCoroutine(BlinkTile(targetTile));
            }
        }
    }

    /// <summary>
    /// Efecto de parpadeo en tile no alcanzable
    /// </summary>
    System.Collections.IEnumerator BlinkTile(Vector2Int tile)
    {
        for (int i = 0; i < 3; i++)
        {
            GridVisualizer.Instance.SetTileColor(tile, Color.white);
            yield return new WaitForSeconds(0.1f);
            GridVisualizer.Instance.SetTileColor(tile, unreachableColor);
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Limpia toda la visualización
    /// </summary>
    void ClearVisualization()
    {
        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }

        hoveredTile = null;
        selectedTile = null;
    }

    void OnDisable()
    {
        ClearVisualization();
    }
}