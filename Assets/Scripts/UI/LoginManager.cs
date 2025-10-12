using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

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

    [Header("Player Selection")]
    [SerializeField] private GameObject playerSelectionPanel;
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player2NameText;
    [SerializeField] private Button startGameButton;

    // Firebase
    private FirebaseAuth auth;
    private DatabaseReference databaseRef;
    private bool firebaseInitialized = false;

    // Datos de jugadores
    private string player1Name;
    private string player2Name;
    private bool isOnlineMode = false;

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
        InitializeFirebase();
        SetupUICallbacks();

        // Si no hay UI asignada, trabajar sin Firebase
        if (loginPanel == null)
        {
            Debug.LogWarning("No se ha asignado el panel de login. Iniciando en modo offline.");
            StartOfflineMode();
        }
    }

    void InitializeFirebase()
    {
        Debug.Log("Iniciando verificación de dependencias de Firebase...");

        // Verificar que el archivo existe
        string desktopPath = Path.Combine(Application.streamingAssetsPath, "google-services.json");
        Debug.Log($"Buscando google-services.json en: {desktopPath}");
        Debug.Log($"¿Archivo existe?: {File.Exists(desktopPath)}");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log($"Resultado de dependencias: {task.Result}");

            if (task.Result == DependencyStatus.Available)
            {
                // Verificar que el FirebaseApp esté configurado correctamente
                try
                {
                    FirebaseApp app = FirebaseApp.DefaultInstance;
                    Debug.Log($"Firebase App Name: {app.Name}");

                    if (app.Options != null)
                    {
                        Debug.Log($"Firebase Project ID: {app.Options.ProjectId}");
                        Debug.Log($"Firebase App ID: {app.Options.AppId}");
                        Debug.Log($"Firebase API Key: {(string.IsNullOrEmpty(app.Options.ApiKey) ? "NULL" : "Configurado")}");
                    }
                    else
                    {
                        Debug.LogError("Firebase Options es NULL. Verifica que google-services.json esté en Assets/StreamingAssets/");
                        UpdateStatus("Error: Configuración de Firebase no encontrada");

                        if (offlineButton != null)
                        {
                            loginButton.interactable = false;
                            registerButton.interactable = false;
                        }
                        return;
                    }

                    // Inicializar Firebase
                    auth = FirebaseAuth.DefaultInstance;
                    databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
                    firebaseInitialized = true;

                    UpdateStatus("Firebase inicializado correctamente");
                    Debug.Log($"Firebase inicializado correctamente. Auth: {auth != null}, Database: {databaseRef != null}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error al inicializar Firebase: {ex.Message}");
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                    UpdateStatus($"Error al inicializar Firebase: {ex.Message}");

                    if (offlineButton != null)
                    {
                        loginButton.interactable = false;
                        registerButton.interactable = false;
                    }
                }
            }
            else
            {
                UpdateStatus($"No se pudo inicializar Firebase: {task.Result}");
                Debug.LogError($"No se pudo resolver las dependencias de Firebase: {task.Result}");

                // Habilitar solo modo offline
                if (offlineButton != null)
                {
                    loginButton.interactable = false;
                    registerButton.interactable = false;
                }
            }
        });
    }

    void SetupUICallbacks()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);

        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterClicked);

        if (offlineButton != null)
            offlineButton.onClick.AddListener(StartOfflineMode);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
    }

    void OnLoginClicked()
    {
        if (!firebaseInitialized)
        {
            UpdateStatus("Firebase no está inicializado");
            return;
        }

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Por favor completa todos los campos");
            return;
        }

        // Para simplificar, usamos el username como email agregando un dominio
        string email = username + "@dofusgame.com";

        UpdateStatus("Iniciando sesión...");

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                UpdateStatus("Login cancelado");
                return;
            }

            if (task.IsFaulted)
            {
                // Mejorar el manejo de errores para obtener más información
                string errorMessage = "Error al iniciar sesión";

                if (task.Exception != null)
                {
                    foreach (var inner in task.Exception.InnerExceptions)
                    {
                        if (inner is Firebase.FirebaseException firebaseEx)
                        {
                            errorMessage = GetFirebaseErrorMessage(firebaseEx);
                            Debug.LogError($"Firebase Error Code: {firebaseEx.ErrorCode}");
                        }
                        Debug.LogError($"Error detallado: {inner.Message}");
                    }
                }

                UpdateStatus(errorMessage);
                Debug.LogError($"Error de login completo: {task.Exception}");
                return;
            }

            FirebaseUser user = task.Result.User;
            UpdateStatus($"Bienvenido {username}!");
            player1Name = username;

            // Guardar datos del usuario
            SaveUserData(user.UserId, username);

            // Mostrar panel de selección de segundo jugador
            ShowPlayerSelection();
        });
    }

    void OnRegisterClicked()
    {
        if (!firebaseInitialized)
        {
            UpdateStatus("Firebase no está inicializado");
            return;
        }

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Por favor completa todos los campos");
            return;
        }

        if (password.Length < 6)
        {
            UpdateStatus("La contraseña debe tener al menos 6 caracteres");
            return;
        }

        string email = username + "@dofusgame.com";

        UpdateStatus("Registrando usuario...");

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                UpdateStatus("Registro cancelado");
                return;
            }

            if (task.IsFaulted)
            {
                string errorMessage = "Error al registrar usuario";

                if (task.Exception != null)
                {
                    foreach (var inner in task.Exception.InnerExceptions)
                    {
                        if (inner is Firebase.FirebaseException firebaseEx)
                        {
                            errorMessage = GetFirebaseErrorMessage(firebaseEx);
                            Debug.LogError($"Firebase Error Code: {firebaseEx.ErrorCode}");
                        }
                        Debug.LogError($"Error detallado: {inner.Message}");
                    }
                }

                UpdateStatus(errorMessage);
                Debug.LogError($"Error de registro completo: {task.Exception}");
                return;
            }

            FirebaseUser user = task.Result.User;
            UpdateStatus($"Usuario {username} registrado exitosamente!");

            // Crear perfil inicial del jugador
            CreateUserProfile(user.UserId, username);
        });
    }

    // Nuevo método para obtener mensajes de error más amigables
    private string GetFirebaseErrorMessage(Firebase.FirebaseException exception)
    {
        switch (exception.ErrorCode)
        {
            case (int)Firebase.Auth.AuthError.UserNotFound:
                return "Usuario no encontrado. Por favor regístrate primero.";
            case (int)Firebase.Auth.AuthError.WrongPassword:
                return "Contraseña incorrecta";
            case (int)Firebase.Auth.AuthError.InvalidEmail:
                return "Email inválido";
            case (int)Firebase.Auth.AuthError.EmailAlreadyInUse:
                return "Este usuario ya existe";
            case (int)Firebase.Auth.AuthError.WeakPassword:
                return "La contraseña es demasiado débil";
            case (int)Firebase.Auth.AuthError.NetworkRequestFailed:
                return "Error de conexión. Verifica tu internet.";
            default:
                return $"Error de Firebase: {exception.Message}";
        }
    }

    void SaveUserData(string userId, string username)
    {
        if (!firebaseInitialized || databaseRef == null) return;

        PlayerData playerData = new PlayerData();
        playerData.username = username;

        string json = JsonUtility.ToJson(playerData);

        databaseRef.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Datos del usuario guardados en Firebase");
            }
            else
            {
                Debug.LogError("Error al guardar datos: " + task.Exception);
            }
        });
    }

    void CreateUserProfile(string userId, string username)
    {
        if (!firebaseInitialized || databaseRef == null) return;

        // Crear perfil completo del jugador
        var userData = new
        {
            username = username,
            level = 1,
            experience = 0,
            wins = 0,
            losses = 0,
            createdAt = System.DateTime.Now.ToString()
        };

        string json = JsonUtility.ToJson(userData);

        databaseRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
    }

    void ShowPlayerSelection()
    {
        isOnlineMode = true;

        if (loginPanel != null)
            loginPanel.SetActive(false);

        if (playerSelectionPanel != null)
        {
            playerSelectionPanel.SetActive(true);
            player1NameText.text = $"Jugador 1: {player1Name}";
            player2NameText.text = "Jugador 2: Esperando...";

            // En un juego real aquí buscarías oponentes online
            // Por ahora simularemos un segundo jugador local
            StartCoroutine(SimulatePlayer2Join());
        }
        else
        {
            // Si no hay panel de selección, ir directo al juego
            player2Name = "CPU";
            StartGame();
        }
    }

    IEnumerator SimulatePlayer2Join()
    {
        yield return new WaitForSeconds(2f);
        player2Name = "Jugador 2";
        player2NameText.text = $"Jugador 2: {player2Name}";
        startGameButton.interactable = true;
    }

    void StartOfflineMode()
    {
        isOnlineMode = false;
        player1Name = "Jugador 1";
        player2Name = "Jugador 2";

        UpdateStatus("Iniciando modo offline...");
        StartGame();
    }

    void StartGame()
    {
        // Cargar la escena del juego
        SceneManager.LoadScene("GameScene");
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Login Status: {message}");
    }

    // Métodos públicos para acceder a los datos
    public string GetPlayer1Name()
    {
        return string.IsNullOrEmpty(player1Name) ? "Jugador 1" : player1Name;
    }

    public string GetPlayer2Name()
    {
        return string.IsNullOrEmpty(player2Name) ? "Jugador 2" : player2Name;
    }

    public bool IsOnlineMode()
    {
        return isOnlineMode;
    }

    // Guardar estadísticas del juego
    public void SaveGameResult(string winnerId, string loserId)
    {
        if (!firebaseInitialized || !isOnlineMode) return;

        // Actualizar estadísticas del ganador
        databaseRef.Child("users").Child(winnerId).Child("wins").RunTransaction(mutableData =>
        {
            int wins = mutableData.Value != null ? (int)mutableData.Value : 0;
            mutableData.Value = wins + 1;
            return TransactionResult.Success(mutableData);
        });

        // Actualizar estadísticas del perdedor
        databaseRef.Child("users").Child(loserId).Child("losses").RunTransaction(mutableData =>
        {
            int losses = mutableData.Value != null ? (int)mutableData.Value : 0;
            mutableData.Value = losses + 1;
            return TransactionResult.Success(mutableData);
        });
    }
}