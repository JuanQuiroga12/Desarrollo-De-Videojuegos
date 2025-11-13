using UnityEngine;

public class WaitingRoomManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[WaitingRoomManager] Iniciando en GameScene");

        // Los datos ya están sincronizados desde LoginScene
        string isOnlineMode = PlayerPrefs.GetString("IsOnlineMode", "False");

        Debug.Log($"[WaitingRoomManager] Modo online: {isOnlineMode}");

        // ✅ CORREGIDO: Esperar un frame completo antes de buscar GameManager
        StartCoroutine(WaitForGameManager());
    }

    System.Collections.IEnumerator WaitForGameManager()
    {
        // Esperar 1 frame para que todos los Awake() se ejecuten
        yield return null;

        // Esperar otro poco para que Start() se ejecute
        yield return new WaitForSeconds(0.1f);

        StartGame();
    }

    void StartGame()
    {
        Debug.Log("[WaitingRoomManager] Iniciando juego...");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();

            // ✅ Opcional: Destruir WaitingRoomManager después de iniciar
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("[WaitingRoomManager] ❌ GameManager no encontrado después de esperar");

            // ✅ Verificar si existe el GameObject
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                Debug.Log("[WaitingRoomManager] ✅ GameManager encontrado con FindObjectOfType");
                gm.StartGame();
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("[WaitingRoomManager] ❌ GameManager no existe en la escena");
            }
        }
    }
}