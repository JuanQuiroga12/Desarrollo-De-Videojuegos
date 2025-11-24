using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja la inicialización secuencial de todos los sistemas del juego.
/// Garantiza que todo esté listo antes de permitir interacciones.
/// </summary>
public class GameInitializationManager : MonoBehaviour
{
    public static GameInitializationManager Instance { get; private set; }

    [Header("Estado de Inicialización")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private bool isNetworkReady = false;
    [SerializeField] private bool isMapReady = false;
    [SerializeField] private bool isGridReady = false;
    [SerializeField] private bool arePlayersReady = false;
    [SerializeField] private bool isGameManagerReady = false;

    [Header("Referencias UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMPro.TextMeshProUGUI loadingText;

    // Eventos de inicialización
    public event System.Action OnInitializationComplete;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Solo inicializar si estamos en GameScene
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            StartCoroutine(InitializeGameSystems());
        }
        else
        {
            // Si no estamos en GameScene, marcar como inicializado
            isInitialized = true;
        }
    }

    /// <summary>
    /// Inicializa todos los sistemas del juego en orden secuencial.
    /// </summary>
    private IEnumerator InitializeGameSystems()
    {
        Debug.Log("[GameInit] 🚀 Iniciando secuencia de inicialización...");

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // 1️⃣ FASE 1: Verificar NetworkManager
        UpdateLoadingText("Conectando al servidor...");
        yield return StartCoroutine(WaitForNetworkManager());

        // 2️⃣ FASE 2: Esperar a que Firebase esté listo
        UpdateLoadingText("Sincronizando con Firebase...");
        yield return StartCoroutine(WaitForFirebase());

        // 3️⃣ FASE 3: Esperar MapGenerator
        UpdateLoadingText("Generando mapa...");
        yield return StartCoroutine(WaitForMapGenerator());

        // 4️⃣ FASE 4: Esperar GridVisualizer
        UpdateLoadingText("Preparando grid visual...");
        yield return StartCoroutine(WaitForGridVisualizer());

        // 5️⃣ FASE 5: Esperar GameManager
        UpdateLoadingText("Inicializando juego...");
        yield return StartCoroutine(WaitForGameManager());

        // 6️⃣ FASE 6: Esperar jugadores
        UpdateLoadingText("Cargando jugadores...");
        yield return StartCoroutine(WaitForPlayers());

        // 7️⃣ FASE 7: Esperar turnos
        UpdateLoadingText("Configurando turnos...");
        yield return StartCoroutine(WaitForTurnSystem());

        // 8️⃣ FASE 8: Habilitar sistemas de input
        UpdateLoadingText("Habilitando controles...");
        yield return StartCoroutine(EnableInputSystems());

        // ✅ TODO LISTO
        isInitialized = true;
        UpdateLoadingText("¡Listo!");
        yield return new WaitForSeconds(0.5f);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        OnInitializationComplete?.Invoke();

        Debug.Log("[GameInit] ✅ INICIALIZACIÓN COMPLETA");
        LogGameState();
    }

    // ========================================
    // FASES DE INICIALIZACIÓN
    // ========================================

    private IEnumerator WaitForNetworkManager()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (NetworkManager.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[GameInit] ❌ NetworkManager no encontrado");
            yield break;
        }

        isNetworkReady = true;
        Debug.Log("[GameInit] ✅ NetworkManager listo");
    }

    private IEnumerator WaitForFirebase()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[GameInit] ❌ NetworkManager es null");
            yield break;
        }

        // Solo esperar Firebase si estamos en modo online
        string isOnlineMode = PlayerPrefs.GetString("IsOnlineMode", "false");
        if (isOnlineMode != "True")
        {
            Debug.Log("[GameInit] ✅ Modo offline - Firebase no requerido");
            yield break;
        }

        float timeout = 15f;
        float elapsed = 0f;

        // Esperar a que Firebase esté inicializado
        while (!NetworkManager.Instance.IsFirebaseReady() && elapsed < timeout)
        {
            Debug.Log($"[GameInit] ⏳ Esperando Firebase... ({elapsed:F1}s)");
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (!NetworkManager.Instance.IsFirebaseReady())
        {
            Debug.LogError("[GameInit] ❌ Firebase timeout");
            yield break;
        }

        Debug.Log("[GameInit] ✅ Firebase listo");
    }

    private IEnumerator WaitForMapGenerator()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (MapGenerator.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (MapGenerator.Instance == null)
        {
            Debug.LogError("[GameInit] ❌ MapGenerator no encontrado");
            yield break;
        }

        // Esperar a que el mapa tenga dimensiones válidas
        while (MapGenerator.Instance.GetMapWidth() == 0 || MapGenerator.Instance.GetMapHeight() == 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        isMapReady = true;
        Debug.Log("[GameInit] ✅ Mapa generado");
    }

    private IEnumerator WaitForGridVisualizer()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (GridVisualizer.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (GridVisualizer.Instance == null)
        {
            Debug.LogError("[GameInit] ❌ GridVisualizer no encontrado");
            yield break;
        }

        // Dar tiempo adicional para que se creen las tiles
        yield return new WaitForSeconds(0.5f);

        isGridReady = true;
        Debug.Log("[GameInit] ✅ Grid visual listo");
    }

    private IEnumerator WaitForGameManager()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (GameManager.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameInit] ❌ GameManager no encontrado");
            yield break;
        }

        isGameManagerReady = true;
        Debug.Log("[GameInit] ✅ GameManager listo");
    }

    private IEnumerator WaitForPlayers()
    {
        float timeout = 15f;
        float elapsed = 0f;

        // ✅ CORREGIDO: Usar ArePlayersInitialized() en lugar de GetAllPlayers().Count
        while (!GameManager.Instance.ArePlayersInitialized() && elapsed < timeout)
        {
            var players = GameManager.Instance.GetAllPlayers();
            Debug.Log($"[GameInit] ⏳ Esperando jugadores... ({players.Count}/2)");
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (!GameManager.Instance.ArePlayersInitialized())
        {
            Debug.LogWarning("[GameInit] ⚠️ No se inicializaron correctamente los jugadores");
        }

        // Verificar que los jugadores tengan sus sistemas cargados
        foreach (var player in GameManager.Instance.GetAllPlayers())
        {
            PlayerMovementSystem movementSystem = player.GetComponent<PlayerMovementSystem>();
            SpellCastingSystem spellSystem = player.GetComponent<SpellCastingSystem>();

            if (movementSystem == null || spellSystem == null)
            {
                Debug.LogWarning($"[GameInit] ⚠️ {player.name} no tiene todos los sistemas");
            }
        }

        arePlayersReady = true;
        Debug.Log("[GameInit] ✅ Jugadores listos");
    }

    private IEnumerator WaitForTurnSystem()
    {
        // Esperar a que GameManager esté completamente inicializado
        yield return new WaitForSeconds(0.5f);

        // Verificar que hay un turno activo
        if (GameManager.Instance != null)
        {
            var gameState = GameManager.Instance.GetGameState();
            if (gameState != null)
            {
                Debug.Log($"[GameInit] Turno actual: {gameState.currentTurn}");
            }
        }

        Debug.Log("[GameInit] ✅ Sistema de turnos listo");
    }

    private IEnumerator EnableInputSystems()
    {
        // Verificar que Enhanced Touch esté habilitado en todos los PlayerMovementSystem
        foreach (var player in GameManager.Instance.GetAllPlayers())
        {
            PlayerMovementSystem movementSystem = player.GetComponent<PlayerMovementSystem>();
            if (movementSystem != null && movementSystem.enabled)
            {
                Debug.Log($"[GameInit] ✅ Input habilitado para {player.name}");
            }
            else
            {
                Debug.LogWarning($"[GameInit] ⚠️ PlayerMovementSystem deshabilitado en {player.name}");
            }
        }

        yield return null;
    }

    // ========================================
    // UTILIDADES
    // ========================================

    private void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
        Debug.Log($"[GameInit] 📝 {text}");
    }

    private void LogGameState()
    {
        Debug.Log("=== ESTADO DEL JUEGO ===");
        Debug.Log($"Network Ready: {isNetworkReady}");
        Debug.Log($"Map Ready: {isMapReady}");
        Debug.Log($"Grid Ready: {isGridReady}");
        Debug.Log($"GameManager Ready: {isGameManagerReady}");
        Debug.Log($"Players Ready: {arePlayersReady}");

        if (GameManager.Instance != null)
        {
            var players = GameManager.Instance.GetAllPlayers();
            Debug.Log($"Jugadores encontrados: {players.Count}");

            foreach (var player in players)
            {
                PlayerData data = player.GetPlayerData();
                Debug.Log($"  - {player.name}: Turno={data.isMyTurn}, Local={player.isLocalPlayer}");
            }
        }

        if (NetworkManager.Instance != null)
        {
            Debug.Log($"Player Number: {NetworkManager.Instance.GetPlayerNumber()}");
            Debug.Log($"Is Host: {NetworkManager.Instance.isHost}");
            Debug.Log($"Room ID: {NetworkManager.Instance.GetRoomId()}");
        }

        Debug.Log("========================");
    }

    // ========================================
    // MÉTODOS PÚBLICOS
    // ========================================

    public bool IsInitialized()
    {
        return isInitialized &&
               isNetworkReady &&
               isMapReady &&
               isGridReady &&
               arePlayersReady &&
               isGameManagerReady;
    }

    public bool CanPlayerInteract()
    {
        if (!IsInitialized())
        {
            Debug.LogWarning("[GameInit] ⚠️ Juego aún no está inicializado");
            return false;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameInit] ⚠️ GameManager es null");
            return false;
        }

        return true;
    }
}