using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerMovementSystem : MonoBehaviour
{
    [Header("Colores de Visualización")]
    [SerializeField] private Color reachableColor = Color.green;
    [SerializeField] private Color unreachableColor = Color.red;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Configuración Touch")]
    [SerializeField] private float doubleTapTime = 0.5f;

    private PlayerController playerController;
    private Vector2Int? selectedTarget = null;
    private List<Vector2Int> currentPath = null;
    private float lastTapTime = 0f;
    private Vector2Int lastTappedTile = Vector2Int.zero;

    // ✅ NUEVO: Flag para verificar si el sistema está listo
    private bool isSystemReady = false;

    void OnEnable()
    {
        Debug.Log("[PlayerMovementSystem] 🟢 OnEnable - Habilitando Enhanced Touch");
        TouchSimulation.Enable();
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        Debug.Log("[PlayerMovementSystem] 🔴 OnDisable - Deshabilitando Enhanced Touch");
        TouchSimulation.Disable();
        EnhancedTouchSupport.Disable();

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }

        selectedTarget = null;
        currentPath = null;
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("[PlayerMovementSystem] ❌ No se encontró PlayerController");
            enabled = false;
            return;
        }

        Debug.Log($"[PlayerMovementSystem] ✅ Inicializado para {gameObject.name}");

        // ✅ Esperar a que el juego esté inicializado
        StartCoroutine(WaitForGameInitialization());
    }

    // ✅ NUEVO: Esperar inicialización completa
    private IEnumerator WaitForGameInitialization()
    {
        Debug.Log($"[PlayerMovementSystem] ⏳ Esperando inicialización del juego...");

        // Esperar a que GameInitializationManager exista
        while (GameInitializationManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Esperar a que todo esté listo
        while (!GameInitializationManager.Instance.IsInitialized())
        {
            yield return new WaitForSeconds(0.1f);
        }

        isSystemReady = true;
        Debug.Log($"[PlayerMovementSystem] ✅ Sistema listo para {gameObject.name}");
    }

    void Update()
    {
        // ✅ NUEVO: No procesar input si el sistema no está listo
        if (!isSystemReady)
        {
            return;
        }

        PlayerData playerData = playerController.GetPlayerData();

        if (playerData == null)
        {
            return;
        }

        // ✅ LOGS DE DEBUGGING (comentar en producción)
        if (Time.frameCount % 300 == 0) // Log cada 5 segundos
        {
            Debug.Log($"[PlayerMovementSystem] Estado de {gameObject.name}:");
            Debug.Log($"    - isMyTurn: {playerData.isMyTurn}");
            Debug.Log($"    - isLocalPlayer: {playerController.isLocalPlayer}");
            Debug.Log($"    - enabled: {enabled}");
            Debug.Log($"    - isSystemReady: {isSystemReady}");
        }

        // ✅ VERIFICACIÓN MEJORADA: Confirmar que es el turno del jugador
        if (!playerData.isMyTurn)
        {
            return; // ← Salir silenciosamente si no es su turno
        }

        if (!playerController.isLocalPlayer)
        {
            return; // ← Salir silenciosamente si no es jugador local
        }

        SpellCastingSystem spellSystem = GetComponent<SpellCastingSystem>();
        if (spellSystem != null && spellSystem.IsSelectingTarget())
        {
            return;
        }

        HandleMovementInput();
    }

    void HandleMovementInput()
    {
        Vector2Int? tappedTile = null;
        Vector2 inputPosition = Vector2.zero;

        // 📱 TOUCH INPUT
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                inputPosition = touch.screenPosition;

                if (IsPointerOverUI(inputPosition))
                {
                    Debug.Log("[PlayerMovementSystem] 🛑 Toque sobre UI - ignorando");
                    return;
                }

                Debug.Log($"[PlayerMovementSystem] 📱 Toque FUERA de UI en: {inputPosition}");
                tappedTile = GetTileFromScreenPosition(inputPosition);
            }
        }
        // 🖱️ MOUSE INPUT
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputPosition = Mouse.current.position.ReadValue();

            if (IsPointerOverUI(inputPosition))
            {
                Debug.Log("[PlayerMovementSystem] 🛑 Click sobre UI - ignorando");
                return;
            }

            Debug.Log($"[PlayerMovementSystem] 🖱️ Click FUERA de UI en: {inputPosition}");
            tappedTile = GetTileFromScreenPosition(inputPosition);
        }

        if (tappedTile.HasValue)
        {
            Debug.Log($"[PlayerMovementSystem] 🎯 Procesando tap en: {tappedTile.Value}");
            ProcessTileTap(tappedTile.Value);
        }
    }

    bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[PlayerMovementSystem] ⚠️ EventSystem.current es NULL");
            return false;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            Debug.Log($"[PlayerMovementSystem] 📍 UI detectada: {results[0].gameObject.name}");
            return true;
        }

        return false;
    }

    Vector2Int? GetTileFromScreenPosition(Vector2 screenPos)
    {
        if (Camera.main == null)
        {
            Debug.LogError("[PlayerMovementSystem] ❌ Camera.main es null");
            return null;
        }

        if (Camera.main.orthographic)
        {
            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, 0.55f, 0));
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (gridPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hitPoint);

                    if (MapGenerator.Instance.IsWalkable(gridPos.x, gridPos.y))
                    {
                        Debug.Log($"[PlayerMovementSystem] ✅ Grid Position: {gridPos}");
                        return gridPos;
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerMovementSystem] ⚠️ Casilla {gridPos} no es caminable");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[PlayerMovementSystem] ⚠️ Rayo no intersecta el plano del grid");
            }
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hit.point);
                    Debug.Log($"[PlayerMovementSystem] ✅ Grid Position: {gridPos}");
                    return gridPos;
                }
            }
        }

        return null;
    }

    void ProcessTileTap(Vector2Int tappedTile)
    {
        PlayerData playerData = playerController.GetPlayerData();

        Vector2Int currentPos = playerData.gridPosition;
        bool isDoubleTap = (tappedTile == lastTappedTile) && (Time.time - lastTapTime < doubleTapTime);

        lastTappedTile = tappedTile;
        lastTapTime = Time.time;

        if (tappedTile == currentPos)
        {
            Debug.Log("[PlayerMovementSystem] ⚠️ Tap en posición actual - Ignorando");
            return;
        }

        if (!MapGenerator.Instance.IsWalkable(tappedTile.x, tappedTile.y))
        {
            Debug.Log($"[PlayerMovementSystem] ❌ Casilla {tappedTile} no es caminable");
            return;
        }

        if (!isDoubleTap || selectedTarget != tappedTile)
        {
            Debug.Log($"[PlayerMovementSystem] 📍 Visualizando camino a {tappedTile}");
            VisualizePathTo(tappedTile);
            selectedTarget = tappedTile;
        }
        else
        {
            Debug.Log($"[PlayerMovementSystem] ✅ DoubleTap confirmado - Iniciando movimiento");
            ConfirmMovement();
        }
    }

    void VisualizePathTo(Vector2Int target)
    {
        PlayerData playerData = playerController.GetPlayerData();
        Vector2Int currentPos = playerData.gridPosition;

        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(currentPos, target);

        if (path == null || path.Count == 0)
        {
            Debug.Log($"[PlayerMovementSystem] ❌ No se encontró camino a {target}");
            GridVisualizer.Instance.ResetGridColors();
            currentPath = null;
            return;
        }

        currentPath = path;
        int availablePM = playerData.currentMovementPoints;

        GridVisualizer.Instance.ResetGridColors();

        for (int i = 0; i < path.Count; i++)
        {
            if (i < availablePM)
            {
                GridVisualizer.Instance.SetTileColor(path[i], reachableColor);
            }
            else
            {
                GridVisualizer.Instance.SetTileColor(path[i], unreachableColor);
            }
        }

        GridVisualizer.Instance.SetTileColor(target, selectedColor);
    }

    void ConfirmMovement()
    {
        PlayerData playerData = playerController.GetPlayerData();

        if (!selectedTarget.HasValue || currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning("[PlayerMovementSystem] ⚠️ No hay objetivo seleccionado");
            return;
        }

        int requiredPM = currentPath.Count;
        int availablePM = playerData.currentMovementPoints;

        if (requiredPM > availablePM)
        {
            Debug.Log($"[PlayerMovementSystem] ⚠️ PM insuficientes. Moviendo hasta donde sea posible.");

            if (availablePM > 0)
            {
                currentPath = currentPath.GetRange(0, Mathf.Min(availablePM, currentPath.Count));
                selectedTarget = currentPath[currentPath.Count - 1];
            }
            else
            {
                GridVisualizer.Instance.ResetGridColors();
                selectedTarget = null;
                currentPath = null;
                return;
            }
        }

        Debug.Log($"[PlayerMovementSystem] ✅ Iniciando movimiento");
        StartCoroutine(MoveAlongPath(currentPath));

        GridVisualizer.Instance.ResetGridColors();
        selectedTarget = null;
        currentPath = null;
    }

    IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0) yield break;

        enabled = false;
        float moveSpeed = 3f;

        foreach (Vector2Int gridPos in path)
        {
            PlayerData playerData = playerController.GetPlayerData();

            if (playerData.currentMovementPoints <= 0)
            {
                Debug.Log("[PlayerMovementSystem] ⚠️ PM agotados");
                break;
            }

            playerData.gridPosition = gridPos;
            playerData.Move(1);

            Vector3 targetWorldPos = MapGenerator.Instance.GetWorldPosition(gridPos.x, gridPos.y);
            targetWorldPos.y = transform.position.y;

            playerController.PlayAnimation("Slow Run");

            while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

                Vector3 direction = (targetWorldPos - transform.position).normalized;
                if (direction.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                }

                yield return null;
            }

            transform.position = targetWorldPos;
        }

        playerController.PlayAnimation("Idle");

        PlayerUI playerUI = Object.FindFirstObjectByType<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerController.GetPlayerData());
        }

        enabled = true;
    }
}