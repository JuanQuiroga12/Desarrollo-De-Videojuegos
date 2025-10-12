using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private int port = 7777;
    [SerializeField] private float syncInterval = 0.1f; // Sincronizar cada 100ms

    [Header("Connection Status")]
    [SerializeField] private bool isHost = false;
    [SerializeField] private bool isConnected = false;
    [SerializeField] private string connectionStatus = "Desconectado";

    // Networking
    private TcpListener tcpListener;
    private TcpClient tcpClient;
    private NetworkStream stream;
    private Thread tcpListenerThread;
    private bool isListening = false;

    // Sincronización
    private float lastSyncTime = 0f;
    private Queue<string> messageQueue = new Queue<string>();

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
        // Inicializar basándose en el modo de login
        if (LoginManager.Instance != null && LoginManager.Instance.IsOnlineMode())
        {
            ShowConnectionDialog();
        }
    }

    void Update()
    {
        // Procesar mensajes recibidos
        ProcessMessages();

        // Sincronizar estado del juego
        if (isConnected && Time.time - lastSyncTime > syncInterval)
        {
            SyncGameState();
            lastSyncTime = Time.time;
        }
    }

    void ShowConnectionDialog()
    {
        // En una implementación real, mostrarías un diálogo
        // Por ahora, automáticamente intentar ser host
        StartAsHost();
    }

    public void StartAsHost()
    {
        isHost = true;
        connectionStatus = "Iniciando como Host...";

        // Iniciar servidor TCP
        tcpListenerThread = new Thread(new ThreadStart(ListenForClients));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        Debug.Log($"Host iniciado en puerto {port}");
        connectionStatus = $"Host - Esperando jugador en puerto {port}";
    }

    public void StartAsClient(string hostIP)
    {
        isHost = false;
        connectionStatus = "Conectando...";

        StartCoroutine(ConnectToHost(hostIP));
    }

    void ListenForClients()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            isListening = true;

            Debug.Log("Servidor esperando conexiones...");

            while (isListening)
            {
                using (tcpClient = tcpListener.AcceptTcpClient())
                {
                    Debug.Log("Cliente conectado!");
                    isConnected = true;
                    connectionStatus = "Host - Cliente conectado";

                    stream = tcpClient.GetStream();

                    // Manejar comunicación con el cliente
                    HandleClientCommunication();
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.LogError("SocketException: " + socketException.ToString());
        }
        finally
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }
    }

    IEnumerator ConnectToHost(string hostIP)
    {
        try
        {
            tcpClient = new TcpClient();

            // Intentar conectar
            IAsyncResult result = tcpClient.BeginConnect(hostIP, port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(5000, true);

            if (!success)
            {
                throw new SocketException();
            }

            tcpClient.EndConnect(result);

            isConnected = true;
            connectionStatus = "Cliente - Conectado al host";
            stream = tcpClient.GetStream();

            Debug.Log($"Conectado al host: {hostIP}");

            // Iniciar recepción de mensajes
            StartCoroutine(ReceiveMessages());
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al conectar: {e.Message}");
            connectionStatus = "Error de conexión";
        }

        yield return null;
    }

    void HandleClientCommunication()
    {
        byte[] buffer = new byte[1024];

        while (tcpClient != null && tcpClient.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Agregar mensaje a la cola para procesamiento
                    lock (messageQueue)
                    {
                        messageQueue.Enqueue(message);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error en comunicación: {e.Message}");
                break;
            }
        }
    }

    IEnumerator ReceiveMessages()
    {
        byte[] buffer = new byte[1024];

        while (isConnected && tcpClient != null && tcpClient.Connected)
        {
            if (stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Agregar mensaje a la cola para procesamiento
                    lock (messageQueue)
                    {
                        messageQueue.Enqueue(message);
                    }
                }
            }

            yield return new WaitForSeconds(0.01f);
        }
    }

    void ProcessMessages()
    {
        lock (messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                ProcessNetworkMessage(message);
            }
        }
    }

    void ProcessNetworkMessage(string message)
    {
        try
        {
            // Parsear el mensaje JSON
            NetworkMessage netMsg = JsonUtility.FromJson<NetworkMessage>(message);

            switch (netMsg.type)
            {
                case "GameState":
                    HandleGameStateUpdate(netMsg.data);
                    break;

                case "PlayerAction":
                    HandlePlayerAction(netMsg.data);
                    break;

                case "SpellCast":
                    HandleSpellCast(netMsg.data);
                    break;

                case "EndTurn":
                    HandleEndTurn();
                    break;

                case "Chat":
                    HandleChatMessage(netMsg.data);
                    break;

                default:
                    Debug.LogWarning($"Tipo de mensaje desconocido: {netMsg.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error procesando mensaje de red: {e.Message}");
        }
    }

    void HandleGameStateUpdate(string data)
    {
        GameStateData gameState = GameStateData.FromJson(data);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateGameState(gameState);
        }
    }

    void HandlePlayerAction(string data)
    {
        PlayerActionData action = JsonUtility.FromJson<PlayerActionData>(data);

        // Aplicar la acción del jugador remoto
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.GetPlayerData().username == action.playerName)
            {
                if (action.actionType == "Move")
                {
                    Vector2Int targetPos = new Vector2Int(action.targetX, action.targetY);
                    player.TryMoveTo(targetPos);
                }
                break;
            }
        }
    }

    void HandleSpellCast(string data)
    {
        SpellCastData spellCast = JsonUtility.FromJson<SpellCastData>(data);

        // Aplicar el hechizo del jugador remoto
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.GetPlayerData().username == spellCast.playerName)
            {
                player.TryCastSpell(spellCast.spellIndex);
                break;
            }
        }
    }

    void HandleEndTurn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndCurrentTurn();
        }
    }

    void HandleChatMessage(string message)
    {
        // Mostrar mensaje en el UI del chat
        var playerUI = UnityEngine.Object.FindFirstObjectByType<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.AddActionToLog($"[Chat] {message}");
        }
    }

    public void SendGameState(GameStateData gameState)
    {
        if (!isConnected || stream == null) return;

        NetworkMessage msg = new NetworkMessage
        {
            type = "GameState",
            data = gameState.ToJson()
        };

        SendMessage(JsonUtility.ToJson(msg));
    }

    public void SendPlayerAction(string playerName, string actionType, int targetX, int targetY)
    {
        if (!isConnected || stream == null) return;

        PlayerActionData action = new PlayerActionData
        {
            playerName = playerName,
            actionType = actionType,
            targetX = targetX,
            targetY = targetY
        };

        NetworkMessage msg = new NetworkMessage
        {
            type = "PlayerAction",
            data = JsonUtility.ToJson(action)
        };

        SendMessage(JsonUtility.ToJson(msg));
    }

    public void SendSpellCast(string playerName, int spellIndex, int targetX, int targetY)
    {
        if (!isConnected || stream == null) return;

        SpellCastData spellCast = new SpellCastData
        {
            playerName = playerName,
            spellIndex = spellIndex,
            targetX = targetX,
            targetY = targetY
        };

        NetworkMessage msg = new NetworkMessage
        {
            type = "SpellCast",
            data = JsonUtility.ToJson(spellCast)
        };

        SendMessage(JsonUtility.ToJson(msg));
    }

    public void SendEndTurn()
    {
        if (!isConnected || stream == null) return;

        NetworkMessage msg = new NetworkMessage
        {
            type = "EndTurn",
            data = ""
        };

        SendMessage(JsonUtility.ToJson(msg));
    }

    public void SendChatMessage(string message)
    {
        if (!isConnected || stream == null) return;

        NetworkMessage msg = new NetworkMessage
        {
            type = "Chat",
            data = message
        };

        SendMessage(JsonUtility.ToJson(msg));
    }

    void SendMessage(string message)
    {
        try
        {
            if (stream != null && stream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error enviando mensaje: {e.Message}");
        }
    }

    void SyncGameState()
    {
        if (isHost && GameManager.Instance != null)
        {
            SendGameState(GameManager.Instance.GetGameState());
        }
    }

    public bool IsNetworkGame()
    {
        return isConnected;
    }

    public bool IsHostPlayer()
    {
        return isHost;
    }

    public string GetConnectionStatus()
    {
        return connectionStatus;
    }

    void OnDestroy()
    {
        Disconnect();
    }

    public void Disconnect()
    {
        isListening = false;
        isConnected = false;

        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient = null;
        }

        if (tcpListener != null)
        {
            tcpListener.Stop();
            tcpListener = null;
        }

        if (tcpListenerThread != null)
        {
            tcpListenerThread.Abort();
            tcpListenerThread = null;
        }

        connectionStatus = "Desconectado";
        Debug.Log("Desconectado de la red");
    }
}

// Clases de datos para mensajes de red
[System.Serializable]
public class NetworkMessage
{
    public string type;
    public string data;
}

[System.Serializable]
public class PlayerActionData
{
    public string playerName;
    public string actionType;
    public int targetX;
    public int targetY;
}

[System.Serializable]
public class SpellCastData
{
    public string playerName;
    public int spellIndex;
    public int targetX;
    public int targetY;
}