using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameSceneUISetup : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Dofus Clone/Setup Game Scene UI")]
    public static void SetupGameSceneUI()
    {
        // Buscar o crear Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameUI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Limpiar UI existente
        foreach (Transform child in canvas.transform)
        {
            DestroyImmediate(child.gameObject);
        }

        // Crear todos los paneles de UI
        CreatePlayer1StatsPanel(canvas.transform);
        CreatePlayer2StatsPanel(canvas.transform);
        CreateSpellPanel(canvas.transform);
        CreateTurnIndicatorPanel(canvas.transform);
        CreateActionLogPanel(canvas.transform);
        CreateGameOverPanel(canvas.transform);

        // Agregar PlayerUI component
        if (canvas.GetComponent<PlayerUI>() == null)
        {
            canvas.gameObject.AddComponent<PlayerUI>();
        }

        // Asignar referencias automáticamente
        AssignUIReferences();

        Debug.Log("Game Scene UI configurada completamente!");
    }

    static void CreatePlayer1StatsPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Player1StatsPanel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.7f);
        rect.anchorMax = new Vector2(0.25f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Nombre del jugador
        GameObject nameText = CreateText("Player1NameText", panel.transform, "Jugador 1", 18,
            new Vector2(0.5f, 0.85f), new Vector2(0, 0));

        // Barra de vida
        GameObject healthBar = CreateHealthBar("Player1HealthBar", panel.transform,
            new Vector2(0.5f, 0.65f), new Vector2(0, 0));

        // Texto de vida
        GameObject healthText = CreateText("Player1HealthText", panel.transform, "200/200", 14,
            new Vector2(0.5f, 0.65f), new Vector2(0, 0));
        healthText.GetComponent<TextMeshProUGUI>().color = Color.white;

        // PM Text
        GameObject pmText = CreateText("Player1PMText", panel.transform, "PM: 4/4", 14,
            new Vector2(0.5f, 0.45f), new Vector2(0, 0));

        // PA Text
        GameObject paText = CreateText("Player1PAText", panel.transform, "PA: 6/6", 14,
            new Vector2(0.5f, 0.35f), new Vector2(0, 0));
    }

    static void CreatePlayer2StatsPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Player2StatsPanel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.75f, 0.7f);
        rect.anchorMax = new Vector2(1, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Nombre del jugador
        GameObject nameText = CreateText("Player2NameText", panel.transform, "Jugador 2", 18,
            new Vector2(0.5f, 0.85f), new Vector2(0, 0));

        // Barra de vida
        GameObject healthBar = CreateHealthBar("Player2HealthBar", panel.transform,
            new Vector2(0.5f, 0.65f), new Vector2(0, 0));

        // Texto de vida
        GameObject healthText = CreateText("Player2HealthText", panel.transform, "200/200", 14,
            new Vector2(0.5f, 0.65f), new Vector2(0, 0));
        healthText.GetComponent<TextMeshProUGUI>().color = Color.white;

        // PM Text
        GameObject pmText = CreateText("Player2PMText", panel.transform, "PM: 4/4", 14,
            new Vector2(0.5f, 0.45f), new Vector2(0, 0));

        // PA Text
        GameObject paText = CreateText("Player2PAText", panel.transform, "PA: 6/6", 14,
            new Vector2(0.5f, 0.35f), new Vector2(0, 0));
    }

    static void CreateSpellPanel(Transform parent)
    {
        GameObject panel = CreatePanel("SpellPanel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.35f, 0);
        rect.anchorMax = new Vector2(0.65f, 0.15f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Crear 4 botones de hechizos
        float spacing = 90;
        float startX = -135;

        for (int i = 0; i < 4; i++)
        {
            GameObject spellBtn = CreateSpellButton($"SpellButton{i}", panel.transform, i);
            RectTransform btnRect = spellBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(startX + (i * spacing), 0);
            btnRect.sizeDelta = new Vector2(75, 75);
        }
    }

    static GameObject CreateSpellButton(string name, Transform parent, int index)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);

        Image img = button.AddComponent<Image>();
        Button btn = button.AddComponent<Button>();

        // Colores según elemento
        Color spellColor = Color.white;
        string spellName = "";
        string costText = "";

        switch (index)
        {
            case 0: // Fuego
                spellColor = new Color(1f, 0.3f, 0f);
                spellName = "🔥";
                costText = "3 PA";
                break;
            case 1: // Tierra
                spellColor = new Color(0.6f, 0.4f, 0.2f);
                spellName = "⛰️";
                costText = "2 PA";
                break;
            case 2: // Agua
                spellColor = new Color(0f, 0.5f, 1f);
                spellName = "💧";
                costText = "3 PA";
                break;
            case 3: // Viento
                spellColor = new Color(0.7f, 1f, 0.7f);
                spellName = "🌪️";
                costText = "2 PA";
                break;
        }

        img.color = spellColor;

        // Icono del hechizo
        GameObject icon = CreateText("Icon", button.transform, spellName, 32,
            new Vector2(0.5f, 0.6f), new Vector2(0, 5));

        // Costo del hechizo
        GameObject cost = CreateText("SpellCostText", button.transform, costText, 12,
            new Vector2(0.5f, 0.15f), new Vector2(0, 0));

        // Overlay de cooldown
        GameObject overlay = new GameObject("SpellCooldownOverlay");
        overlay.transform.SetParent(button.transform, false);
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.5f);
        overlay.SetActive(false);

        return button;
    }

    static void CreateTurnIndicatorPanel(Transform parent)
    {
        GameObject panel = CreatePanel("TurnIndicatorPanel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.35f, 0.85f);
        rect.anchorMax = new Vector2(0.65f, 0.95f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        // Texto del turno actual
        GameObject turnText = CreateText("CurrentTurnText", panel.transform, "Turno de: Jugador 1", 20,
            new Vector2(0.3f, 0.5f), new Vector2(0, 0));

        // Timer del turno
        GameObject timerText = CreateText("TurnTimerText", panel.transform, "Tiempo: 60s", 18,
            new Vector2(0.7f, 0.5f), new Vector2(0, 0));
        timerText.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.8f, 0.2f);

        // Botón de pasar turno
        GameObject endTurnBtn = CreateButton("EndTurnButton", panel.transform, "Pasar Turno");
        RectTransform btnRect = endTurnBtn.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.85f, 0.5f);
        btnRect.anchorMax = new Vector2(0.85f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(120, 35);
    }

    static void CreateActionLogPanel(Transform parent)
    {
        GameObject panel = CreatePanel("ActionLogPanel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0.25f, 0.3f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

        // ScrollView
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.sizeDelta = Vector2.zero;
        scrollRect.offsetMin = new Vector2(5, 5);
        scrollRect.offsetMax = new Vector2(-5, -5);

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0, 1);

        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(1, 1, 1, 0);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 300);

        // Log Text
        GameObject logText = CreateText("ActionLogText", content.transform, "", 12,
            new Vector2(0.5f, 1), new Vector2(0, 0));
        TextMeshProUGUI tmp = logText.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.TopLeft;
        RectTransform logRect = logText.GetComponent<RectTransform>();
        logRect.anchorMin = Vector2.zero;
        logRect.anchorMax = Vector2.one;
        logRect.sizeDelta = Vector2.zero;
        logRect.offsetMin = new Vector2(5, 0);
        logRect.offsetMax = new Vector2(-5, 0);

        // Configurar ScrollRect
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
    }

    static void CreateGameOverPanel(Transform parent)
    {
        GameObject panel = CreatePanel("GameOverPanel", parent);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        panel.SetActive(false); // Oculto por defecto

        // Panel central
        GameObject centerPanel = CreatePanel("CenterPanel", panel.transform);
        RectTransform centerRect = centerPanel.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.sizeDelta = new Vector2(600, 400);

        centerPanel.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.95f);

        // Texto de victoria
        GameObject winnerText = CreateText("WinnerText", centerPanel.transform, "¡Victoria!", 48,
            new Vector2(0.5f, 0.7f), new Vector2(0, 0));
        winnerText.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.843f, 0f);

        // Botón reiniciar
        GameObject restartBtn = CreateButton("RestartButton", centerPanel.transform, "Jugar de Nuevo");
        RectTransform restartRect = restartBtn.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0.35f);
        restartRect.anchorMax = new Vector2(0.5f, 0.35f);
        restartRect.anchoredPosition = new Vector2(-100, 0);
        restartRect.sizeDelta = new Vector2(180, 50);

        // Botón menú
        GameObject menuBtn = CreateButton("MenuButton", centerPanel.transform, "Menú Principal");
        RectTransform menuRect = menuBtn.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.35f);
        menuRect.anchorMax = new Vector2(0.5f, 0.35f);
        menuRect.anchoredPosition = new Vector2(100, 0);
        menuRect.sizeDelta = new Vector2(180, 50);
    }

    static void AssignUIReferences()
    {
        // Buscar PlayerUI
        PlayerUI playerUI = Object.FindFirstObjectByType<PlayerUI>();
        if (playerUI == null) return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        System.Type type = playerUI.GetType();
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Player 1
        Transform p1Panel = canvas.transform.Find("Player1StatsPanel");
        if (p1Panel != null)
        {
            type.GetField("player1StatsPanel", flags)?.SetValue(playerUI, p1Panel.gameObject);
            type.GetField("player1NameText", flags)?.SetValue(playerUI, p1Panel.Find("Player1NameText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("player1HealthBar", flags)?.SetValue(playerUI, p1Panel.Find("Player1HealthBar")?.GetComponent<Slider>());
            type.GetField("player1HealthText", flags)?.SetValue(playerUI, p1Panel.Find("Player1HealthText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("player1PMText", flags)?.SetValue(playerUI, p1Panel.Find("Player1PMText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("player1PAText", flags)?.SetValue(playerUI, p1Panel.Find("Player1PAText")?.GetComponent<TextMeshProUGUI>());
        }

        // Player 2
        Transform p2Panel = canvas.transform.Find("Player2StatsPanel");
        if (p2Panel != null)
        {
            type.GetField("player2StatsPanel", flags)?.SetValue(playerUI, p2Panel.gameObject);
            type.GetField("player2NameText", flags)?.SetValue(playerUI, p2Panel.Find("Player2NameText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("player2HealthBar", flags)?.SetValue(playerUI, p2Panel.Find("Player2HealthBar")?.GetComponent<Slider>());
            type.GetField("player2HealthText", flags)?.SetValue(playerUI, p2Panel.Find("Player2HealthText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("player2PMText", flags)?.SetValue(playerUI, p2Panel.Find("Player2PMText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("player2PAText", flags)?.SetValue(playerUI, p2Panel.Find("Player2PAText")?.GetComponent<TextMeshProUGUI>());
        }

        // Spell Panel
        Transform spellPanel = canvas.transform.Find("SpellPanel");
        if (spellPanel != null)
        {
            type.GetField("spellPanel", flags)?.SetValue(playerUI, spellPanel.gameObject);

            // Botones de hechizos
            var spellButtons = new System.Collections.Generic.List<Button>();
            var spellCostTexts = new System.Collections.Generic.List<TextMeshProUGUI>();
            var spellCooldownOverlays = new System.Collections.Generic.List<Image>();

            for (int i = 0; i < 4; i++)
            {
                Transform btn = spellPanel.Find($"SpellButton{i}");
                if (btn != null)
                {
                    spellButtons.Add(btn.GetComponent<Button>());
                    spellCostTexts.Add(btn.Find("SpellCostText")?.GetComponent<TextMeshProUGUI>());

                    Transform overlay = btn.Find("SpellCooldownOverlay");
                    if (overlay != null)
                        spellCooldownOverlays.Add(overlay.GetComponent<Image>());
                }
            }

            type.GetField("spellButtons", flags)?.SetValue(playerUI, spellButtons);
            type.GetField("spellCostTexts", flags)?.SetValue(playerUI, spellCostTexts);
            type.GetField("spellCooldownOverlays", flags)?.SetValue(playerUI, spellCooldownOverlays);
        }

        // Turn Indicator
        Transform turnPanel = canvas.transform.Find("TurnIndicatorPanel");
        if (turnPanel != null)
        {
            type.GetField("turnIndicatorPanel", flags)?.SetValue(playerUI, turnPanel.gameObject);
            type.GetField("currentTurnText", flags)?.SetValue(playerUI, turnPanel.Find("CurrentTurnText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("turnTimerText", flags)?.SetValue(playerUI, turnPanel.Find("TurnTimerText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("endTurnButton", flags)?.SetValue(playerUI, turnPanel.Find("EndTurnButton")?.GetComponent<Button>());
        }

        // Action Log
        Transform logPanel = canvas.transform.Find("ActionLogPanel");
        if (logPanel != null)
        {
            type.GetField("actionLogPanel", flags)?.SetValue(playerUI, logPanel.gameObject);
            type.GetField("actionLogText", flags)?.SetValue(playerUI, logPanel.Find("ScrollView/Viewport/Content/ActionLogText")?.GetComponent<TextMeshProUGUI>());
            type.GetField("actionLogScrollRect", flags)?.SetValue(playerUI, logPanel.Find("ScrollView")?.GetComponent<ScrollRect>());
        }

        // Game Over
        Transform gameOverPanel = canvas.transform.Find("GameOverPanel");
        if (gameOverPanel != null)
        {
            type.GetField("gameOverPanel", flags)?.SetValue(playerUI, gameOverPanel.gameObject);
            type.GetField("winnerText", flags)?.SetValue(playerUI, gameOverPanel.Find("CenterPanel/WinnerText")?.GetComponent<TextMeshProUGUI>());
        }

        EditorUtility.SetDirty(playerUI);
    }

    // Funciones auxiliares
    static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        return panel;
    }

    static GameObject CreateText(string name, Transform parent, string text, int fontSize, Vector2 anchorPos, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200, 30);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return textObj;
    }

    static GameObject CreateButton(string name, Transform parent, string text)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);

        Image img = button.AddComponent<Image>();
        img.color = new Color(0.3f, 0.5f, 0.8f);

        Button btn = button.AddComponent<Button>();

        GameObject textObj = CreateText("Text", button.transform, text, 16,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }

    static GameObject CreateHealthBar(string name, Transform parent, Vector2 anchorPos, Vector2 position)
    {
        GameObject healthBar = new GameObject(name);
        healthBar.transform.SetParent(parent, false);

        RectTransform rect = healthBar.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(180, 20);

        Slider slider = healthBar.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 200;
        slider.value = 200;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(healthBar.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(healthBar.transform, false);
        RectTransform fillRect = fillArea.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-5, 0);
        fillRect.offsetMin = new Vector2(5, 0);
        fillRect.offsetMax = new Vector2(0, 0);

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillImgRect = fill.AddComponent<RectTransform>();
        fillImgRect.anchorMin = Vector2.zero;
        fillImgRect.anchorMax = new Vector2(1, 1);
        fillImgRect.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.8f, 0.2f, 0.2f);

        slider.fillRect = fillImgRect;

        return healthBar;
    }
#endif
}