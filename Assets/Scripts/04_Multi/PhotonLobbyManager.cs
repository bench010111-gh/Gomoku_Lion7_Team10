using System.Collections.Generic;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

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

    [Header("Room Search")]
    public TMP_InputField searchRoomInput;

    private string currentSearchKeyword = "";
    private readonly Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private RoomInfo selectedRoomInfo;

    private void Start()
    {
        string playerNickname = "Guest";

        if (UserSession.Instance != null && !string.IsNullOrEmpty(UserSession.Instance.nickname))
        {
            playerNickname = UserSession.Instance.nickname;
        }

        PhotonNetwork.NickName = playerNickname;

        // 서로 다른 빌드/에디터에서 버전이 달라져 방이 안 보이는 문제 방지
        PhotonNetwork.GameVersion = "1.0";

        if (playerName != null)
        {
            playerName.text = playerNickname;
        }

        if (createRoomPopup != null) createRoomPopup.SetActive(false);
        if (joinRoomPopup != null) joinRoomPopup.SetActive(false);

        if (PhotonNetwork.InRoom)
        {
            SetStatus("이전 방에서 나가는 중...");
            PhotonNetwork.LeaveRoom();
            return;
        }

        EnsurePhotonLobby();
    }

    private void EnsurePhotonLobby()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SetStatus("Photon 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        if (PhotonNetwork.InLobby)
        {
            SetStatus("로비 입장 완료");
            return;
        }

        if (PhotonNetwork.NetworkClientState == ClientState.JoiningLobby)
        {
            SetStatus("로비 입장 중...");
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            SetStatus("로비 입장 중...");
            PhotonNetwork.JoinLobby();
            return;
        }

        SetStatus("Photon 연결 준비 중...");
    }

    public override void OnConnectedToMaster()
    {
        SetStatus("Photon 연결 성공");

        if (!PhotonNetwork.InLobby)
        {
            SetStatus("로비 입장 중...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        SetStatus("로비 입장 완료");
    }

    public override void OnLeftLobby()
    {
        SetStatus("로비에서 나갔습니다.");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SetStatus("Photon 연결이 끊어졌습니다.");
        Debug.LogWarning("Photon disconnected: " + cause);
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

    public void OnClickSearchRoom()
    {
        if (searchRoomInput == null)
        {
            SetStatus("검색 입력 UI가 연결되지 않았습니다.");
            return;
        }

        currentSearchKeyword = searchRoomInput.text.Trim().ToLower();
        RefreshRoomListUI();
    }

    public void OnClickCancelSearch()
    {
        currentSearchKeyword = "";

        if (searchRoomInput != null)
        {
            searchRoomInput.text = "";
        }

        RefreshRoomListUI();
    }

    private void RefreshRoomListUI()
    {
        if (roomListContent == null || roomListItemPrefab == null)
        {
            Debug.LogWarning("Room list UI is not assigned.");
            return;
        }

        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (RoomInfo roomInfo in cachedRoomList.Values)
        {
            if (!roomInfo.IsVisible)
                continue;

            if (!string.IsNullOrEmpty(currentSearchKeyword))
            {
                string roomNameLower = roomInfo.Name.ToLower();

                if (!roomNameLower.Contains(currentSearchKeyword))
                    continue;
            }

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

    public void OnClickOpenCreateRoomPopup()
    {
        if (!PhotonNetwork.InLobby)
        {
            SetStatus("아직 로비 입장 전입니다.");
            EnsurePhotonLobby();
            return;
        }

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
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            SetStatus("Photon 연결이 아직 준비되지 않았습니다.");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            SetStatus("이미 방에 들어가 있습니다.");
            return;
        }

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
        options.IsVisible = true;
        options.IsOpen = true;
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
        SetStatus("방 생성 중...");

        if (createRoomPopup != null)
        {
            createRoomPopup.SetActive(false);
        }
    }

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
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            SetStatus("Photon 연결이 아직 준비되지 않았습니다.");
            return;
        }

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
        SetStatus("방 입장 중...");

        if (joinRoomPopup != null)
        {
            joinRoomPopup.SetActive(false);
        }
    }

    public override void OnCreatedRoom()
    {
        SetStatus("방 생성 완료");
    }

    public override void OnJoinedRoom()
    {
        SetStatus("방 입장 완료");
        SceneManager.LoadScene(multiGameSceneName);
    }

    public override void OnLeftRoom()
    {
        SetStatus("방에서 나왔습니다.");
        EnsurePhotonLobby();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus("방 생성 실패");
        Debug.LogWarning($"Create room failed: {returnCode} / {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetStatus("방 입장 실패");
        Debug.LogWarning($"Join room failed: {returnCode} / {message}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        SetStatus("방 입장 실패");
        Debug.LogWarning($"Join random failed: {returnCode} / {message}");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log(message);
    }
}