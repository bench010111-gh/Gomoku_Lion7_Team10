using UnityEngine;
using UnityEngine.SceneManagement;

public class DuoLobbyManager : MonoBehaviour
{
    public string sceneName = "";

    private void Awake()
    {
        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }

    public void OnClickDuoMode()
    {
        Cursor.visible = true;

        SceneManager.LoadScene(sceneName);
    }
}
