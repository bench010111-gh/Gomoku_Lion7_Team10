using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using BackEnd;
using System.Collections;

public class BackendAuthManager : MonoBehaviour
{
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text resultText;

    private const string TableName = "USER_DATA";

    IEnumerator Start()
    {
        yield return null;

        EventSystem.current.SetSelectedGameObject(null);

        yield return null;

        FocusInput(idInput);
    }

    void FocusInput(TMP_InputField input)
    {
        input.Select();
        input.ActivateInputField();

        EventSystem.current.SetSelectedGameObject(input.gameObject);
    }

    void Update()
    {
        HandleTabNavigation();
        HandleEnterKey();
    }

    // -------------------------
    // Tab / Shift+Tab 이동 처리
    // -------------------------
    void HandleTabNavigation()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!isShift)
        {
            if (current == idInput.gameObject)
            {
                FocusInput(pwInput);
            }
        }
        else
        {
            if (current == pwInput.gameObject)
            {
                FocusInput(idInput);
            }
        }
    }

    // -------------------------
    // Enter → 로그인
    // -------------------------
    void HandleEnterKey()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnClickLogin();
        }
    }

    // -------------------------
    // 회원가입
    // -------------------------
    public void OnClickSignUp()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();

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

    // -------------------------
    // 로그인
    // -------------------------
    public void OnClickLogin()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();

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

        // 닉네임 설정
        Backend.BMember.UpdateNickname(id);

        // 유저 데이터 확인 및 생성
        EnsureUserDataExists(id);

        SceneTransitionManager.Instance.ChangeScene("03_MainLobbyScene");
        //SceneManager.LoadScene("03_MainLobbyScene");
    }

    // -------------------------
    // 유저 데이터 체크
    // -------------------------
    private void EnsureUserDataExists(string nickname)
    {
        var bro = Backend.PlayerData.GetMyData(
            TableName,
            new string[] { "nickname", "winCount", "loseCount", "drawCount" },
            1
        );

        if (!bro.IsSuccess())
        {
            Debug.LogError("유저 데이터 조회 실패: " + bro);
            return;
        }

        if (bro.FlattenRows().Count <= 0)
        {
            bool created = PlayerDataService.CreateDefaultData(nickname);

            if (created)
                Debug.Log("처음 유저 데이터 생성 성공");
            else
                Debug.LogError("처음 유저 데이터 생성 실패");
        }
        else
        {
            Debug.Log("기존 유저 데이터 존재");
        }
    }
}