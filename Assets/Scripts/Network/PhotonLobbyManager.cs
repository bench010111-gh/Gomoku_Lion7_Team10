using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class PhotonLobbyManager : MonoBehaviourPunCallbacks
{
    public TMP_Text statusText;
    public TMP_InputField roomNameInput;
    public TMP_Text playerName;

    [Header("Next Scene")]
    public string multiGameSceneName = "07_MultiGameScene";

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

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = playerNickname;
            PhotonNetwork.ConnectUsingSettings();
            statusText.text = "Photon 연결 중...";
        }
        else
        {
            statusText.text = "Photon 이미 연결됨";
        }
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Photon 연결 성공";
    }

    public void OnClickCreateRoom()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "방 이름을 입력하세요.";
            return;
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;

        PhotonNetwork.CreateRoom(roomName, options);
        statusText.text = "방 생성 시도 중...";
    }

    public void OnClickJoinRoom()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "방 이름을 입력하세요.";
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
        statusText.text = "방 입장 시도 중...";
    }

    public override void OnCreatedRoom()
    {
        statusText.text = "방 생성 성공";
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "방 입장 성공";
        SceneManager.LoadScene(multiGameSceneName);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "방 생성 실패: " + message;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "방 입장 실패: " + message;
    }
}