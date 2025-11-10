using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    [Header("Login UI")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button offlineButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Player Selection")]
    [SerializeField] private GameObject playerSelectionPanel;
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player2NameText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    private FirebaseAuth auth;
    private FirebaseUser user;
    private DatabaseReference databaseRef;

    private string player1Name;
    private string player2Name;
    private string currentUserId;
    private string currentRoomId;

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
        // Asegurar EventSystem
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[LoginManager] EventSystem creado");
        }

        ValidateUIReferences();
        SetupUICallbacks();

        // ✅ ESPERAR a que NetworkManager inicialice Firebase
        StartCoroutine(WaitForFirebaseInitialization());

        if (loginPanel == null)
        {
            Debug.LogWarning("[LoginManager] Panel no asignado. Iniciando offline.");
            StartOfflineMode();
        }
    }

    private IEnumerator WaitForFirebaseInitialization()
    {
        Debug.Log("[LoginManager] ⏳ Esperando inicialización de Firebase...");

        // Esperar a que NetworkManager exista e inicialice Firebase
        while (NetworkManager.Instance == null || !NetworkManager.Instance.IsFirebaseReady())
        {
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("[LoginManager] ✅ Firebase listo! Inicializando Auth...");

        // Ahora sí inicializar Auth
        try
        {
            auth = FirebaseAuth.DefaultInstance;
            databaseRef = FirebaseDatabase.DefaultInstance.RootReference;

            if (auth != null)
            {
                auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);
                Debug.Log("[LoginManager] ✅ Auth inicializado correctamente");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginManager] ❌ Error al inicializar Auth: {e.Message}");
        }
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("[LoginManager] Usuario desconectado");
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log($"[LoginManager] Usuario conectado: {user.DisplayName ?? user.Email}");
            }
        }
    }

    private void ValidateUIReferences()
    {
        Debug.Log("[LoginManager] Validando referencias UI...");

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[LoginManager] GraphicRaycaster agregado al Canvas");
            }
        }

        if (loginButton != null)
        {
            loginButton.interactable = true;
            Debug.Log($"[LoginManager] Botón Login: {(loginButton.interactable ? "Activo" : "Inactivo")}");
        }

        if (registerButton != null)
        {
            registerButton.interactable = true;
            Debug.Log($"[LoginManager] Botón Register: {(registerButton.interactable ? "Activo" : "Inactivo")}");
        }

        if (offlineButton != null)
        {
            offlineButton.interactable = true;
            Debug.Log($"[LoginManager] Botón Offline: {(offlineButton.interactable ? "Activo" : "Inactivo")}");
        }
    }

    void SetupUICallbacks()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() =>
            {
                Debug.Log("[LoginManager] 🔘 Botón Login presionado");
                OnLoginClicked();
            });
        }

        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(() =>
            {
                Debug.Log("[LoginManager] 🔘 Botón Register presionado");
                OnRegisterClicked();
            });
        }

        if (offlineButton != null)
        {
            offlineButton.onClick.RemoveAllListeners();
            offlineButton.onClick.AddListener(() =>
            {
                Debug.Log("[LoginManager] 🔘 Botón Offline presionado");
                StartOfflineMode();
            });
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(() =>
            {
                Debug.Log("[LoginManager] 🔘 Botón Start Game presionado");
                StartGame();
            });
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                Debug.Log("[LoginManager] 🔘 Botón Back presionado");
                OnBackClicked();
            });
        }
    }

    async void OnLoginClicked()
    {
        // ✅ Verificar si Firebase está listo
        if (auth == null)
        {
            ShowError("Firebase aún no está inicializado. Por favor espera...");
            Debug.LogWarning("[LoginManager] Auth aún no está listo");
            return;
        }

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Completa todos los campos");
            return;
        }

        string email = username + "@dofusgame.com";
        UpdateStatus("Iniciando sesión...");

        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = result.User;
            currentUserId = user.UserId;
            player1Name = username;

            UpdateStatus($"¡Bienvenido {username}!");

            if (NetworkManager.Instance != null)
            {
                Debug.Log("[LoginManager] Buscando o creando sala...");

                currentRoomId = await NetworkManager.Instance.JoinRoom(null, username);

                if (string.IsNullOrEmpty(currentRoomId))
                {
                    Debug.Log("[LoginManager] No hay salas, creando nueva...");
                    currentRoomId = await NetworkManager.Instance.CreateRoom(username);
                }

                if (!string.IsNullOrEmpty(currentRoomId))
                {
                    NetworkManager.Instance.OnRoomUpdated += OnRoomUpdated;
                    NetworkManager.Instance.OnGameStarted += OnGameStarted;

                    ShowPlayerSelection();
                }
                else
                {
                    ShowError("Error al conectar a sala");
                }
            }

            SaveUserData(user.UserId, username);
        }
        catch (System.Exception ex)
        {
            ShowError("Error al iniciar sesión: " + ex.Message);
            Debug.LogError($"[LoginManager] Error: {ex}");
        }
    }

    async void OnRegisterClicked()
    {
        if (auth == null)
        {
            ShowError("Firebase aún no está inicializado. Por favor espera...");
            Debug.LogWarning("[LoginManager] Auth aún no está listo");
            return;
        }

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Completa todos los campos");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("La contraseña debe tener al menos 6 caracteres");
            return;
        }

        string email = username + "@dofusgame.com";
        UpdateStatus("Registrando usuario...");

        try
        {
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = result.User;

            SaveUserData(user.UserId, username);
            UpdateStatus($"Usuario {username} registrado exitosamente");

            Debug.Log($"[LoginManager] Usuario registrado: {username}");
        }
        catch (System.Exception ex)
        {
            ShowError("Error al registrar: " + ex.Message);
            Debug.LogError($"[LoginManager] Error: {ex}");
        }
    }

    void OnRoomUpdated(DofusRoomData roomData)
    {
        Debug.Log($"[LoginManager] Sala actualizada: {roomData.roomId}");

        if (player1NameText != null)
            player1NameText.text = $"Jugador 1: {roomData.player1Name}";

        if (!string.IsNullOrEmpty(roomData.player2Name))
        {
            if (player2NameText != null)
                player2NameText.text = $"Jugador 2: {roomData.player2Name}";

            player2Name = roomData.player2Name;
        }
        else
        {
            if (player2NameText != null)
                player2NameText.text = "Jugador 2: Esperando...";
        }

        if (roomData.player1Ready && roomData.player2Ready)
        {
            if (startGameButton != null)
                startGameButton.interactable = true;
            UpdateStatus("¡Listos! Presiona Iniciar Juego");
        }
    }

    void OnGameStarted()
    {
        Debug.Log("[LoginManager] ¡Juego iniciado! Cambiando a GameScene...");
        SceneManager.LoadScene("GameScene");
    }

    void ShowPlayerSelection()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (playerSelectionPanel != null)
        {
            playerSelectionPanel.SetActive(true);
            if (player1NameText != null)
                player1NameText.text = $"Jugador 1: {player1Name}";
            if (player2NameText != null)
                player2NameText.text = "Jugador 2: Esperando...";
            if (startGameButton != null)
                startGameButton.interactable = false;
        }

        UpdateStatus("Esperando al oponente...");
    }

    async void OnBackClicked()
    {
        if (auth != null && user != null)
        {
            auth.SignOut();
        }

        if (NetworkManager.Instance != null)
        {
            await NetworkManager.Instance.LeaveRoom();
            NetworkManager.Instance.OnRoomUpdated -= OnRoomUpdated;
            NetworkManager.Instance.OnGameStarted -= OnGameStarted;
        }

        if (loginPanel != null) loginPanel.SetActive(true);
        if (playerSelectionPanel != null) playerSelectionPanel.SetActive(false);
        UpdateStatus("Sesión cerrada");
    }

    void StartOfflineMode()
    {
        Debug.Log("[LoginManager] 🎮 Iniciando modo offline...");

        player1Name = "Jugador 1";
        player2Name = "Jugador 2";

        PlayerPrefs.SetString("IsOnlineMode", "False");
        PlayerPrefs.SetString("Player1Name", player1Name);
        PlayerPrefs.SetString("Player2Name", player2Name);
        PlayerPrefs.Save();

        UpdateStatus("Iniciando offline...");

        StartCoroutine(LoadGameSceneDelayed());
    }

    private IEnumerator LoadGameSceneDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[LoginManager] Cargando GameScene...");
        SceneManager.LoadScene("GameScene");
    }

    async void StartGame()
    {
        Debug.Log("[LoginManager] 🎮 Iniciando juego...");

        if (NetworkManager.Instance != null && NetworkManager.Instance.IsOnlineMode())
        {
            await NetworkManager.Instance.SetPlayerReady();

            if (NetworkManager.Instance.isHost)
            {
                var roomSnapshot = await NetworkManager.Instance.currentRoomRef.GetValueAsync();
                var roomData = DofusRoomData.FromSnapshot(roomSnapshot);

                if (roomData.player1Ready && roomData.player2Ready)
                {
                    await NetworkManager.Instance.StartGame();
                }
            }
        }
        else
        {
            Debug.Log("[LoginManager] Cambiando a GameScene (Offline)...");
            SceneManager.LoadScene("GameScene");
        }
    }

    void SaveUserData(string userId, string username)
    {
        if (databaseRef == null) return;

        var userData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "username", username },
            { "createdAt", System.DateTime.Now.ToString() }
        };

        databaseRef.Child("users").Child(userId).UpdateChildrenAsync(userData);
    }

    private void ShowError(string message)
    {
        Debug.LogError($"[LoginManager] ❌ {message}");
        UpdateStatus(message);

        if (errorText != null)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = message;
            errorText.color = Color.red;

            StartCoroutine(HideErrorAfterDelay(5f));
        }
    }

    private IEnumerator HideErrorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = message.Contains("Error") || message.Contains("❌") ? Color.red : Color.white;
        }
        Debug.Log($"[LoginManager] {message}");
    }

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomUpdated -= OnRoomUpdated;
            NetworkManager.Instance.OnGameStarted -= OnGameStarted;
        }
    }
}