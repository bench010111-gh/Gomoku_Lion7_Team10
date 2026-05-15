using UnityEngine;

public class GameExit : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("게임 종료");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}