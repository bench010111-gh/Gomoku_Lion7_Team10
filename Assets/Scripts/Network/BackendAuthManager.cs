using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using BackEnd;

public class BackendAuthManager : MonoBehaviour
{
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text resultText;

    private const string TableName = "USER_DATA";

    public void OnClickSignUp()
    {
        string id = idInput.text.Trim();
        string pw = pwInput.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            resultText.text = "아이디와 비밀번호를 입력하세요.";
            return;
        }

        var bro = Backend.BMember.CustomSignUp(id, pw);

        if (bro.IsSuccess())
        {
            resultText.text = "회원가입 성공";
        }
        else
        {
            resultText.text = "회원가입 실패: " + bro;
        }
    }

    public void OnClickLogin()
    {
        string id = idInput.text.Trim();
        string pw = pwInput.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            resultText.text = "아이디와 비밀번호를 입력하세요.";
            return;
        }

        var bro = Backend.BMember.CustomLogin(id, pw);

        if (!bro.IsSuccess())
        {
            resultText.text = "로그인 실패: " + bro;
            return;
        }

        resultText.text = "로그인 성공";

        // 세션 저장
        UserSession.Instance.userId = id;
        UserSession.Instance.nickname = id;

        // 닉네임도 일단 아이디로 설정
        Backend.BMember.UpdateNickname(id);

        // 처음 유저인지 확인 후 기본 데이터 생성
        EnsureUserDataExists(id);

        SceneManager.LoadScene("03_MainLobbyScene");
    }

    private void EnsureUserDataExists(string nickname)
    {
        var bro = Backend.PlayerData.GetMyData(TableName, new string[] { "nickname", "winCount", "loseCount", "drawCount" }, 1);

        if (!bro.IsSuccess())
        {
            Debug.LogError("유저 데이터 조회 실패: " + bro);
            return;
        }

        // 데이터가 없으면 새로 생성
        if (bro.FlattenRows().Count <= 0)
        {
            bool created = PlayerDataService.CreateDefaultData(nickname);

            if (created)
            {
                Debug.Log("처음 유저 데이터 생성 성공");
            }
            else
            {
                Debug.LogError("처음 유저 데이터 생성 실패");
            }
        }
        else
        {
            Debug.Log("기존 유저 데이터 존재");
        }
    }
}