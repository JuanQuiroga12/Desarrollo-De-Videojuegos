using UnityEngine;

public class SceneDebugger : MonoBehaviour
{
    void Awake()
    {
        Debug.Log($"[SceneDebugger] ========== SCENE LOADED: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} ==========");
        ListAllObjects();
    }

    void Start()
    {
        Debug.Log("[SceneDebugger] ========== START PHASE ==========");
        ListAllObjects();
    }

    void Update()
    {
        // Solo en los primeros 3 segundos
        if (Time.time < 3f && Time.frameCount % 30 == 0)
        {
            ListAllObjects();
        }
    }

    void ListAllObjects()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Debug.Log($"[SceneDebugger] 📋 Total objetos: {allObjects.Length}");

        // Buscar específicamente GameManager
        bool foundGameManager = false;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("GameManager"))
            {
                foundGameManager = true;
                Debug.Log($"[SceneDebugger] ✅ ENCONTRADO: {obj.name} - Activo: {obj.activeInHierarchy}");

                GameManager gm = obj.GetComponent<GameManager>();
                if (gm != null)
                {
                    Debug.Log($"[SceneDebugger]    - Componente GameManager: PRESENTE");
                    Debug.Log($"[SceneDebugger]    - Singleton Instance: {GameManager.Instance != null}");
                }
            }
        }

        if (!foundGameManager)
        {
            Debug.LogError("[SceneDebugger] ❌ NO SE ENCONTRÓ GameManager en la escena");
        }
    }
}