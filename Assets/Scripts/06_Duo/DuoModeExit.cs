using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DuoModeExit : MonoBehaviour
{
    public string sceneName = "";
    public void ExitDuoMode()
    {
        
        SceneManager.LoadScene(sceneName);
    }
}
