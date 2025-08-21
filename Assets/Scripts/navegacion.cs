using UnityEngine;
using UnityEngine.SceneManagement;

public class navegacion : MonoBehaviour
{
    public void Load(int sceneNumber)
    {
        SceneManager.LoadScene(sceneNumber);
    }
}
