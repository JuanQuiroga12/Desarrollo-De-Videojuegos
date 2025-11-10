using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Firebase")]
    private FirebaseAuth auth;
    private DatabaseReference databaseRef;
    private FirebaseDatabase database;
    private bool firebaseInitialized = false; // ✅ NUEVO

    [Header("Room Settings")]
    public string currentRoomId;
    public bool isHost = false;
    public string playerId;
    public int playerNumber = -1; // 1 o 2

    // ✅ Eventos
    public event Action<DofusRoomData> OnRoomUpdated;
    public event Action<string> OnPlayerJoined;
    public event Action<string> OnPlayerLeft;
    public event Action OnGameStarted;
    public event Action<DofusGameState> OnGameStateUpdated;
    public event Action<int> OnTurnChanged;

    [Header("References")]
    private DatabaseReference roomsRef;
    public DatabaseReference currentRoomRef;
    private List<System.Object> roomListeners = new List<System.Object>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        Debug.Log("[NetworkManager] 🔥 Iniciando Firebase...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                database = FirebaseDatabase.DefaultInstance;
                databaseRef = database.RootReference;
                roomsRef = databaseRef.Child("dofus_rooms");
                auth = FirebaseAuth.DefaultInstance;

                firebaseInitialized = true; // ✅ MARCAR COMO INICIALIZADO

                SignInAnonymously();

                Debug.Log("[NetworkManager] ✅ Firebase inicializado correctamente");
            }
            else
            {
                Debug.LogError($"[NetworkManager] ❌ No se pudo inicializar Firebase: {task.Result}");
                firebaseInitialized = false;
            }
        });
    }

    // ✅ NUEVO MÉTODO PÚBLICO
    public bool IsFirebaseReady()
    {
        return firebaseInitialized && auth != null && databaseRef != null;
    }

    // ✅ ESPERAR AUTENTICACIÓN
    private async Task<bool> WaitForAuthentication(float timeout = 10f)
    {
        float elapsed = 0f;

        while (string.IsNullOrEmpty(playerId) && elapsed < timeout)
        {
            await Task.Delay(100);
            elapsed += 0.1f;
        }

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[NetworkManager] ⏱️ Timeout esperando autenticación");
            return false;
        }

        return true;
    }

    private void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[NetworkManager] ❌ Error al autenticar: " + task.Exception);
                return;
            }

            playerId = auth.CurrentUser.UserId;
            Debug.Log($"[NetworkManager] ✅ Usuario autenticado: {playerId}");
        });
    }

    // ✅ CREAR SALA
    public async Task<string> CreateRoom(string hostName)
    {
        // Esperar autenticación
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.Log("[NetworkManager] ⏳ Esperando autenticación...");
            bool authenticated = await WaitForAuthentication();

            if (!authenticated)
            {
                Debug.LogError("[NetworkManager] ❌ No se pudo autenticar al usuario");
                return null;
            }

            Debug.Log("[NetworkManager] ✅ Usuario autenticado exitosamente");
        }

        // Validar que no esté ya en una sala
        if (!string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogWarning($"[NetworkManager] Ya estás en la sala {currentRoomId}");
            return currentRoomId;
        }

        // Crear nueva sala
        string roomId = GenerateRoomId();
        currentRoomRef = roomsRef.Child(roomId);
        currentRoomId = roomId;
        isHost = true;
        playerNumber = 1;

        DofusRoomData roomData = new DofusRoomData
        {
            roomId = roomId,
            hostId = playerId,
            hostName = hostName,
            player1Id = playerId,
            player1Name = hostName,
            player2Id = "",
            player2Name = "",
            player1Ready = false,
            player2Ready = false,
            gameStarted = false,
            createdAt = ServerValue.Timestamp
        };

        try
        {
            await currentRoomRef.SetValueAsync(roomData.ToDictionary());
            SetupRoomListeners();

            // Guardar en PlayerPrefs
            PlayerPrefs.SetString("CurrentRoomId", roomId);
            PlayerPrefs.SetString("CurrentUserId", playerId);
            PlayerPrefs.SetString("Player1Name", hostName);
            PlayerPrefs.SetString("PlayerNumber", "1");
            PlayerPrefs.SetString("IsOnlineMode", "True");
            PlayerPrefs.Save();

            Debug.Log($"[NetworkManager] ✅ Sala creada: {roomId}");
            return roomId;
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] ❌ Error al crear sala: {e.Message}");

            // Limpiar estado
            currentRoomId = null;
            currentRoomRef = null;
            isHost = false;
            playerNumber = -1;

            return null;
        }
    }

    // ✅ UNIRSE A SALA
    public async Task<string> JoinRoom(string roomId = null, string playerName = null)
    {
        // Esperar autenticación
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.Log("[NetworkManager] ⏳ Esperando autenticación...");
            bool authenticated = await WaitForAuthentication();

            if (!authenticated)
            {
                Debug.LogError("[NetworkManager] ❌ No se pudo autenticar al usuario");
                return null;
            }
        }

        // Si no se especifica sala, buscar una disponible
        if (string.IsNullOrEmpty(roomId))
        {
            var availableRoom = await FindAvailableRoom();
            if (availableRoom == null)
            {
                Debug.Log("[NetworkManager] No hay salas disponibles");
                return null;
            }
            roomId = availableRoom.roomId;
        }

        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogError("[NetworkManager] ❌ roomId es null o vacío");
            return null;
        }

        // Validar que no estemos ya en una sala
        if (!string.IsNullOrEmpty(currentRoomId))
        {
            if (currentRoomId == roomId)
            {
                Debug.LogWarning($"[NetworkManager] Ya estás en la sala {roomId}");
                return roomId;
            }
            else
            {
                Debug.LogWarning($"[NetworkManager] Saliendo de sala {currentRoomId}...");
                await LeaveRoom();
            }
        }

        currentRoomId = roomId;
        currentRoomRef = roomsRef.Child(roomId);

        // Obtener datos de la sala
        var roomSnapshot = await currentRoomRef.GetValueAsync();
        if (!roomSnapshot.Exists)
        {
            Debug.LogError($"[NetworkManager] ❌ La sala {roomId} no existe");
            currentRoomId = null;
            currentRoomRef = null;
            return null;
        }

        var roomData = DofusRoomData.FromSnapshot(roomSnapshot);

        if (roomData.gameStarted)
        {
            Debug.LogWarning("[NetworkManager] ⚠️ El juego ya ha comenzado");
            currentRoomId = null;
            currentRoomRef = null;
            return null;
        }

        if (!string.IsNullOrEmpty(roomData.player2Id))
        {
            Debug.LogWarning("[NetworkManager] ⚠️ La sala está llena");
            currentRoomId = null;
            currentRoomRef = null;
            return null;
        }

        // Unirse como jugador 2
        playerNumber = 2;
        string finalPlayerName = string.IsNullOrEmpty(playerName) ? "Jugador 2" : playerName;

        await currentRoomRef.Child("player2Id").SetValueAsync(playerId);
        await currentRoomRef.Child("player2Name").SetValueAsync(finalPlayerName);

        SetupRoomListeners();

        // Guardar en PlayerPrefs
        PlayerPrefs.SetString("CurrentRoomId", roomId);
        PlayerPrefs.SetString("CurrentUserId", playerId);
        PlayerPrefs.SetString("Player1Name", roomData.player1Name);
        PlayerPrefs.SetString("Player2Name", finalPlayerName);
        PlayerPrefs.SetString("PlayerNumber", "2");
        PlayerPrefs.SetString("IsOnlineMode", "True");
        PlayerPrefs.Save();

        Debug.Log($"[NetworkManager] ✅ Unido a sala: {roomId} como {finalPlayerName}");
        return roomId;
    }

    // ✅ BUSCAR SALA DISPONIBLE
    private async Task<DofusRoomData> FindAvailableRoom()
    {
        var snapshot = await roomsRef.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.Log("[NetworkManager] No hay salas en Firebase");
            return null;
        }

        foreach (var roomSnapshot in snapshot.Children)
        {
            var roomData = DofusRoomData.FromSnapshot(roomSnapshot);

            if (string.IsNullOrEmpty(roomData.roomId))
                continue;

            if (!roomData.gameStarted && string.IsNullOrEmpty(roomData.player2Id))
            {
                Debug.Log($"[NetworkManager] ✅ Sala disponible: {roomData.roomId}");
                return roomData;
            }
        }

        Debug.Log("[NetworkManager] No hay salas disponibles");
        return null;
    }

    // ✅ LISTENERS
    private void SetupRoomListeners()
    {
        if (currentRoomRef == null)
            return;

        currentRoomRef.ValueChanged += HandleRoomValueChanged;
        roomListeners.Add(currentRoomRef);

        currentRoomRef.Child("gameState").ValueChanged += HandleGameStateChanged;
        roomListeners.Add(currentRoomRef.Child("gameState"));
    }

    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError($"[NetworkManager] ❌ Error en sala: {e.DatabaseError.Message}");
            return;
        }

        if (e.Snapshot.Exists)
        {
            var roomData = DofusRoomData.FromSnapshot(e.Snapshot);
            OnRoomUpdated?.Invoke(roomData);

            if (roomData.gameStarted)
            {
                Debug.Log("[NetworkManager] 🎮 ¡Juego iniciado!");
                OnGameStarted?.Invoke();
            }
        }
    }

    private void HandleGameStateChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null || !e.Snapshot.Exists)
            return;

        var gameState = DofusGameState.FromSnapshot(e.Snapshot);
        OnGameStateUpdated?.Invoke(gameState);

        if (e.Snapshot.Child("currentTurn").Exists)
        {
            int turn = Convert.ToInt32(e.Snapshot.Child("currentTurn").Value);
            OnTurnChanged?.Invoke(turn);
        }
    }

    // ✅ MARCAR JUGADOR LISTO
    public async Task SetPlayerReady()
    {
        if (currentRoomRef == null)
            return;

        string readyField = playerNumber == 1 ? "player1Ready" : "player2Ready";
        await currentRoomRef.Child(readyField).SetValueAsync(true);

        Debug.Log($"[NetworkManager] ✅ Jugador {playerNumber} listo");
    }

    // ✅ INICIAR JUEGO (SOLO HOST)
    public async Task StartGame()
    {
        if (!isHost)
        {
            Debug.LogWarning("[NetworkManager] ⚠️ Solo el host puede iniciar");
            return;
        }

        await currentRoomRef.Child("gameStarted").SetValueAsync(true);
        await currentRoomRef.Child("gameState").Child("currentTurn").SetValueAsync(1);

        Debug.Log("[NetworkManager] ✅ Juego iniciado");
    }

    // ✅ SINCRONIZAR ESTADO
    public async Task SendGameState(GameStateData gameState)
    {
        if (currentRoomRef == null)
            return;

        string json = JsonUtility.ToJson(gameState);
        await currentRoomRef.Child("gameState").Child("data").SetValueAsync(json);
    }

    public async Task SendTurnChange(int nextPlayer)
    {
        if (currentRoomRef == null)
            return;

        await currentRoomRef.Child("gameState").Child("currentTurn").SetValueAsync(nextPlayer);
    }

    // ✅ SALIR DE SALA
    public async Task LeaveRoom()
    {
        if (string.IsNullOrEmpty(currentRoomId))
            return;

        if (isHost)
        {
            await currentRoomRef.RemoveValueAsync();
        }
        else
        {
            await currentRoomRef.Child("player2Id").SetValueAsync("");
            await currentRoomRef.Child("player2Name").SetValueAsync("");
        }

        // Limpiar listeners
        foreach (var listener in roomListeners)
        {
            if (listener is DatabaseReference dbRef)
            {
                dbRef.ValueChanged -= HandleRoomValueChanged;
                dbRef.ValueChanged -= HandleGameStateChanged;
            }
        }
        roomListeners.Clear();

        currentRoomId = null;
        currentRoomRef = null;
        isHost = false;
        playerNumber = -1;
    }

    private string GenerateRoomId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        var result = new char[6];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new string(result);
    }

    // ✅ MÉTODOS PÚBLICOS
    public bool IsOnlineMode() => !string.IsNullOrEmpty(currentRoomId);
    public bool IsConnected() => currentRoomRef != null;
    public string GetRoomId() => currentRoomId;
    public string GetUserId() => playerId;
    public int GetPlayerNumber() => playerNumber;

    void OnDestroy()
    {
        LeaveRoom().ContinueWith(task => { });
    }
}

// ✅ CLASES DE DATOS
[Serializable]
public class DofusRoomData
{
    public string roomId;
    public string hostId;
    public string hostName;
    public string player1Id;
    public string player1Name;
    public string player2Id;
    public string player2Name;
    public bool player1Ready;
    public bool player2Ready;
    public bool gameStarted;
    public object createdAt;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            {"roomId", roomId},
            {"hostId", hostId},
            {"hostName", hostName},
            {"player1Id", player1Id},
            {"player1Name", player1Name},
            {"player2Id", player2Id},
            {"player2Name", player2Name},
            {"player1Ready", player1Ready},
            {"player2Ready", player2Ready},
            {"gameStarted", gameStarted},
            {"createdAt", createdAt}
        };
    }

    public static DofusRoomData FromSnapshot(DataSnapshot snapshot)
    {
        return new DofusRoomData
        {
            roomId = snapshot.Child("roomId").Value?.ToString(),
            hostId = snapshot.Child("hostId").Value?.ToString(),
            hostName = snapshot.Child("hostName").Value?.ToString() ?? "Host",
            player1Id = snapshot.Child("player1Id").Value?.ToString(),
            player1Name = snapshot.Child("player1Name").Value?.ToString() ?? "Jugador 1",
            player2Id = snapshot.Child("player2Id").Value?.ToString() ?? "",
            player2Name = snapshot.Child("player2Name").Value?.ToString() ?? "",
            player1Ready = Convert.ToBoolean(snapshot.Child("player1Ready").Value ?? false),
            player2Ready = Convert.ToBoolean(snapshot.Child("player2Ready").Value ?? false),
            gameStarted = Convert.ToBoolean(snapshot.Child("gameStarted").Value ?? false),
            createdAt = snapshot.Child("createdAt").Value
        };
    }
}

[Serializable]
public class DofusGameState
{
    public int currentTurn;
    public string data; // JSON del GameStateData

    public static DofusGameState FromSnapshot(DataSnapshot snapshot)
    {
        return new DofusGameState
        {
            currentTurn = Convert.ToInt32(snapshot.Child("currentTurn").Value ?? 1),
            data = snapshot.Child("data").Value?.ToString() ?? ""
        };
    }
}