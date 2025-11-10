using UnityEngine;

public class WaitingRoomManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[WaitingRoomManager] Iniciando en GameScene");

        // Los datos ya están sincronizados desde LoginScene
        string isOnlineMode = PlayerPrefs.GetString("IsOnlineMode", "False");

        Debug.Log($"[WaitingRoomManager] Modo online: {isOnlineMode}");

        // Pequeño delay para que GameManager se inicialice
        Invoke(nameof(StartGame), 0.5f);
    }

    void StartGame()
    {
        Debug.Log("[WaitingRoomManager] Iniciando juego...");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogWarning("[WaitingRoomManager] GameManager no encontrado");
        }
    }
}