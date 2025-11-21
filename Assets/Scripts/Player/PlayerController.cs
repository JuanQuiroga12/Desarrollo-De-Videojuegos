using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador principal del jugador. Gestiona datos, turno, animaciones y daño.
/// El movimiento y casting ahora se delegan a sistemas especializados.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] public bool isLocalPlayer = false;
    [SerializeField] private int playerNumber = 1;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Visual Feedback")]
    [SerializeField] private PlayerTurnIndicator turnIndicator;

    // Animaciones
    private readonly string ANIM_IDLE = "Idle";
    private readonly string ANIM_WALK = "Slow Run";
    private readonly string ANIM_SPELL = "Magic Spell Cast";

    // Estado
    private bool isCastingSpell = false;
    private PlayerUI playerUI;

    // Referencias a sistemas
    private PlayerMovementSystem movementSystem;
    private SpellCastingSystem spellCastingSystem;

    void Start()
    {
        if (playerData == null)
        {
            playerData = new PlayerData();
        }

        playerUI = Object.FindFirstObjectByType<PlayerUI>();

        // Inicializar posición en grid
        Vector3 worldPos = transform.position;
        playerData.gridPosition = MapGenerator.Instance.GetGridPosition(worldPos);

        // Obtener referencias a sistemas
        movementSystem = GetComponent<PlayerMovementSystem>();
        spellCastingSystem = GetComponent<SpellCastingSystem>();

        // Configurar animator si existe
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Configurar indicador de turno
        if (turnIndicator == null)
        {
            turnIndicator = GetComponent<PlayerTurnIndicator>();
        }

        Debug.Log($"[PlayerController] {playerData.username} iniciado. isLocalPlayer: {isLocalPlayer}, PlayerNumber: {playerNumber}");
    }

    void Update()
    {
        // Solo permitir input si:
        // 1. Es jugador local
        // 2. Es su turno
        // 3. No está casteando un hechizo
        if (isLocalPlayer && playerData.isMyTurn && !isCastingSpell)
        {
            HandleInput();
        }
    }

    /// <summary>
    /// Maneja el input del jugador usando el nuevo Input System.
    /// Teclas numéricas para seleccionar hechizos.
    /// El movimiento por tap lo maneja PlayerMovementSystem.
    /// </summary>
    void HandleInput()
    {
        // ✅ NUEVO Input System: Keyboard.current puede ser null si no hay teclado
        if (Keyboard.current == null)
            return;

        // Teclas numéricas para seleccionar hechizos
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SelectSpell(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SelectSpell(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SelectSpell(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SelectSpell(3);
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CancelSpellSelection();
    }

    /// <summary>
    /// Selecciona un hechizo para castear.
    /// </summary>
    void SelectSpell(int spellIndex)
    {
        if (spellCastingSystem != null)
        {
            spellCastingSystem.SelectSpell(spellIndex);
        }
        else
        {
            Debug.LogWarning("[PlayerController] SpellCastingSystem no encontrado");
        }
    }

    /// <summary>
    /// Cancela la selección de hechizo actual.
    /// </summary>
    void CancelSpellSelection()
    {
        if (spellCastingSystem != null)
        {
            spellCastingSystem.CancelSpellSelection();
        }
    }

    /// <summary>
    /// Inicia el turno del jugador.
    /// </summary>
    public void StartTurn()
    {
        Debug.Log($"[PlayerController] {playerData.username} inicia turno");

        // ✅ PRIMERO: Actualizar el estado del turno
        playerData.StartNewTurn();

        // SEGUNDO: Activar indicador visual
        if (turnIndicator != null)
        {
            turnIndicator.SetActive(true);
        }

        // TERCERO: Habilitar sistemas de entrada
        if (isLocalPlayer)
        {
            if (movementSystem != null)
            {
                movementSystem.enabled = true;
            }

            if (spellCastingSystem != null)
            {
                spellCastingSystem.enabled = true;
            }
        }
    }

    /// <summary>
    /// Termina el turno del jugador.
    /// </summary>
    public void EndTurn()
    {
        Debug.Log($"[PlayerController] {playerData.username} termina turno");

        playerData.isMyTurn = false;

        // Desactivar indicador visual
        if (turnIndicator != null)
        {
            turnIndicator.SetActive(false);
        }

        // Deshabilitar sistemas
        if (movementSystem != null)
        {
            movementSystem.enabled = false;
        }

        if (spellCastingSystem != null)
        {
            spellCastingSystem.CancelSpellSelection();
            spellCastingSystem.enabled = false;
        }

        // Actualizar UI
        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
        }

        // Notificar al GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndCurrentTurn();
        }
    }

    /// <summary>
    /// Aplica daño al jugador.
    /// </summary>
    public void TakeDamage(int damage)
    {
        playerData.TakeDamage(damage);

        Debug.Log($"[PlayerController] {playerData.username} recibe {damage} de daño. HP restante: {playerData.currentHealth}");

        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
        }

        if (!playerData.IsAlive())
        {
            Debug.Log($"[PlayerController] {playerData.username} ha muerto");
        }
    }

    /// <summary>
    /// Cura al jugador.
    /// </summary>
    public void Heal(int amount)
    {
        playerData.Heal(amount);

        Debug.Log($"[PlayerController] {playerData.username} se cura {amount}. HP: {playerData.currentHealth}");

        if (playerUI != null)
        {
            playerUI.UpdatePlayerStats(playerData);
        }
    }

    /// <summary>
    /// Reproduce una animación.
    /// </summary>
    public void PlayAnimation(string animationName)
    {
        if (animator != null && !string.IsNullOrEmpty(animationName))
        {
            animator.Play(animationName);
        }
    }

    // ========== GETTERS Y SETTERS ==========

    public PlayerData GetPlayerData() => playerData;

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
        Debug.Log($"[PlayerController] {playerData?.username} SetIsLocalPlayer: {isLocal}");
    }

    public int GetPlayerNumber() => playerNumber;

    public bool IsCastingSpell() => isCastingSpell;

    public void SetCastingSpell(bool casting)
    {
        isCastingSpell = casting;
    }
}