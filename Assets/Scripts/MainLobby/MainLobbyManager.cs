using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using BackEnd;

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

    void Start()
    {
        if (nicknameText != null && UserSession.Instance != null)
        {
            nicknameText.text = $"닉네임: {UserSession.Instance.nickname}";
        }

        if (logoutPopupPanel != null)
        {
            logoutPopupPanel.SetActive(false);
        }
    }

    public void OnClickSingleMode()
    {
        SceneManager.LoadScene(singleLobbyScene);
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
            Debug.Log("로그아웃 성공");

            if (UserSession.Instance != null)
            {
                UserSession.Instance.userId = "";
                UserSession.Instance.nickname = "";
            }

            SceneManager.LoadScene(loginScene);
        }
        else
        {
            Debug.LogError("로그아웃 실패: " + bro);
        }
    }

    public void OnClickLogoutNo()
    {
        if (logoutPopupPanel != null)
        {
            logoutPopupPanel.SetActive(false);
        }
    }
}