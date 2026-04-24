using TMPro;
using UnityEngine;
using Photon.Pun;

// 멀티 게임 씬에서 현재 방 제목을 표시하고, 같은 Photon 방에 있는 플레이어끼리 실시간 채팅을 주고받는 스크립트
// 전송 버튼 클릭 시 RPC를 통해 모든(같은 방에 있는) 클라이언트에 메시지를 전달하고, 채팅 로그 UI에 시간과 발신자를 함께 출력

public class PhotonChatManager : MonoBehaviourPun
{
    public TMP_InputField chatInput;
    public TMP_Text chatLogText;
    public TMP_Text roomTitleText;

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            if (roomTitleText != null)
            {
                roomTitleText.text = $"방 제목: {PhotonNetwork.CurrentRoom.Name}";
            }

            AddChatSystemMessage($"방 입장 확인: {PhotonNetwork.CurrentRoom.Name}");
        }
        else
        {
            if (roomTitleText != null)
            {
                roomTitleText.text = "방 제목: 없음";
            }

            AddChatSystemMessage("현재 방에 들어가 있지 않습니다.");
        }
    }

    public void OnClickSendChat()
    {
        if (!PhotonNetwork.InRoom)
        {
            AddChatSystemMessage("방에 들어간 뒤 채팅할 수 있습니다.");
            return;
        }

        if (chatInput == null)
        {
            Debug.LogError("chatInput이 연결되지 않았습니다.");
            return;
        }

        string msg = chatInput.text.Trim();

        if (string.IsNullOrEmpty(msg))
            return;

        photonView.RPC(nameof(RPC_ReceiveChat), RpcTarget.All, PhotonNetwork.NickName, msg);
        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    [PunRPC]
    private void RPC_ReceiveChat(string sender, string message)
    {
        string timeStamp = System.DateTime.Now.ToString("HH:mm");
        AddChat($"[{timeStamp}] [{sender}] {message}");
    }

    private void AddChatSystemMessage(string message)
    {
        string timeStamp = System.DateTime.Now.ToString("HH:mm");
        AddChat($"[{timeStamp}] [SYSTEM] {message}");
    }

    private void AddChat(string message)
    {
        if (chatLogText == null)
        {
            Debug.LogError("chatLogText가 연결되지 않았습니다.");
            return;
        }

        chatLogText.text += message + "\n";
    }
}