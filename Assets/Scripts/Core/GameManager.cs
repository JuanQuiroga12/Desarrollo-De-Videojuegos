using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Agregar estas variables al inicio de GameManager
    private float serverTurnStartTime = 0f;
    private bool isSyncingTime = false;

    [Header("Game State")]
    [SerializeField] private GameStateData gameState;

    [Header("Player References")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;
    private PlayerController player1Controller;
    private PlayerController player2Controller;
    public PlayerUI playerUI;

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



    async void InitializeGame()
    {
        gameState = new GameStateData();

        string player1Name = PlayerPrefs.GetString("Player1Name", "Jugador 1");
        string player2Name = PlayerPrefs.GetString("Player2Name", "Jugador 2");
        isNetworkGame = PlayerPrefs.GetString("IsOnlineMode", "false") == "True";

        gameState.player1.username = player1Name;
        gameState.player2.username = player2Name;

        Debug.Log($"[GameManager] Modo: {(isNetworkGame ? "Online" : "Offline")}");

        // ✅ NUEVO: Esperar sincronización completa antes de continuar
        if (isNetworkGame && NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnTurnChanged += HandleTurnChanged;
            NetworkManager.Instance.OnGameStateUpdated += HandleGameStateUpdate;

            // Esperar a que se sincronicen los nombres
            await WaitForPlayersSync();
        }

        StartCoroutine(SetupPlayers());
    }

    // ✅ NUEVO MÉTODO: Retornar lista de todos los jugadores
    public List<PlayerController> GetAllPlayers()
    {
        List<PlayerController> players = new List<PlayerController>();

        if (player1Controller != null)
        {
            players.Add(player1Controller);
        }

        if (player2Controller != null)
        {
            players.Add(player2Controller);
        }

        return players;
    }

    // ✅ NUEVO MÉTODO: Verificar si todos los jugadores están listos
    public bool ArePlayersInitialized()
    {
        return player1Controller != null &&
               player2Controller != null &&
               player1Controller.GetPlayerData() != null &&
               player2Controller.GetPlayerData() != null;
    }

    // ✅ NUEVO MÉTODO: Esperar sincronización completa
    async System.Threading.Tasks.Task WaitForPlayersSync()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.currentRoomRef == null)
            return;

        Debug.Log("[GameManager] Esperando sincronización de jugadores...");

        int attempts = 0;
        while (attempts < 20) // Máximo 10 segundos
        {
            try
            {
                var snapshot = await NetworkManager.Instance.currentRoomRef.GetValueAsync();

                if (snapshot.Exists)
                {
                    var roomData = DofusRoomData.FromSnapshot(snapshot);

                    if (!string.IsNullOrEmpty(roomData.player1Name))
                    {
                        gameState.player1.username = roomData.player1Name;
                    }

                    if (!string.IsNullOrEmpty(roomData.player2Name))
                    {
                        gameState.player2.username = roomData.player2Name;
                        Debug.Log($"[GameManager] ✅ Player2 sincronizado: {roomData.player2Name}");
                        break; // Ambos nombres están listos
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Error sincronizando: {e.Message}");
            }

            await System.Threading.Tasks.Task.Delay(500);
            attempts++;
        }

        Debug.Log($"[GameManager] Jugadores sincronizados - P1: {gameState.player1.username}, P2: {gameState.player2.username}");
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

        // ✅ NUEVO: Esperar a GameInitializationManager
        if (GameInitializationManager.Instance != null)
        {
            while (!GameInitializationManager.Instance.IsInitialized())
            {
                Debug.Log("[GameManager] ⏳ Esperando inicialización completa...");
                yield return new WaitForSeconds(0.5f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        const float PLAYER_SPAWN_HEIGHT = 0.41f;

        // ========== CONFIGURAR JUGADOR 1 ==========
        Vector3 player1StartPos = MapGenerator.Instance.GetWorldPosition(1, 1);
        player1StartPos.y = PLAYER_SPAWN_HEIGHT;

        GameObject player1Obj = Instantiate(player1Prefab, player1StartPos, Quaternion.identity);
        player1Obj.name = "Player1";
        player1Controller = player1Obj.GetComponent<PlayerController>();

        if (player1Controller != null)
        {
            player1Controller.SetPlayerData(gameState.player1);
            player1Controller.SetPlayerNumber(1);

            if (isNetworkGame && NetworkManager.Instance != null)
            {
                int myPlayerNumber = NetworkManager.Instance.GetPlayerNumber();
                player1Controller.SetIsLocalPlayer(myPlayerNumber == 1);
                player1Controller.isLocalPlayer = (myPlayerNumber == 1);
            }
            else
            {
                player1Controller.SetIsLocalPlayer(true);
                player1Controller.isLocalPlayer = true;
            }

            if (player1Obj.GetComponent<PlayerMovementSystem>() == null)
            {
                player1Obj.AddComponent<PlayerMovementSystem>();
            }
            if (player1Obj.GetComponent<SpellCastingSystem>() == null)
            {
                player1Obj.AddComponent<SpellCastingSystem>();
            }
        }

        // ========== CONFIGURAR JUGADOR 2 ==========
        Vector3 player2StartPos = MapGenerator.Instance.GetWorldPosition(8, 8);
        player2StartPos.y = PLAYER_SPAWN_HEIGHT;

        GameObject player2Obj = Instantiate(player2Prefab, player2StartPos, Quaternion.identity);
        player2Obj.name = "Player2";
        player2Controller = player2Obj.GetComponent<PlayerController>();

        if (player2Controller != null)
        {
            player2Controller.SetPlayerData(gameState.player2);
            player2Controller.SetPlayerNumber(2);

            if (isNetworkGame && NetworkManager.Instance != null)
            {
                int myPlayerNumber = NetworkManager.Instance.GetPlayerNumber();
                player2Controller.SetIsLocalPlayer(myPlayerNumber == 2);
                player2Controller.isLocalPlayer = (myPlayerNumber == 2);
            }
            else
            {
                player2Controller.SetIsLocalPlayer(false);
                player2Controller.isLocalPlayer = false;
            }

            if (player2Obj.GetComponent<PlayerMovementSystem>() == null)
            {
                player2Obj.AddComponent<PlayerMovementSystem>();
            }
            if (player2Obj.GetComponent<SpellCastingSystem>() == null)
            {
                player2Obj.AddComponent<SpellCastingSystem>();
            }
        }

        SetupCameras();

        // ✅ ESPERAR UN FRAME ADICIONAL para que los sistemas se inicialicen
        yield return null;

        StartGame();

        Debug.Log("[GameManager] ✅ Jugadores configurados correctamente");
        Debug.Log($"    - Modo: {(isNetworkGame ? "Online" : "Offline")}");
        Debug.Log($"    - Player1 Local: {player1Controller?.isLocalPlayer}");
        Debug.Log($"    - Player2 Local: {player2Controller?.isLocalPlayer}");
    }

    // Reemplazar el método SyncTurnTimer para que sea un IEnumerator normal (corutina de Unity)
    // El error CS1624 ocurre porque no se puede usar 'async Task<IEnumerator>' con 'yield return'.
    // Solución: Cambiar la firma a 'private IEnumerator SyncTurnTimer()' y usar corutinas normales.

    private IEnumerator SyncTurnTimer()
    {
        while (isGameActive)
        {
            if (isNetworkGame && NetworkManager.Instance != null)
            {
                // Sincronizar cada 2 segundos
                if (!isSyncingTime)
                {
                    isSyncingTime = true;

                    // Si soy el host, envío el tiempo
                    if (NetworkManager.Instance.isHost)
                    {
                        var updates = new Dictionary<string, object>
                        {
                            { "gameState/turnTimeRemaining", currentTurnTime },
                            { "gameState/serverTime", Firebase.Database.ServerValue.Timestamp }
                        };

                        // Usar una tarea y esperar a que termine (no se puede usar await en corutinas, así que solo lanzamos la tarea)
                        NetworkManager.Instance.currentRoomRef.UpdateChildrenAsync(updates);
                    }
                    // Si no soy host, leo el tiempo
                    else
                    {
                        var task = NetworkManager.Instance.currentRoomRef
                            .Child("gameState/turnTimeRemaining").GetValueAsync();

                        while (!task.IsCompleted)
                            yield return null;

                        if (task.Exception == null && task.Result.Exists)
                        {
                            float serverTime = Convert.ToSingle(task.Result.Value);
                            currentTurnTime = serverTime;
                        }
                    }

                    isSyncingTime = false;
                }
            }

            yield return new WaitForSeconds(2f);
        }
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



    // Modificar StartGame() para iniciar la sincronización
    public void StartGame()
    {
        gameState.gameStarted = true;
        isGameActive = true;
        currentTurnTime = turnDuration;

        // ❌ ELIMINADO: Esta línea causaba que StartTurn() se llamara DENTRO de SetupPlayers()
        // if (player1Controller != null)
        // {
        //     player1Controller.StartTurn();
        // }

        // ✅ ÚNICA LLAMADA - maneja todo el setup del primer turno
        StartTurn(1);

        if (isNetworkGame)
        {
            StartCoroutine(SyncTurnTimer());
        }

        Debug.Log("[GameManager] ¡Juego iniciado!");
    }

    void StartTurn(int playerNumber)
    {
        if (playerNumber != 1 && playerNumber != 2)
        {
            Debug.LogError($"[GameManager] Número de jugador inválido: {playerNumber}");
            playerNumber = 1;
        }

        gameState.currentTurn = playerNumber;
        currentTurnTime = turnDuration;

        Debug.Log($"[GameManager] ========== INICIANDO TURNO JUGADOR {playerNumber} ==========");

        if (GridVisualizer.Instance != null)
        {
            GridVisualizer.Instance.ResetGridColors();
        }

        PlayerController playerToActivate = GetPlayerController(playerNumber);
        PlayerController playerToDeactivate = GetPlayerController(playerNumber == 1 ? 2 : 1);

        // ✅ PASO 1: Desactivar jugador anterior PRIMERO
        if (playerToDeactivate != null)
        {
            playerToDeactivate.GetPlayerData().isMyTurn = false;

            PlayerTurnIndicator indicator = playerToDeactivate.GetComponent<PlayerTurnIndicator>();
            if (indicator != null)
            {
                indicator.SetActive(false);
            }

            // ✅ DESHABILITAR sistemas de input explícitamente
            PlayerMovementSystem moveSystem = playerToDeactivate.GetComponent<PlayerMovementSystem>();
            if (moveSystem != null)
            {
                moveSystem.enabled = false;
            }

            SpellCastingSystem spellSystem = playerToDeactivate.GetComponent<SpellCastingSystem>();
            if (spellSystem != null)
            {
                spellSystem.enabled = false;
                spellSystem.CancelSpellSelection();
            }

            Debug.Log($"[GameManager] ❌ Desactivado: {playerToDeactivate.GetPlayerData().username}");
        }

        // ✅ PASO 2: Activar jugador actual
        if (playerToActivate != null)
        {
            // ✅ CRÍTICO: Asegurar que isMyTurn esté en TRUE ANTES de StartTurn()
            playerToActivate.GetPlayerData().isMyTurn = true;

            // Esperar un frame para que el estado se propague
            StartCoroutine(ActivatePlayerNextFrame(playerToActivate, playerNumber));
        }

        // Actualizar UI
        if (currentTurnText != null)
        {
            string playerName = (playerNumber == 1) ? gameState.player1.username : gameState.player2.username;
            currentTurnText.text = $"Turno de: {playerName}";
        }

        // Sincronizar con red
        if (isNetworkGame && NetworkManager.Instance != null)
        {
            _ = NetworkManager.Instance.SendTurnChange(playerNumber);
        }
    }

    // ✅ NUEVO: Activar jugador en el siguiente frame
    private IEnumerator ActivatePlayerNextFrame(PlayerController player, int playerNumber)
    {
        yield return null; // Esperar un frame

        player.StartTurn();

        if (playerUI != null)
        {
            if (player1Controller != null)
                playerUI.UpdatePlayerStats(player1Controller.GetPlayerData());
            if (player2Controller != null)
                playerUI.UpdatePlayerStats(player2Controller.GetPlayerData());
        }

        Debug.Log($"[GameManager] ✅ Activado: {player.GetPlayerData().username}");
        Debug.Log($"    - isMyTurn: {player.GetPlayerData().isMyTurn}");
        Debug.Log($"    - isLocalPlayer: {player.isLocalPlayer}");
    }

    public void EndCurrentTurn()
    {
        Debug.Log($"[GameManager] Terminando turno del jugador {gameState.currentTurn}");

        // ✅ CORREGIDO: Cambiar turno ANTES de llamar StartTurn
        int currentPlayer = gameState.currentTurn;
        int nextPlayer = (currentPlayer == 1) ? 2 : 1;

        // Terminar turno del jugador actual
        PlayerController currentController = GetPlayerController(currentPlayer);
        if (currentController != null)
        {
            currentController.GetPlayerData().isMyTurn = false;
            PlayerTurnIndicator indicator = currentController.GetComponent<PlayerTurnIndicator>();
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        // Actualizar estado del juego
        gameState.currentTurn = nextPlayer;

        // Iniciar turno del siguiente jugador
        StartTurn(nextPlayer);

        Debug.Log($"[GameManager] ✅ Turno cambiado a jugador {nextPlayer}");
    }

    // ✅ REEMPLAZAR UpdateTurnTimer() con sincronización mejorada
    void UpdateTurnTimer()
    {
        if (!isGameActive) return;

        currentTurnTime -= Time.deltaTime;

        if (turnTimerText != null)
        {
            turnTimerText.text = $"Tiempo: {Mathf.CeilToInt(currentTurnTime)}s";
        }

        if (currentTurnTime <= 0)
        {
            EndCurrentTurn();
        }

        // ✅ Sincronizar tiempo cada 2 segundos
        if (isNetworkGame && NetworkManager.Instance != null)
        {
            timeSyncCounter += Time.deltaTime;
            if (timeSyncCounter >= 2f)
            {
                timeSyncCounter = 0f;
                _ = SyncTimeWithNetwork();
            }
        }
    }

    // ✅ NUEVO: Variable para contador de sincronización
    private float timeSyncCounter = 0f;

    // ✅ NUEVO: Método de sincronización de tiempo
    async System.Threading.Tasks.Task SyncTimeWithNetwork()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.currentRoomRef == null)
            return;

        if (NetworkManager.Instance.isHost)
        {
            // Host envía el tiempo actual
            var updates = new Dictionary<string, object>
        {
            { "gameState/turnTimeRemaining", currentTurnTime },
            { "gameState/currentTurn", gameState.currentTurn }
        };

            await NetworkManager.Instance.currentRoomRef.UpdateChildrenAsync(updates);
        }
        else
        {
            // Cliente lee el tiempo del host
            var snapshot = await NetworkManager.Instance.currentRoomRef.Child("gameState/turnTimeRemaining").GetValueAsync();

            if (snapshot.Exists)
            {
                float serverTime = System.Convert.ToSingle(snapshot.Value);
                // Solo sincronizar si la diferencia es mayor a 1 segundo
                if (Mathf.Abs(currentTurnTime - serverTime) > 1f)
                {
                    currentTurnTime = serverTime;
                }
            }
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
