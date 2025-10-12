using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Player Stats Panel")]
    [SerializeField] private GameObject player1StatsPanel;
    [SerializeField] private GameObject player2StatsPanel;

    [Header("Player 1 UI")]
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private Slider player1HealthBar;
    [SerializeField] private TextMeshProUGUI player1HealthText;
    [SerializeField] private TextMeshProUGUI player1PMText;
    [SerializeField] private TextMeshProUGUI player1PAText;

    [Header("Player 2 UI")]
    [SerializeField] private TextMeshProUGUI player2NameText;
    [SerializeField] private Slider player2HealthBar;
    [SerializeField] private TextMeshProUGUI player2HealthText;
    [SerializeField] private TextMeshProUGUI player2PMText;
    [SerializeField] private TextMeshProUGUI player2PAText;

    [Header("Spell UI")]
    [SerializeField] private GameObject spellPanel;
    [SerializeField] private List<Button> spellButtons;
    [SerializeField] private List<Image> spellIcons;
    [SerializeField] private List<TextMeshProUGUI> spellCostTexts;
    [SerializeField] private List<Image> spellCooldownOverlays;

    [Header("Spell Colors")]
    [SerializeField] private Color fireColor = new Color(1f, 0.3f, 0f);
    [SerializeField] private Color earthColor = new Color(0.6f, 0.4f, 0.2f);
    [SerializeField] private Color waterColor = new Color(0f, 0.5f, 1f);
    [SerializeField] private Color windColor = new Color(0.7f, 1f, 0.7f);

    [Header("Turn Indicator")]
    [SerializeField] private GameObject turnIndicatorPanel;
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI turnTimerText;
    [SerializeField] private Button endTurnButton;

    [Header("Action Log")]
    [SerializeField] private GameObject actionLogPanel;
    [SerializeField] private TextMeshProUGUI actionLogText;
    [SerializeField] private ScrollRect actionLogScrollRect;
    private List<string> actionLog = new List<string>();
    private int maxLogEntries = 20;

    [Header("Tooltips")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipDescription;

    void Start()
    {
        SetupSpellButtons();
        SetupEndTurnButton();
        InitializeUI();
    }

    void InitializeUI()
    {
        // Configurar barras de vida
        if (player1HealthBar != null)
        {
            player1HealthBar.maxValue = 200;
            player1HealthBar.value = 200;
        }

        if (player2HealthBar != null)
        {
            player2HealthBar.maxValue = 200;
            player2HealthBar.value = 200;
        }

        // Ocultar tooltip al inicio
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void SetupSpellButtons()
    {
        if (spellButtons == null || spellButtons.Count == 0) return;

        // Configurar botones de hechizos
        for (int i = 0; i < spellButtons.Count && i < 4; i++)
        {
            int spellIndex = i;
            spellButtons[i].onClick.AddListener(() => OnSpellButtonClicked(spellIndex));

            // Configurar colores de los iconos
            if (spellIcons != null && i < spellIcons.Count)
            {
                switch (i)
                {
                    case 0: spellIcons[i].color = fireColor; break;
                    case 1: spellIcons[i].color = earthColor; break;
                    case 2: spellIcons[i].color = waterColor; break;
                    case 3: spellIcons[i].color = windColor; break;
                }
            }

            // Configurar textos de costo
            if (spellCostTexts != null && i < spellCostTexts.Count)
            {
                switch (i)
                {
                    case 0: spellCostTexts[i].text = "3 PA"; break;
                    case 1: spellCostTexts[i].text = "2 PA"; break;
                    case 2: spellCostTexts[i].text = "3 PA"; break;
                    case 3: spellCostTexts[i].text = "2 PA"; break;
                }
            }

            // Agregar eventos de hover para tooltips
            AddTooltipEvents(spellButtons[i], i);
        }
    }

    void AddTooltipEvents(Button button, int spellIndex)
    {
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        // Mouse Enter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { ShowSpellTooltip(spellIndex); });
        trigger.triggers.Add(enterEntry);

        // Mouse Exit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { HideTooltip(); });
        trigger.triggers.Add(exitEntry);
    }

    void ShowSpellTooltip(int spellIndex)
    {
        if (tooltipPanel == null) return;

        tooltipPanel.SetActive(true);

        switch (spellIndex)
        {
            case 0: // Fuego
                tooltipTitle.text = "Fuego";
                tooltipDescription.text = "Costo: 3 PA\nDaño: 40\nRango: 4\nLanza una bola de fuego al enemigo.";
                break;
            case 1: // Tierra
                tooltipTitle.text = "Tierra";
                tooltipDescription.text = "Costo: 2 PA\nDaño: 30\nRango: 3\nGolpea con el poder de la tierra.";
                break;
            case 2: // Agua
                tooltipTitle.text = "Agua";
                tooltipDescription.text = "Costo: 3 PA\nCuración: 20\n+1 PM\nSolo a ti mismo\nRestaura vida y movilidad.";
                break;
            case 3: // Viento
                tooltipTitle.text = "Viento";
                tooltipDescription.text = "Costo: 2 PA\nÁrea: 3x3\nEfectos variables según objetivos\nManipula el campo de batalla.";
                break;
        }

        // Posicionar tooltip cerca del cursor
        Vector3 mousePos = Input.mousePosition;
        tooltipPanel.transform.position = mousePos + new Vector3(100, -50, 0);
    }

    void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void SetupEndTurnButton()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }
    }

    void OnSpellButtonClicked(int spellIndex)
    {
        // Buscar el jugador activo
        PlayerController activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.TryCastSpell(spellIndex);
            AddActionToLog($"{activePlayer.GetPlayerData().username} seleccionó hechizo {GetSpellName(spellIndex)}");
        }
    }

    string GetSpellName(int index)
    {
        switch (index)
        {
            case 0: return "Fuego";
            case 1: return "Tierra";
            case 2: return "Agua";
            case 3: return "Viento";
            default: return "Hechizo";
        }
    }

    void OnEndTurnClicked()
    {
        PlayerController activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            AddActionToLog($"{activePlayer.GetPlayerData().username} terminó su turno");
            activePlayer.EndTurn();
        }
    }

    // Reemplaza este método:
    PlayerController GetActivePlayer()
    {
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.GetPlayerData().isMyTurn)
                return player;
        }
        return null;
    }

    public void UpdatePlayerStats(PlayerData playerData)
    {
        // Determinar qué panel actualizar basándose en el nombre del jugador
        bool isPlayer1 = playerData.username == GameManager.Instance.GetGameState().player1.username;

        if (isPlayer1)
        {
            UpdatePlayer1Stats(playerData);
        }
        else
        {
            UpdatePlayer2Stats(playerData);
        }

        // Actualizar disponibilidad de hechizos
        UpdateSpellAvailability(playerData);
    }

    void UpdatePlayer1Stats(PlayerData data)
    {
        if (player1NameText != null)
            player1NameText.text = data.username;

        if (player1HealthBar != null)
        {
            player1HealthBar.value = data.currentHealth;
        }

        if (player1HealthText != null)
            player1HealthText.text = $"{data.currentHealth}/{data.maxHealth}";

        if (player1PMText != null)
            player1PMText.text = $"PM: {data.currentMovementPoints}/{data.baseMovementPoints}";

        if (player1PAText != null)
            player1PAText.text = $"PA: {data.currentAttackPoints}/{data.baseAttackPoints}";
    }

    void UpdatePlayer2Stats(PlayerData data)
    {
        if (player2NameText != null)
            player2NameText.text = data.username;

        if (player2HealthBar != null)
        {
            player2HealthBar.value = data.currentHealth;
        }

        if (player2HealthText != null)
            player2HealthText.text = $"{data.currentHealth}/{data.maxHealth}";

        if (player2PMText != null)
            player2PMText.text = $"PM: {data.currentMovementPoints}/{data.baseMovementPoints}";

        if (player2PAText != null)
            player2PAText.text = $"PA: {data.currentAttackPoints}/{data.baseAttackPoints}";
    }

    void UpdateSpellAvailability(PlayerData playerData)
    {
        if (!playerData.isMyTurn)
        {
            // Desactivar todos los botones si no es el turno del jugador
            foreach (Button btn in spellButtons)
            {
                btn.interactable = false;
            }
            return;
        }

        // Actualizar disponibilidad según PA
        for (int i = 0; i < spellButtons.Count && i < playerData.spells.Count; i++)
        {
            bool canCast = playerData.currentAttackPoints >= playerData.spells[i].apCost;
            spellButtons[i].interactable = canCast;

            // Mostrar overlay de cooldown si no puede lanzar
            if (spellCooldownOverlays != null && i < spellCooldownOverlays.Count)
            {
                spellCooldownOverlays[i].gameObject.SetActive(!canCast);
            }
        }
    }

    public void UpdateTurnIndicator(string currentPlayerName, float timeRemaining)
    {
        if (currentTurnText != null)
            currentTurnText.text = $"Turno de: {currentPlayerName}";

        if (turnTimerText != null)
            turnTimerText.text = $"Tiempo: {Mathf.CeilToInt(timeRemaining)}s";
    }

    public void AddActionToLog(string action)
    {
        actionLog.Add($"[{System.DateTime.Now:HH:mm:ss}] {action}");

        // Limitar el tamaño del log
        if (actionLog.Count > maxLogEntries)
        {
            actionLog.RemoveAt(0);
        }

        // Actualizar el texto del log
        if (actionLogText != null)
        {
            actionLogText.text = string.Join("\n", actionLog);

            // Auto-scroll al final
            if (actionLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                actionLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    public void ShowDamagePopup(Vector3 worldPosition, int damage, Color color)
    {
        // Aquí puedes crear un texto flotante que muestre el daño
        // Por ahora solo agregamos al log
        AddActionToLog($"Daño infligido: {damage}");
    }

    public void ShowHealPopup(Vector3 worldPosition, int healing)
    {
        // Aquí puedes crear un texto flotante que muestre la curación
        // Por ahora solo agregamos al log
        AddActionToLog($"Curación recibida: {healing}");
    }
}