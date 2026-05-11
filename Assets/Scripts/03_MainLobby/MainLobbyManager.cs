using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using BackEnd;

// 메인 로비에서 플레이어 닉네임을 표시하고, 싱글/멀티/듀오 로비 씬으로 이동을 관리
// 뒤로가기 버튼 클릭 시 로그아웃 확인 팝업을 띄우며, 로그아웃 성공 시 로그인 씬으로 이동

public class MainLobbyManager : MonoBehaviour
{
    public TMP_Text nicknameText;

    [Header("Scene Names")]
    public string singleLobbyScene = "04_SingleLobbyScene";
    public string multiLobbyScene = "05_MultiLobbyScene";
    public string duoLobbyScene = "06_DuoLobbyScene";
    public string loginScene = "02_LoginScene";

    [Header("Logout Popup")]
    public GameObject logoutPopupPanel;

    [Header("Setting Popup")]
    public GameObject settingPopupPanel;

    [Header("AI Setting Popup")]
    public GameObject aiSettingPopup; 

    void Start()
    {
        if (nicknameText != null && UserSession.Instance != null)
        {
            nicknameText.text = $"회원명: {UserSession.Instance.nickname}";
        }

        if (logoutPopupPanel != null)
        {
            logoutPopupPanel.SetActive(false);
        }

        if (settingPopupPanel != null)
        {
            settingPopupPanel.SetActive(false);
        }

        if(aiSettingPopup != null)
        {
            settingPopupPanel.SetActive(false); 
        }
    }

    public void OnClickSingleMode()
    {
        aiSettingPopup.SetActive(true); 
        // SceneManager.LoadScene(singleLobbyScene);
    }

    public void OnClickMultiMode()
    {
        SceneManager.LoadScene(multiLobbyScene);
    }

    public void OnClickDuoMode()
    {
        SceneManager.LoadScene(duoLobbyScene);
    }

    public void OnClickBack()
    {
        if (logoutPopupPanel != null)
        {
            logoutPopupPanel.SetActive(true);
        }
    }

    public void OnClickLogoutYes()
    {
        var bro = Backend.BMember.Logout();

        if (bro.IsSuccess())
        {
            Debug.Log("접속종료 성공");

            if (UserSession.Instance != null)
            {
                UserSession.Instance.userId = "";
                UserSession.Instance.nickname = "";
            }

            SceneManager.LoadScene(loginScene);
        }
        else
        {
            Debug.LogError("접속종료 실패: " + bro);
        }
    }

    public void OnClickLogoutNo()
    {
        if (logoutPopupPanel != null)
        {
            logoutPopupPanel.SetActive(false);
        }
    }

    public void OnClickSetting()
    {
        if (settingPopupPanel != null)
        {
            settingPopupPanel.SetActive(true);
        }
    }

    public void OnClickCloseSetting()
    {
        if (settingPopupPanel != null)
        {
            settingPopupPanel.SetActive(false);
        }
    }
}