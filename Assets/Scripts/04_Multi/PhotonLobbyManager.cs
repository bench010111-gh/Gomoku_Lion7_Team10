using System.Collections.Generic;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

// 멀티 로비 씬에서 Photon 연결, 로비 입장, 방 목록 갱신, 방 생성/입장을 관리하는 스크립트
// 공개/비공개 방 생성 팝업과 입장 확인 팝업을 제어, 방 입장 성공 시 멀티 게임 씬으로 이동

public class PhotonLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Top UI")]
    public TMP_Text statusText;
    public TMP_Text playerName;

    [Header("Next Scene")]
    public string multiGameSceneName = "07_MultiGameScene";

    [Header("Create Room Popup")]
    public GameObject createRoomPopup;
    public TMP_InputField createRoomNameInput;
    public Toggle privateToggle;
    public TMP_InputField passwordInput;

    [Header("Join Room Popup")]
    public GameObject joinRoomPopup;
    public TMP_Text selectedRoomTitleText;
    public TMP_InputField joinPasswordInput;
    public TMP_Text joinPopupPrivateLabel;

    [Header("Room List")]
    public Transform roomListContent;
    public GameObject roomListItemPrefab;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private RoomInfo selectedRoomInfo;

    private void Start()
    {
        string playerNickname = "Guest";

        if (UserSession.Instance != null)
        {
            playerNickname = UserSession.Instance.nickname;
        }

        if (playerName != null)
        {
            playerName.text = playerNickname;
        }

        if (createRoomPopup != null) createRoomPopup.SetActive(false);
        if (joinRoomPopup != null) joinRoomPopup.SetActive(false);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = playerNickname;
            PhotonNetwork.ConnectUsingSettings();
            SetStatus("Photon 연결 중...");
        }
        else
        {
            SetStatus("Photon 이미 연결됨");

            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                SetStatus("Photon 로비 입장 중...");
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        SetStatus("Photon 연결 성공, 로비 입장 중...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        SetStatus("로비 입장 성공");
    }

    public override void OnLeftLobby()
    {
        SetStatus("로비에서 나갔습니다.");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        RefreshRoomListUI();
    }

    private void RefreshRoomListUI()
    {
        if (roomListContent == null || roomListItemPrefab == null)
            return;

        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo roomInfo in cachedRoomList.Values)
        {
            if (!roomInfo.IsVisible)
                continue;

            GameObject item = Instantiate(roomListItemPrefab, roomListContent);

            TMP_Text itemText = item.GetComponentInChildren<TMP_Text>();
            Button itemButton = item.GetComponent<Button>();

            string hostName = "알 수 없음";
            bool isPrivate = false;

            if (roomInfo.CustomProperties.ContainsKey("host"))
            {
                hostName = roomInfo.CustomProperties["host"].ToString();
            }

            if (roomInfo.CustomProperties.ContainsKey("priv"))
            {
                isPrivate = (bool)roomInfo.CustomProperties["priv"];
            }

            string privateText = isPrivate ? "비공개" : "공개";

            if (itemText != null)
            {
                itemText.text =
                    $"방 제목: {roomInfo.Name}      " +
                    $"방장: {hostName}\n" +
                    $"공개 여부: {privateText}      " +
                    $"인원: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
            }

            if (itemButton != null)
            {
                RoomInfo capturedRoomInfo = roomInfo;
                itemButton.onClick.AddListener(() => OpenJoinRoomPopup(capturedRoomInfo));
            }
        }
    }

    // -----------------------------
    // Create Room Popup
    // -----------------------------
    public void OnClickOpenCreateRoomPopup()
    {
        if (createRoomPopup != null)
        {
            createRoomPopup.SetActive(true);
        }

        if (createRoomNameInput != null)
        {
            createRoomNameInput.text = "";
        }

        if (passwordInput != null)
        {
            passwordInput.text = "";
        }

        if (privateToggle != null)
        {
            privateToggle.isOn = false;
        }
    }

    public void OnClickCloseCreateRoomPopup()
    {
        if (createRoomPopup != null)
        {
            createRoomPopup.SetActive(false);
        }
    }

    public void OnClickCreateRoomConfirm()
    {
        if (createRoomNameInput == null)
        {
            SetStatus("방 이름 입력 UI가 연결되지 않았습니다.");
            return;
        }

        string roomName = createRoomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            SetStatus("방 이름을 입력하세요.");
            return;
        }

        bool isPrivate = privateToggle != null && privateToggle.isOn;
        string password = passwordInput != null ? passwordInput.text.Trim() : "";

        if (isPrivate && string.IsNullOrEmpty(password))
        {
            SetStatus("비공개 방은 비밀번호를 입력해야 합니다.");
            return;
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        options.EmptyRoomTtl = 0;

        Hashtable props = new Hashtable();
        props["host"] = PhotonNetwork.NickName;
        props["priv"] = isPrivate;

        if (isPrivate)
        {
            props["pw"] = password;
        }

        options.CustomRoomProperties = props;
        options.CustomRoomPropertiesForLobby = new string[] { "host", "priv", "pw" };

        PhotonNetwork.CreateRoom(roomName, options);
        SetStatus("방 생성 시도 중...");

        if (createRoomPopup != null)
        {
            createRoomPopup.SetActive(false);
        }
    }

    // -----------------------------
    // Join Room Popup
    // -----------------------------
    private void OpenJoinRoomPopup(RoomInfo roomInfo)
    {
        selectedRoomInfo = roomInfo;

        if (joinRoomPopup != null)
        {
            joinRoomPopup.SetActive(true);
        }

        if (selectedRoomTitleText != null)
        {
            selectedRoomTitleText.text = $"'{roomInfo.Name}' 방에 입장하시겠습니까?";
        }

        bool isPrivate = false;
        if (roomInfo.CustomProperties.ContainsKey("priv"))
        {
            isPrivate = (bool)roomInfo.CustomProperties["priv"];
        }

        if (joinPasswordInput != null)
        {
            joinPasswordInput.text = "";
            joinPasswordInput.gameObject.SetActive(isPrivate);
        }

        if (joinPopupPrivateLabel != null)
        {
            joinPopupPrivateLabel.text = isPrivate ? "비공개 방 - 비밀번호 입력" : "공개 방";
        }
    }

    public void OnClickCloseJoinRoomPopup()
    {
        selectedRoomInfo = null;

        if (joinRoomPopup != null)
        {
            joinRoomPopup.SetActive(false);
        }
    }

    public void OnClickJoinRoomYes()
    {
        if (selectedRoomInfo == null)
        {
            SetStatus("선택된 방 정보가 없습니다.");
            return;
        }

        bool isPrivate = false;
        string savedPassword = "";

        if (selectedRoomInfo.CustomProperties.ContainsKey("priv"))
        {
            isPrivate = (bool)selectedRoomInfo.CustomProperties["priv"];
        }

        if (selectedRoomInfo.CustomProperties.ContainsKey("pw"))
        {
            savedPassword = selectedRoomInfo.CustomProperties["pw"].ToString();
        }

        if (isPrivate)
        {
            string inputPassword = joinPasswordInput != null ? joinPasswordInput.text.Trim() : "";

            if (string.IsNullOrEmpty(inputPassword))
            {
                SetStatus("비밀번호를 입력하세요.");
                return;
            }

            if (inputPassword != savedPassword)
            {
                SetStatus("비밀번호가 올바르지 않습니다.");
                return;
            }
        }

        PhotonNetwork.JoinRoom(selectedRoomInfo.Name);
        SetStatus("방 입장 시도 중...");

        if (joinRoomPopup != null)
        {
            joinRoomPopup.SetActive(false);
        }
    }

    // -----------------------------
    // Photon Callbacks
    // -----------------------------
    public override void OnCreatedRoom()
    {
        SetStatus("방 생성 성공");
    }

    public override void OnJoinedRoom()
    {
        SetStatus("방 입장 성공");
        SceneManager.LoadScene(multiGameSceneName);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus("방 생성 실패: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetStatus("방 입장 실패: " + message);
    }

    // -----------------------------
    // Utility
    // -----------------------------
    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log(message);
    }
}