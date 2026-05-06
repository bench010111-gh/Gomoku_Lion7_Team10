using UnityEngine;
using UnityEngine.SceneManagement;

// 각 씬에서 사용되는 뒤로가기 버튼 (이동할 씬 이름 인스펙터에서 적어주면 됨)
// 메인 로비 -> 로그인 화면은 따로 구현해놨음(로그아웃 여부 확인 필요)

public class BackButtonHandler : MonoBehaviour
{
    [Header("Back Target Scene")]
    public string targetSceneName;

    public void OnClickBack()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("targetSceneName이 비어 있습니다.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}