using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameStateData gameState;

    [Header("Player References")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;
    private PlayerController player1Controller;
    private PlayerController player2Controller;

    [Header("Turn Management")]
    [SerializeField] private float turnDuration = 60f;
    private float currentTurnTime;
    private bool isGameActive = false;

    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMPro.TextMeshProUGUI winnerText;
    [SerializeField] private TMPro.TextMeshProUGUI turnTimerText;
    [SerializeField] private TMPro.TextMeshProUGUI currentTurnText;

    private bool isNetworkGame = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ← AGREGAR ESTO
            Debug.Log("[GameManager] ✅ Instancia creada y marcada como persistente");
        }
        else if (Instance != this)
        {
            Debug.Log("[GameManager] Destruyendo duplicado");
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[GameManager] Instancia limpiada en OnDestroy");
        }
    }

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (isGameActive)
        {
            UpdateTurnTimer();
            CheckGameOver();
        }
    }

    void InitializeGame()
    {
        gameState = new GameStateData();

        // Obtener datos desde PlayerPrefs
        string player1Name = PlayerPrefs.GetString("Player1Name", "Jugador 1");
        string player2Name = PlayerPrefs.GetString("Player2Name", "Jugador 2");
        isNetworkGame = PlayerPrefs.GetString("IsOnlineMode", "false") == "True";

        gameState.player1.username = player1Name;
        gameState.player2.username = player2Name;

        Debug.Log($"[GameManager] Modo: {(isNetworkGame ? "Online" : "Offline")}");

        // Suscribirse a eventos de red
        if (isNetworkGame && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnTurnChanged += HandleTurnChanged;
            NetworkManager.Instance.OnGameStateUpdated += HandleGameStateUpdate;
        }

        StartCoroutine(SetupPlayers());
    }

    IEnumerator SetupPlayers()
    {
        // Esperar a que el mapa esté listo
        while (MapGenerator.Instance == null || MapGenerator.Instance.GetMapWidth() == 0)
        {
            yield return null;
        }

        while (GridVisualizer.Instance == null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Instanciar jugadores
        Vector3 player1StartPos = MapGenerator.Instance.GetWorldPosition(1, 1);
        player1StartPos.y = 0f;

        GameObject player1Obj = Instantiate(player1Prefab, player1StartPos, Quaternion.identity);
        player1Obj.name = "Player1";
        player1Controller = player1Obj.GetComponent<PlayerController>();

        if (player1Controller != null)
        {
            player1Controller.SetPlayerData(gameState.player1);
            player1Controller.SetPlayerNumber(1);

            int playerNumber = NetworkManager.Instance != null ? NetworkManager.Instance.GetPlayerNumber() : 1;
            player1Controller.SetIsLocalPlayer(playerNumber == 1);
        }

        // Instanciar jugador 2
        Vector3 player2StartPos = MapGenerator.Instance.GetWorldPosition(8, 8);
        player2StartPos.y = 0f;

        GameObject player2Obj = Instantiate(player2Prefab, player2StartPos, Quaternion.identity);
        player2Obj.name = "Player2";
        player2Controller = player2Obj.GetComponent<PlayerController>();

        if (player2Controller != null)
        {
            player2Controller.SetPlayerData(gameState.player2);
            player2Controller.SetPlayerNumber(2);

            int playerNumber = NetworkManager.Instance != null ? NetworkManager.Instance.GetPlayerNumber() : 1;
            player2Controller.SetIsLocalPlayer(playerNumber == 2);
        }

        SetupCameras();
        StartGame();

        Debug.Log("[GameManager] Jugadores configurados correctamente");
    }

    void SetupCameras()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(5, 10, -5);
            mainCamera.transform.rotation = Quaternion.Euler(45, 45, 0);
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60;
        }
    }

    public void StartGame()
    {
        gameState.gameStarted = true;
        isGameActive = true;
        currentTurnTime = turnDuration;

        if (player1Controller != null)
        {
            player1Controller.StartTurn();
        }

        StartTurn(1);

        Debug.Log("[GameManager] ¡Juego iniciado!");
    }

    void StartTurn(int playerNumber)
    {
        gameState.currentTurn = playerNumber;
        currentTurnTime = turnDuration;

        if (playerNumber == 1)
        {
            if (player1Controller != null)
            {
                player1Controller.StartTurn();
            }
            if (player2Controller != null)
            {
                player2Controller.GetPlayerData().isMyTurn = false;
            }
            if (currentTurnText != null)
                currentTurnText.text = $"Turno de: {gameState.player1.username}";
        }
        else
        {
            if (player2Controller != null)
            {
                player2Controller.StartTurn();
            }
            if (player1Controller != null)
            {
                player1Controller.GetPlayerData().isMyTurn = false;
            }
            if (currentTurnText != null)
                currentTurnText.text = $"Turno de: {gameState.player2.username}";
        }

        // ✅ CORREGIDO: Usar async void o esperar el Task
        if (isNetworkGame && NetworkManager.Instance != null)
        {
            // Opción 1: Fire and forget (no esperar)
            _ = NetworkManager.Instance.SendTurnChange(playerNumber);

            // O Opción 2: Crear método async
            // SendTurnChangeAsync(playerNumber);
        }
    }

    public void EndCurrentTurn()
    {
        int nextPlayer = gameState.currentTurn == 1 ? 2 : 1;
        StartTurn(nextPlayer);
    }

    void UpdateTurnTimer()
    {
        currentTurnTime -= Time.deltaTime;

        if (turnTimerText != null)
        {
            turnTimerText.text = $"Tiempo: {Mathf.CeilToInt(currentTurnTime)}s";
        }

        if (currentTurnTime <= 0)
        {
            EndCurrentTurn();
        }
    }

    void HandleTurnChanged(int playerNumber)
    {
        Debug.Log($"[GameManager] Turno cambiado a Jugador {playerNumber} desde red");

        if (gameState.currentTurn != playerNumber)
        {
            gameState.currentTurn = playerNumber;
            UpdateTurnUI(playerNumber);
        }
    }

    void HandleGameStateUpdate(DofusGameState gameState)
    {
        Debug.Log("[GameManager] Estado del juego actualizado desde red");

        if (!string.IsNullOrEmpty(gameState.data))
        {
            var newState = GameStateData.FromJson(gameState.data);
            UpdateGameState(newState);
        }
    }

    void UpdateTurnUI(int playerNumber)
    {
        if (playerNumber == 1)
        {
            if (currentTurnText != null)
                currentTurnText.text = $"Turno de: {gameState.player1.username}";
        }
        else
        {
            if (currentTurnText != null)
                currentTurnText.text = $"Turno de: {gameState.player2.username}";
        }
    }

    void CheckGameOver()
    {
        if (player1Controller != null && !player1Controller.GetPlayerData().IsAlive())
        {
            EndGame(gameState.player2.username);
        }
        else if (player2Controller != null && !player2Controller.GetPlayerData().IsAlive())
        {
            EndGame(gameState.player1.username);
        }
    }

    void EndGame(string winnerName)
    {
        isGameActive = false;
        gameState.gameEnded = true;
        gameState.winner = winnerName;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (winnerText != null)
            {
                winnerText.text = $"¡{winnerName} ha ganado!";
            }
        }

        Debug.Log($"[GameManager] Juego terminado. Ganador: {winnerName}");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ✅ CORREGIR TAMBIÉN en ReturnToMenu()
    public async void ReturnToMenu() // ← Cambiar a async void
    {
        if (NetworkManager.Instance != null)
        {
            await NetworkManager.Instance.LeaveRoom(); // ← Ahora SÍ espera
        }

        SceneManager.LoadScene("LoginScene");
    }

    public GameStateData GetGameState()
    {
        return gameState;
    }

    public void UpdateGameState(GameStateData newState)
    {
        gameState = newState;

        if (player1Controller != null)
            player1Controller.SetPlayerData(gameState.player1);
        if (player2Controller != null)
            player2Controller.SetPlayerData(gameState.player2);
    }

    // ✅ CORREGIDO: Usar ShowDamage() en lugar de ShowDamageNumber()
    public void ApplyDamageAt(Vector2Int targetPos, int damage)
    {
        PlayerController targetPlayer = GetPlayerAtPosition(targetPos);

        if (targetPlayer != null)
        {
            targetPlayer.TakeDamage(damage);
            Debug.Log($"[GameManager] Daño de {damage} aplicado en posición {targetPos}");

            // ✅ CORREGIDO: Usar ShowDamage()
            if (VisualEffectsManager.Instance != null)
            {
                Vector3 worldPos = MapGenerator.Instance.GetWorldPosition(targetPos.x, targetPos.y);
                worldPos.y = 1f; // Elevar el popup para que sea visible
                VisualEffectsManager.Instance.ShowDamage(worldPos, damage);
            }
        }
        else
        {
            Debug.Log($"[GameManager] No hay jugador en posición {targetPos}");
        }
    }

    public List<PlayerController> GetPlayersInArea(Vector2Int centerPos, int radius)
    {
        List<PlayerController> playersInArea = new List<PlayerController>();

        if (player1Controller != null)
        {
            Vector2Int p1Pos = player1Controller.GetPlayerData().gridPosition;
            int distance = Mathf.Abs(p1Pos.x - centerPos.x) + Mathf.Abs(p1Pos.y - centerPos.y);

            if (distance <= radius)
            {
                playersInArea.Add(player1Controller);
            }
        }

        if (player2Controller != null)
        {
            Vector2Int p2Pos = player2Controller.GetPlayerData().gridPosition;
            int distance = Mathf.Abs(p2Pos.x - centerPos.x) + Mathf.Abs(p2Pos.y - centerPos.y);

            if (distance <= radius)
            {
                playersInArea.Add(player2Controller);
            }
        }

        Debug.Log($"[GameManager] {playersInArea.Count} jugadores encontrados en área centrada en {centerPos} con radio {radius}");
        return playersInArea;
    }

    public PlayerController GetPlayerAtPosition(Vector2Int position)
    {
        if (player1Controller != null && player1Controller.GetPlayerData().gridPosition == position)
        {
            return player1Controller;
        }

        if (player2Controller != null && player2Controller.GetPlayerData().gridPosition == position)
        {
            return player2Controller;
        }

        return null;
    }

    public PlayerController GetPlayerController(int playerNumber)
    {
        return playerNumber == 1 ? player1Controller : player2Controller;
    }
}