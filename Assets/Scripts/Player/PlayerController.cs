using UnityEngine;
using UnityEngine.InputSystem; // AGREGAR ESTE USING
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] public bool isLocalPlayer { get; set; } = true; // ‚Üê Agregar propiedad p√∫blica

    [SerializeField] private int playerNumber = 1;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

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

        if (playerNumber == 1)
        {
            playerData.gridPosition = new Vector2Int(1, 1);
        }
        else
        {
            playerData.gridPosition = new Vector2Int(8, 8);
        }

        // Validar que MapGenerator existe antes de usarlo
        if (MapGenerator.Instance != null)
        {
            transform.position = MapGenerator.Instance.GetWorldPosition(playerData.gridPosition.x, playerData.gridPosition.y);
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        playerUI = Object.FindFirstObjectByType<PlayerUI>();
        PlayAnimation(ANIM_IDLE);
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

    void HandleInput()
    {
        if (MapGenerator.Instance == null || GridVisualizer.Instance == null)
        {
            return;
        }

        // USAR NEW INPUT SYSTEM
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        if (mouse == null || keyboard == null)
        {
            return; // Dispositivos no disponibles
        }

        // Click del ratÛn para movimiento
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int targetPos = MapGenerator.Instance.GetGridPosition(hit.point);

                if (MapGenerator.Instance.IsWalkable(targetPos.x, targetPos.y))
                {
                    TryMoveTo(targetPos);
                }
            }
        }

        // Teclas para hechizos
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            TryCastSpell(0); // Fuego
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            TryCastSpell(1); // Tierra
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            TryCastSpell(2); // Agua
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            TryCastSpell(3); // Viento
        }

        // Pasar turno
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            EndTurn();
        }
    }

    public void TryMoveTo(Vector2Int targetPos)
    {
        if (!playerData.isMyTurn || isMoving || PathfindingSystem.Instance == null) return;

        List<Vector2Int> path = PathfindingSystem.Instance.FindPath(playerData.gridPosition, targetPos);

        if (path != null && path.Count <= playerData.currentMovementPoints)
        {
            GridVisualizer.Instance.ShowPathToTarget(playerData.gridPosition, targetPos, playerData.currentMovementPoints);

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
            Debug.Log("No hay suficientes puntos de movimiento o el camino est· bloqueado");
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

        var mouse = Mouse.current;

        while (!targetSelected && isCastingSpell && mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Vector2Int targetPos = MapGenerator.Instance.GetGridPosition(hit.point);

                    if (IsInSpellRange(playerData.gridPosition, targetPos, spell))
                    {
                        StartCoroutine(CastSpell(spell, targetPos));
                        targetSelected = true;
                    }
                }
            }

            // Cancelar con click derecho
            if (mouse.rightButton.wasPressedThisFrame)
            {
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

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ShowMovementRange(playerData.gridPosition, playerData.currentMovementPoints);
        }

        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
        }
    }

    public void EndTurn()
    {
        playerData.isMyTurn = false;

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }

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
            Debug.Log($"°Jugador {playerNumber} ha sido derrotado!");
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