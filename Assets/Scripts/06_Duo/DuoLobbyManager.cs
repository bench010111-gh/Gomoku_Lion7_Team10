using UnityEngine;
using UnityEngine.SceneManagement;

public class DuoLobbyManager : MonoBehaviour
{
    public string sceneName = "";
    public void OnClickDuoMode()
    {
        Cursor.visible = true;

        SceneManager.LoadScene(sceneName);
    }
}
