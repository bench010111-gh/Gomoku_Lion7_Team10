using UnityEngine;
using UnityEngine.SceneManagement;

public class DuoLobbyManager : MonoBehaviour
{
    public string sceneName = "";
    public void OnClickDuoMode()
    {
        SceneManager.LoadScene(sceneName);
    }
}
