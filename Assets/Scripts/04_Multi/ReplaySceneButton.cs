using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplaySceneButton : MonoBehaviour
{
    public string replaySceneName = "09_ReplayScene";

    public void OnClickReplayScene()
    {
        SceneManager.LoadScene(replaySceneName);
    }
}