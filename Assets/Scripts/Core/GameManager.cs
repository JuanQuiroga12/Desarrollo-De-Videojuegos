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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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

        // Obtener nombres de usuario del sistema de login
        if (LoginManager.Instance != null)
        {
            gameState.player1.username = LoginManager.Instance.GetPlayer1Name();
            gameState.player2.username = LoginManager.Instance.GetPlayer2Name();
        }
        else
        {
            gameState.player1.username = "Jugador 1";
            gameState.player2.username = "Jugador 2";
        }

        // Esperar a que el mapa esté generado
        StartCoroutine(SetupPlayers());
    }

    IEnumerator SetupPlayers()
    {
        // Esperar a que el mapa esté listo
        while (MapGenerator.Instance == null || MapGenerator.Instance.GetMapWidth() == 0)
        {
            yield return null;
        }

        // Esperar un frame adicional para asegurarse de que todo está inicializado
        yield return new WaitForSeconds(0.5f);

        // Validar que MapGenerator está disponible
        if (MapGenerator.Instance == null)
        {
            Debug.LogError("MapGenerator.Instance es null después de esperar!");
            yield break;
        }

        // Instanciar jugador 1
        Vector3 player1StartPos = MapGenerator.Instance.GetWorldPosition(1, 1);
        GameObject player1Obj = Instantiate(player1Prefab, player1StartPos, Quaternion.identity);
        player1Controller = player1Obj.GetComponent<PlayerController>();

        if (player1Controller != null)
        {
            player1Controller.SetPlayerData(gameState.player1);
        }
        else
        {
            Debug.LogError("Player1Controller component no encontrado!");
        }

        // Instanciar jugador 2
        Vector3 player2StartPos = MapGenerator.Instance.GetWorldPosition(8, 8);
        GameObject player2Obj = Instantiate(player2Prefab, player2StartPos, Quaternion.identity);
        player2Controller = player2Obj.GetComponent<PlayerController>();

        if (player2Controller != null)
        {
            player2Controller.SetPlayerData(gameState.player2);
        }
        else
        {
            Debug.LogError("Player2Controller component no encontrado!");
        }

        // Configurar cámaras si es necesario
        SetupCameras();

        // Iniciar el juego
        StartGame();
    }

    void SetupCameras()
    {
        // Configurar cámara isométrica
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

        // Iniciar con el turno del jugador 1
        StartTurn(1);
    }

    void StartTurn(int playerNumber)
    {
        gameState.currentTurn = playerNumber;
        currentTurnTime = turnDuration;

        if (playerNumber == 1)
        {
            player1Controller.StartTurn();
            if (currentTurnText != null)
                currentTurnText.text = $"Turno de: {gameState.player1.username}";
        }
        else
        {
            player2Controller.StartTurn();
            if (currentTurnText != null)
                currentTurnText.text = $"Turno de: {gameState.player2.username}";
        }

        // Si es un juego en red, sincronizar el estado
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsNetworkGame())
        {
            NetworkManager.Instance.SendGameState(gameState);
        }
    }

    public void EndCurrentTurn()
    {
        // Cambiar al siguiente jugador
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
            // Tiempo agotado, cambiar turno
            EndCurrentTurn();
        }
    }

    public void ApplyDamageAt(Vector2Int position, int damage)
    {
        // Verificar si hay un jugador en esa posición
        if (player1Controller.GetPlayerData().gridPosition == position)
        {
            player1Controller.TakeDamage(damage);

            // Efecto visual de daño
            ShowDamageEffect(MapGenerator.Instance.GetWorldPosition(position.x, position.y), damage);
        }
        else if (player2Controller.GetPlayerData().gridPosition == position)
        {
            player2Controller.TakeDamage(damage);

            // Efecto visual de daño
            ShowDamageEffect(MapGenerator.Instance.GetWorldPosition(position.x, position.y), damage);
        }
    }

    void ShowDamageEffect(Vector3 position, int damage)
    {
        // Aquí puedes instanciar un efecto de partículas o texto flotante
        // Por ahora solo un log
        Debug.Log($"¡Daño de {damage} puntos en posición {position}!");
    }

    public List<PlayerController> GetPlayersInArea(Vector2Int center, int radius)
    {
        List<PlayerController> playersInArea = new List<PlayerController>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int checkPos = new Vector2Int(center.x + x, center.y + y);

                if (player1Controller.GetPlayerData().gridPosition == checkPos)
                {
                    playersInArea.Add(player1Controller);
                }
                if (player2Controller.GetPlayerData().gridPosition == checkPos)
                {
                    playersInArea.Add(player2Controller);
                }
            }
        }

        return playersInArea;
    }

    void CheckGameOver()
    {
        if (!player1Controller.GetPlayerData().IsAlive())
        {
            EndGame(gameState.player2.username);
        }
        else if (!player2Controller.GetPlayerData().IsAlive())
        {
            EndGame(gameState.player1.username);
        }
    }

    void EndGame(string winnerName)
    {
        isGameActive = false;
        gameState.gameEnded = true;
        gameState.winner = winnerName;

        // Mostrar panel de fin de juego
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (winnerText != null)
            {
                winnerText.text = $"¡{winnerName} ha ganado!";
            }
        }

        // Guardar estadísticas si es necesario
        SaveGameStats();
    }

    void SaveGameStats()
    {
        // Aquí puedes implementar el guardado usando Firebase como se muestra en el documento
        string gameStateJson = gameState.ToJson();
        PlayerPrefs.SetString("LastGameState", gameStateJson);
        PlayerPrefs.Save();

        Debug.Log("Estado del juego guardado: " + gameStateJson);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("LoginScene");
    }

    public GameStateData GetGameState()
    {
        return gameState;
    }

    public void UpdateGameState(GameStateData newState)
    {
        gameState = newState;

        // Actualizar controladores con los nuevos datos
        player1Controller.SetPlayerData(gameState.player1);
        player2Controller.SetPlayerData(gameState.player2);
    }
}