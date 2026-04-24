using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

// 멀티 게임 씬에서 현재 방 제목을 표시하고, 같은 Photon 방에 있는 플레이어끼리 실시간 채팅을 주고받는 스크립트
// Enter 키로 채팅 입력창을 활성화할 수 있으며, 입력창 포커스 상태에서는 Enter 입력 또는 전송 버튼 클릭으로 채팅을 전송한다.

public class PhotonChatManager : MonoBehaviourPunCallbacks
{
    public static PhotonChatManager Instance;

    [Header("Chat UI")]
    public TMP_InputField chatInput;
    public TMP_Text roomTitleText;

    [Header("Chat Scroll")]
    public Transform chatContent;
    public GameObject chatItemPrefab;
    public ScrollRect chatScrollRect;

    [Header("Option")]
    public int maxChatCount = 100;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (chatInput != null)
        {
            // 입력창 포커스 상태에서 Enter 제출 시 전송
            chatInput.onSubmit.AddListener(OnSubmitChat);
            chatInput.onEndEdit.AddListener(OnEndEditChat);

            // 채팅은 한 줄 입력 기준
            chatInput.lineType = TMP_InputField.LineType.SingleLine;
        }

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

    private void OnDestroy()
    {
        if (chatInput != null)
        {
            chatInput.onSubmit.RemoveListener(OnSubmitChat);
            chatInput.onEndEdit.RemoveListener(OnEndEditChat);
        }
    }

    private void Update()
    {
        if (chatInput == null)
            return;

        // 포커스가 없을 때만 Enter로 채팅 입력 시작
        if (!chatInput.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            chatInput.Select();
            chatInput.ActivateInputField();
        }
    }

    private void OnSubmitChat(string _)
    {
        // 포커스 상태에서 제출되면 전송
        SendChatFromInput();
    }

    private void OnEndEditChat(string _)
    {
        if (chatInput == null)
            return;

        // Enter로 EndEdit된 경우 전송
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendChatFromInput();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddChatSystemMessage($"{newPlayer.NickName}님이 입장했습니다.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        AddChatSystemMessage($"{otherPlayer.NickName}님이 퇴장했습니다.");
    }

    public void OnClickSendChat()
    {
        SendChatFromInput();
    }

    private void SendChatFromInput()
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
        {
            chatInput.text = "";
            chatInput.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        photonView.RPC(nameof(RPC_ReceiveChat), RpcTarget.All, PhotonNetwork.NickName, msg);

        chatInput.text = "";
        chatInput.DeactivateInputField();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void BroadcastSystemMessage(string message)
    {
        if (!PhotonNetwork.InRoom)
        {
            AddChatSystemMessage(message);
            return;
        }

        photonView.RPC(nameof(RPC_ReceiveSystemMessage), RpcTarget.All, message);
    }

    [PunRPC]
    private void RPC_ReceiveChat(string sender, string message)
    {
        string timeStamp = System.DateTime.Now.ToString("HH:mm");
        AddChat($"[{timeStamp}] [{sender}] {message}");
    }

    [PunRPC]
    private void RPC_ReceiveSystemMessage(string message)
    {
        AddChatSystemMessage(message);
    }

    private void AddChatSystemMessage(string message)
    {
        string timeStamp = System.DateTime.Now.ToString("HH:mm");
        AddChat($"[{timeStamp}] [SYSTEM] {message}");
    }

    private void AddChat(string message)
    {
        if (chatContent == null || chatItemPrefab == null)
        {
            Debug.LogError("chatContent 또는 chatItemPrefab이 연결되지 않았습니다.");
            return;
        }

        GameObject item = Instantiate(chatItemPrefab, chatContent);
        TMP_Text itemText = item.GetComponentInChildren<TMP_Text>();

        if (itemText != null)
        {
            itemText.text = message;
        }

        TrimOldChats();
        ScrollToBottom();
    }

    private void TrimOldChats()
    {
        if (maxChatCount <= 0 || chatContent == null)
            return;

        while (chatContent.childCount > maxChatCount)
        {
            Destroy(chatContent.GetChild(0).gameObject);
        }
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();

        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}