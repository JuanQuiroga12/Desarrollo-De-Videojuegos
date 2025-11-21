using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Sistema de movimiento basado en taps/clicks para jugadores locales.
/// ✅ CORREGIDO: Detecta correctamente raycasts en cámara ortográfica
/// </summary>
public class PlayerMovementSystem : MonoBehaviour
{
    [Header("Colores de Visualización")]
    [SerializeField] private Color reachableColor = Color.green;
    [SerializeField] private Color unreachableColor = Color.red;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Configuración Touch")]
    [SerializeField] private float doubleTapTime = 0.5f;

    // Estado
    private PlayerController playerController;
    private Vector2Int? selectedTarget = null;
    private List<Vector2Int> currentPath = null;
    private float lastTapTime = 0f;
    private Vector2Int lastTappedTile = Vector2Int.zero;

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
    }

    void Update()
    {
        PlayerData playerData = playerController.GetPlayerData();

        if (playerData == null)
        {
            Debug.LogError("[PlayerMovementSystem] ❌ PlayerData es null");
            return;
        }

        if (!playerData.isMyTurn || !playerController.isLocalPlayer)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[PlayerMovementSystem] ⏳ {gameObject.name} - isMyTurn: {playerData.isMyTurn}, isLocalPlayer: {playerController.isLocalPlayer}");
            }
            return;
        }

        SpellCastingSystem spellSystem = GetComponent<SpellCastingSystem>();
        if (spellSystem != null && spellSystem.IsSelectingTarget())
        {
            Debug.Log("[PlayerMovementSystem] 🔮 Sistema de casteo activo - Ignorando movimiento");
            return;
        }

        HandleMovementInput();
    }

    void HandleMovementInput()
    {
        Vector2Int? tappedTile = null;

        if (Touch.activeTouches.Count > 0)
        {
            Debug.Log($"[PlayerMovementSystem] 📱 TOQUE DETECTADO - Cantidad: {Touch.activeTouches.Count}");
            var touch = Touch.activeTouches[0];

            Debug.Log($"[PlayerMovementSystem] 📱 Touch info - Phase: {touch.phase}, Position: {touch.screenPosition}");

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Debug.Log($"[PlayerMovementSystem] 📱 Touch BEGAN - Obteniendo tile desde: {touch.screenPosition}");
                tappedTile = GetTileFromScreenPosition(touch.screenPosition);
                if (tappedTile.HasValue)
                {
                    Debug.Log($"[PlayerMovementSystem] ✅ Tile obtenido: {tappedTile.Value}");
                }
                else
                {
                    Debug.Log("[PlayerMovementSystem] ❌ No se pudo obtener tile (raycast falló)");
                }
            }
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Debug.Log($"[PlayerMovementSystem] 🖱️ CLICK DEL MOUSE DETECTADO - Posición: {mousePos}");
            tappedTile = GetTileFromScreenPosition(mousePos);
            if (tappedTile.HasValue)
            {
                Debug.Log($"[PlayerMovementSystem] ✅ Tile obtenido: {tappedTile.Value}");
            }
            else
            {
                Debug.Log("[PlayerMovementSystem] ❌ No se pudo obtener tile (raycast falló)");
            }
        }

        if (tappedTile.HasValue)
        {
            Debug.Log($"[PlayerMovementSystem] 🎯 Procesando tap en: {tappedTile.Value}");
            ProcessTileTap(tappedTile.Value);
        }
    }

    /// <summary>
    /// ✅ CORREGIDO: Convierte posición de pantalla a coordenadas de grid usando ORTHOGRAPHIC RAYCAST
    /// </summary>
    Vector2Int? GetTileFromScreenPosition(Vector2 screenPos)
    {
        Debug.Log($"[PlayerMovementSystem] 🔍 GetTileFromScreenPosition - screenPos: {screenPos}");

        if (Camera.main == null)
        {
            Debug.LogError("[PlayerMovementSystem] ❌ Camera.main es null");
            return null;
        }

        // ✅ MÉTODO CORRECTO PARA CÁMARA ORTHOGRAPHIC
        if (Camera.main.orthographic)
        {
            Debug.Log("[PlayerMovementSystem] 📐 Usando raycast para cámara ORTHOGRAPHIC");

            // Crear un plano en Y = 0.55 (altura del grid)
            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, 0.55f, 0));

            // Crear rayo desde la cámara
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Debug.Log($"[PlayerMovementSystem] 🔍 Ray - Origin: {ray.origin}, Direction: {ray.direction}");

            // Verificar intersección con el plano
            if (gridPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Debug.Log($"[PlayerMovementSystem] ✅ Plano intersectado en: {hitPoint}");

                // Convertir a coordenadas de grid
                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hitPoint);
                    Debug.Log($"[PlayerMovementSystem] ✅ Grid Position: {gridPos}");

                    // Verificar si es una casilla válida
                    if (MapGenerator.Instance.IsWalkable(gridPos.x, gridPos.y))
                    {
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
            // Método original para cámara en perspectiva
            Debug.Log("[PlayerMovementSystem] 📐 Usando raycast para cámara PERSPECTIVE");
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Debug.Log($"[PlayerMovementSystem] 🔍 Ray - Origin: {ray.origin}, Direction: {ray.direction}");

            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                Debug.Log($"[PlayerMovementSystem] ✅ Raycast HIT - Objeto: {hit.collider.gameObject.name}, Punto: {hit.point}");

                if (MapGenerator.Instance != null)
                {
                    Vector2Int gridPos = MapGenerator.Instance.GetGridPosition(hit.point);
                    Debug.Log($"[PlayerMovementSystem] ✅ Grid Position: {gridPos}");
                    return gridPos;
                }
            }
            else
            {
                Debug.LogWarning("[PlayerMovementSystem] ❌ Raycast NO HIT");
            }
        }

        return null;
    }

    void ProcessTileTap(Vector2Int tappedTile)
    {
        PlayerData playerData = playerController.GetPlayerData();
        Debug.Log($"[PlayerMovementSystem] 🎯 ProcessTileTap - Tile: {tappedTile}");

        Vector2Int currentPos = playerData.gridPosition;

        bool isDoubleTap = (tappedTile == lastTappedTile) && (Time.time - lastTapTime < doubleTapTime);
        Debug.Log($"[PlayerMovementSystem] 🔄 DoubleTap: {isDoubleTap}");

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
            Debug.Log($"[PlayerMovementSystem] ✅ DoubleTap confirmado - Confirmando movimiento");
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
            Debug.Log($"[PlayerMovementSystem] ⚠️ PM insuficientes");

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