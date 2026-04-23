using TMPro;
using UnityEngine;
using Photon.Pun;

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