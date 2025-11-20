using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] public bool isLocalPlayer { get; set; } = true; // ← Agregar propiedad pública

    [SerializeField] private int playerNumber = 1;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // ✅ NUEVO: Referencia al indicador visual
    [Header("Visual Feedback")]
    [SerializeField] private PlayerTurnIndicator turnIndicator;

    private readonly string ANIM_IDLE = "Idle";
    private readonly string ANIM_WALK = "Slow Run";
    private readonly string ANIM_SPELL = "Magic Spell Cast";

    private bool isMoving = false;
    private bool isCastingSpell = false;
    private Queue<Vector2Int> movementQueue = new Queue<Vector2Int>();

    private PlayerUI playerUI;

    void Start()
    {
        if (playerData == null)
        {
            playerData = new PlayerData();
        }

        // Configurar grid position según el número de jugador
        if (playerNumber == 1)
        {
            playerData.gridPosition = new Vector2Int(1, 1);
        }
        else
        {
            playerData.gridPosition = new Vector2Int(8, 8);
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // ✅ OBTENER O CREAR INDICADOR
        if (turnIndicator == null)
        {
            turnIndicator = GetComponent<PlayerTurnIndicator>();

            if (turnIndicator == null)
            {
                turnIndicator = gameObject.AddComponent<PlayerTurnIndicator>();
                Debug.Log($"[PlayerController] PlayerTurnIndicator agregado automáticamente a {gameObject.name}");
            }
        }

        // ✅ IMPORTANTE: Desactivar por defecto
        if (turnIndicator != null)
        {
            turnIndicator.SetActive(false);
            Debug.Log($"[PlayerController] Indicador desactivado inicialmente para {gameObject.name}");
        }

        playerUI = Object.FindFirstObjectByType<PlayerUI>();
        PlayAnimation(ANIM_IDLE);

        Debug.Log($"[PlayerController] Player {playerNumber} inicializado");
        Debug.Log($"    - Posición: {transform.position}");
        Debug.Log($"    - Grid Position: {playerData.gridPosition}");
        Debug.Log($"    - Indicador: {(turnIndicator != null ? "✅ PRESENTE" : "❌ FALTA")}");
    }

    void Update()
    {
        if (isLocalPlayer && playerData.isMyTurn && !isMoving && !isCastingSpell)
        {
            HandleInput();
        }

        if (movementQueue.Count > 0 && !isMoving)
        {
            StartCoroutine(MoveToPosition(movementQueue.Dequeue()));
        }
    }

    // ✅ SIMPLIFICAR HandleInput - El movimiento lo maneja PlayerMovementVisualizer
    void HandleInput()
    {
        // ✅ VALIDACIÓN: Solo el jugador activo en su turno puede dar input
        if (!isLocalPlayer || !playerData.isMyTurn || isMoving || isCastingSpell)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // ✅ Solo manejar teclas de hechizos y pasar turno
        // El movimiento lo maneja PlayerMovementVisualizer

        // Teclas 1-4 para hechizos
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            Debug.Log($"[PlayerController] Player {playerNumber} presionó 1 (Fuego)");
            TryCastSpell(0);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            Debug.Log($"[PlayerController] Player {playerNumber} presionó 2 (Tierra)");
            TryCastSpell(1);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            Debug.Log($"[PlayerController] Player {playerNumber} presionó 3 (Agua)");
            TryCastSpell(2);
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            Debug.Log($"[PlayerController] Player {playerNumber} presionó 4 (Viento)");
            TryCastSpell(3);
        }

        // Pasar turno con Space
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"[PlayerController] Player {playerNumber} presionó Space (End Turn)");
            EndTurn();
        }

        // Cancelar acción con Escape
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (isCastingSpell)
            {
                Debug.Log($"[PlayerController] Player {playerNumber} canceló spell casting");
                isCastingSpell = false;

                if (GridVisualizer.Instance != null)
                {
                    GridVisualizer.Instance.ResetGridColors();
                }
            }
        }
    }

    public void TryMoveTo(Vector2Int targetPos)
    {
        // ✅ VALIDACIONES MEJORADAS
        if (!playerData.isMyTurn)
        {
            Debug.LogWarning($"[PlayerController] Player {playerNumber} tried to move, but it's not their turn!");
            return;
        }

        if (isMoving)
        {
            Debug.LogWarning($"[PlayerController] Player {playerNumber} is already moving!");
            return;
        }

        if (isCastingSpell)
        {
            Debug.LogWarning($"[PlayerController] Player {playerNumber} is casting a spell, can't move!");
            return;
        }

        if (PathfindingSystem.Instance == null)
        {
            Debug.LogError("[PlayerController] PathfindingSystem.Instance is null!");
            return;
        }

        Debug.Log($"[PlayerController] Player {playerNumber} trying to move from {playerData.gridPosition} to {targetPos}");

        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(playerData.gridPosition, targetPos);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[PlayerController] No valid path found from {playerData.gridPosition} to {targetPos}");
            return;
        }

        Debug.Log($"[PlayerController] Path found with {path.Count} steps. Current MP: {playerData.currentMovementPoints}");

        if (path.Count <= playerData.currentMovementPoints)
        {
            Debug.Log($"[PlayerController] ✅ Moving! Path length: {path.Count}, MP available: {playerData.currentMovementPoints}");

            if (GridVisualizer.Instance != null)
            {
                GridVisualizer.Instance.ShowPathToTarget(playerData.gridPosition, targetPos, playerData.currentMovementPoints);
            }

            foreach (Vector2Int pos in path)
            {
                movementQueue.Enqueue(pos);
            }

            playerData.Move(path.Count);

            if (playerUI != null)
            {
                playerUI.UpdatePlayerStats(playerData);
            }
        }
        else
        {
            Debug.LogWarning($"[PlayerController] ❌ Not enough movement points! Need {path.Count}, have {playerData.currentMovementPoints}");

            if (GridVisualizer.Instance != null)
            {
                GridVisualizer.Instance.ShowPathToTarget(playerData.gridPosition, targetPos, playerData.currentMovementPoints);
            }
        }
    }

    IEnumerator MoveToPosition(Vector2Int targetPos)
    {
        isMoving = true;
        PlayAnimation(ANIM_WALK);

        Vector3 startPos = transform.position;
        Vector3 endPos = MapGenerator.Instance.GetWorldPosition(targetPos.x, targetPos.y);

        // ✅ CORREGIDO: Mantener la altura correcta durante el movimiento
        endPos.y = 0.41f; // ← Mismo valor que PLAYER_SPAWN_HEIGHT en GameManager

        Vector3 direction = (endPos - startPos).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float rotationTime = 0f;
            while (rotationTime < 0.3f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                rotationTime += Time.deltaTime;
                yield return null;
            }
        }

        float journey = 0f;
        float distance = Vector3.Distance(startPos, endPos);

        while (journey <= 1f)
        {
            journey += (moveSpeed * Time.deltaTime) / distance;
            transform.position = Vector3.Lerp(startPos, endPos, journey);
            yield return null;
        }

        playerData.gridPosition = targetPos;

        if (movementQueue.Count == 0)
        {
            PlayAnimation(ANIM_IDLE);
            isMoving = false;

            if (GridVisualizer.Instance != null)
            {
                GridVisualizer.Instance.ShowMovementRange(playerData.gridPosition, playerData.currentMovementPoints);
            }
        }
    }

    public void TryCastSpell(int spellIndex)
    {
        if (!playerData.isMyTurn || isCastingSpell || spellIndex >= playerData.spells.Count) return;

        SpellData spell = playerData.spells[spellIndex];

        if (playerData.CanCastSpell(spell))
        {
            if (GridVisualizer.Instance != null)
            {
                GridVisualizer.Instance.ShowSpellRange(playerData.gridPosition, spell);
            }
            StartCoroutine(WaitForSpellTarget(spell));
        }
        else
        {
            Debug.Log($"No hay suficientes PA. Necesitas {spell.apCost}, tienes {playerData.currentAttackPoints}");
        }
    }

    IEnumerator WaitForSpellTarget(SpellData spell)
    {
        isCastingSpell = true;
        bool targetSelected = false;

        Debug.Log($"[PlayerController] Player {playerNumber} waiting for spell target ({spell.spellName})");

        var mouse = Mouse.current;

        while (!targetSelected && isCastingSpell && mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                // Verificar que NO estamos sobre UI
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("[PlayerController] Click on UI, ignoring");
                    yield return null;
                    continue;
                }

                Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Vector2Int targetPos = MapGenerator.Instance.GetGridPosition(hit.point);

                    Debug.Log($"[PlayerController] Spell target clicked: {targetPos}");

                    if (IsInSpellRange(playerData.gridPosition, targetPos, spell))
                    {
                        Debug.Log($"[PlayerController] ✅ Target in range, casting {spell.spellName}");
                        StartCoroutine(CastSpell(spell, targetPos));
                        targetSelected = true;
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerController] ❌ Target {targetPos} is out of range (max: {spell.range})");
                    }
                }
            }

            // Cancelar con click derecho o Escape
            if (mouse.rightButton.wasPressedThisFrame ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                Debug.Log($"[PlayerController] Spell casting cancelled by player {playerNumber}");
                isCastingSpell = false;
                if (GridVisualizer.Instance != null)
                {
                    GridVisualizer.Instance.ResetGridColors();
                }
            }

            yield return null;
        }
    }

    IEnumerator CastSpell(SpellData spell, Vector2Int targetPos)
    {
        PlayAnimation(ANIM_SPELL);

        Vector3 targetWorldPos = MapGenerator.Instance.GetWorldPosition(targetPos.x, targetPos.y);
        Vector3 direction = (targetWorldPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        yield return new WaitForSeconds(1.5f);

        ApplySpellEffects(spell, targetPos);
        playerData.CastSpell(spell);

        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
        }

        PlayAnimation(ANIM_IDLE);
        isCastingSpell = false;

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }
    }

    void ApplySpellEffects(SpellData spell, Vector2Int targetPos)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;

        switch (spell.element)
        {
            case SpellElement.Fire:
                gameManager.ApplyDamageAt(targetPos, spell.damage);
                break;

            case SpellElement.Earth:
                gameManager.ApplyDamageAt(targetPos, spell.damage);
                break;

            case SpellElement.Water:
                playerData.Heal(spell.healing);
                playerData.ModifyMovementPoints(spell.movementBonus);
                break;

            case SpellElement.Wind:
                List<PlayerController> affectedPlayers = gameManager.GetPlayersInArea(targetPos, spell.areaSize);

                foreach (PlayerController player in affectedPlayers)
                {
                    if (player == this)
                    {
                        if (affectedPlayers.Count > 1)
                        {
                            player.playerData.ModifyMovementPoints(2);
                            player.playerData.ModifyAttackPoints(1);
                        }
                        else
                        {
                            player.playerData.ModifyMovementPoints(1);
                        }
                    }
                    else
                    {
                        if (affectedPlayers.Count > 1)
                        {
                            player.playerData.ModifyMovementPoints(-2);
                            player.playerData.ModifyAttackPoints(-1);
                        }
                        else
                        {
                            player.playerData.ModifyMovementPoints(-1);
                        }
                    }
                }
                break;
        }
    }

    bool IsInSpellRange(Vector2Int casterPos, Vector2Int targetPos, SpellData spell)
    {
        if (spell.spellType == SpellType.Self)
        {
            return casterPos == targetPos;
        }

        int distance = Mathf.Abs(casterPos.x - targetPos.x) + Mathf.Abs(casterPos.y - targetPos.y);
        return distance <= spell.range;
    }

    void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }

    public void StartTurn()
    {
        playerData.StartNewTurn();
        playerData.isMyTurn = true;

        Debug.Log($"[PlayerController] ✅ Player {playerNumber} INICIA TURNO");
        Debug.Log($"    - isMyTurn: {playerData.isMyTurn}");
        Debug.Log($"    - PM: {playerData.currentMovementPoints}/{playerData.baseMovementPoints}");
        Debug.Log($"    - PA: {playerData.currentAttackPoints}/{playerData.baseAttackPoints}");

        // ✅ ACTIVAR INDICADOR VISUAL
        if (turnIndicator != null)
        {
            turnIndicator.SetActive(true);
            Debug.Log($"[PlayerController] ✅ Indicador ACTIVADO para Player {playerNumber}");
        }
        else
        {
            Debug.LogError($"[PlayerController] ❌ turnIndicator es NULL para Player {playerNumber}!");
        }

        // Mostrar rango de movimiento
        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ShowMovementRange(playerData.gridPosition, playerData.currentMovementPoints);
            Debug.Log($"[PlayerController] Mostrando rango de movimiento desde {playerData.gridPosition}");
        }

        // Actualizar UI
        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
        }
    }

    public void EndTurn()
    {
        playerData.isMyTurn = false;

        Debug.Log($"[PlayerController] ⏸️ Player {playerNumber} TERMINA TURNO");

        // ✅ DESACTIVAR INDICADOR VISUAL
        if (turnIndicator != null)
        {
            turnIndicator.SetActive(false);
            Debug.Log($"[PlayerController] ⏸️ Indicador DESACTIVADO para Player {playerNumber}");
        }

        // Limpiar visualización
        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }

        // Notificar al GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndCurrentTurn();
        }
    }

    public void TakeDamage(int damage)
    {
        playerData.TakeDamage(damage);

        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
        }

        if (!playerData.IsAlive())
        {
            Debug.Log($"¡Jugador {playerNumber} ha sido derrotado!");
        }
    }

    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }

    public void SetPlayerNumber(int number)
    {
        playerNumber = number;
    }

    public void SetIsLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal;
    }
}