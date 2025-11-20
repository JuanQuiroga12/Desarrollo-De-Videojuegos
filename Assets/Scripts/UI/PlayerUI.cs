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

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void SetupSpellButtons()
    {

        if (spellButtons == null || spellButtons.Count == 0)
            return;

        for (int i = 0; i < spellButtons.Count && i < 4; i++)
        {
            int spellIndex = i;

            // ✅ CONECTAR BOTÓN CON LISTENER
            spellButtons[i].onClick.AddListener(() => OnSpellButtonClicked(spellIndex));

            // Configurar colores
            if (spellIcons != null && i < spellIcons.Count)
            {
                switch (i)
                {
                    case 0: spellIcons[i].color = new Color(1f, 0.3f, 0f); break;
                    case 1: spellIcons[i].color = new Color(0.6f, 0.4f, 0.2f); break;
                    case 2: spellIcons[i].color = new Color(0f, 0.5f, 1f); break;
                    case 3: spellIcons[i].color = new Color(0.7f, 1f, 0.7f); break;
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

            // Agregar esto en SetupSpellButtons() de PlayerUI.cs
            spellButtons[i].onClick.AddListener(() =>
            {
                PlayerController activePlayer = GetActivePlayer();
                if (activePlayer != null)
                {
                    SpellCastingSystem spellSystem = activePlayer.GetComponent<SpellCastingSystem>();
                    if (spellSystem != null)
                    {
                        spellSystem.SelectSpell(spellIndex);
                    }
                }
            });

            AddTooltipEvents(spellButtons[i], i);
        }
    }

    void AddTooltipEvents(Button button, int spellIndex)
    {
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { ShowSpellTooltip(spellIndex); });
        trigger.triggers.Add(enterEntry);

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
            case 0:
                tooltipTitle.text = "Fuego";
                tooltipDescription.text = "Costo: 3 PA\nDaño: 40\nRango: 4";
                break;
            case 1:
                tooltipTitle.text = "Tierra";
                tooltipDescription.text = "Costo: 2 PA\nDaño: 30\nRango: 3";
                break;
            case 2:
                tooltipTitle.text = "Agua";
                tooltipDescription.text = "Costo: 3 PA\nCuración: 20\n+1 PM";
                break;
            case 3:
                tooltipTitle.text = "Viento";
                tooltipDescription.text = "Costo: 2 PA\nÁrea: 3x3";
                break;
        }
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
            // ✅ CONECTAR BOTÓN DE PASAR TURNO
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }
    }

    void OnSpellButtonClicked(int spellIndex)
    {
        Debug.Log($"Hechizo {spellIndex} seleccionado");

        PlayerController activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.TryCastSpell(spellIndex);
            AddActionToLog($"{activePlayer.GetPlayerData().username} seleccionó hechizo {GetSpellName(spellIndex)}");
        }
        else
        {
            Debug.LogWarning("No hay jugador activo");
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
        Debug.Log("Botón de pasar turno presionado");

        PlayerController activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            AddActionToLog($"{activePlayer.GetPlayerData().username} terminó su turno");
            activePlayer.EndTurn();
        }
        else
        {
            Debug.LogWarning("No hay jugador activo para terminar turno");
        }
    }

    PlayerController GetActivePlayer()
    {
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.GetPlayerData() != null && player.GetPlayerData().isMyTurn)
                return player;
        }
        return null;
    }

    public void UpdatePlayerStats(PlayerData playerData)
    {
        if (playerData == null) return;

        bool isPlayer1 = playerData.username == GameManager.Instance.GetGameState().player1.username;

        if (isPlayer1)
        {
            UpdatePlayer1Stats(playerData);
        }
        else
        {
            UpdatePlayer2Stats(playerData);
        }

        UpdateSpellAvailability(playerData);
    }

    void UpdatePlayer1Stats(PlayerData data)
    {
        if (player1NameText != null)
            player1NameText.text = data.username;

        if (player1HealthBar != null)
            player1HealthBar.value = data.currentHealth;

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
            player2HealthBar.value = data.currentHealth;

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
            foreach (Button btn in spellButtons)
            {
                btn.interactable = false;
            }
            return;
        }

        for (int i = 0; i < spellButtons.Count && i < playerData.spells.Count; i++)
        {
            bool canCast = playerData.currentAttackPoints >= playerData.spells[i].apCost;
            spellButtons[i].interactable = canCast;

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

        if (actionLog.Count > maxLogEntries)
        {
            actionLog.RemoveAt(0);
        }

        if (actionLogText != null)
        {
            actionLogText.text = string.Join("\n", actionLog);

            if (actionLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                actionLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    public void ShowDamagePopup(Vector3 worldPosition, int damage, Color color)
    {
        AddActionToLog($"Daño infligido: {damage}");
    }

    public void ShowHealPopup(Vector3 worldPosition, int healing)
    {
        AddActionToLog($"Curación recibida: {healing}");
    }
}