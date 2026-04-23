using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text guideText;

    [Header("Next Scene")]
    public string nextSceneName = "02_LoginScene";

    private bool firstInputReceived = false;

    void Start()
    {
        if (guideText != null)
        {
            guideText.text = "아무 키나 눌러 시작";
        }
    }

    void Update()
    {
        // 키보드 아무 키
        bool keyboardInput = Input.anyKeyDown;

        // 마우스 클릭
        bool mouseInput = Input.GetMouseButtonDown(0);

        if (!keyboardInput && !mouseInput)
            return;

        if (!firstInputReceived)
        {
            firstInputReceived = true;

            if (guideText != null)
            {
                guideText.text = "다시 클릭하면 로그인 화면으로 이동합니다";
            }
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}