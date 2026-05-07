using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DuoModeExit : MonoBehaviour
{
    public string sceneName = "";

    private void Awake()
    {
        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }

    public void ExitDuoMode()
    {
        
        SceneManager.LoadScene(sceneName);
    }
}
