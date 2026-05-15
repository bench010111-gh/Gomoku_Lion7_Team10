using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

using Hashtable = ExitGames.Client.Photon.Hashtable;

// 멀티 게임 씬에서 현재 방 제목을 표시하고,
// 같은 Photon 방에 있는 플레이어끼리 실시간 채팅을 주고받는 스크립트
// 추가 기능:
// 1. 플레이어 입장 시 전적 표시
// 2. 새로 들어온 플레이어도 기존 플레이어 전적을 볼 수 있도록 Player CustomProperties 사용
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

    private const string PROP_RECORD_TEXT = "recordText";

    // 같은 플레이어 전적이 채팅창에 중복 출력되는 것 방지
    private readonly HashSet<int> announcedRecordActors = new HashSet<int>();

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

            // 내 전적을 Photon Player CustomProperties에 등록
            UpdateMyRecordProperty();

            // 이미 방에 있던 플레이어들의 전적도 출력
            Invoke(nameof(PrintAllPlayerRecords), 0.5f);
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

        if (Instance == this)
        {
            Instance = null;
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
        SendChatFromInput();
    }

    private void OnEndEditChat(string _)
    {
        if (chatInput == null)
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendChatFromInput();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddChatSystemMessage($"{newPlayer.NickName}님이 입장했습니다.");

        // 기존 방에 있던 사람은 자신의 전적 정보를 다시 CustomProperties에 올림
        // 새로 들어온 사람도 기존 사람의 전적을 볼 수 있게 하기 위함
        UpdateMyRecordProperty();

        // 혹시 이미 전달된 정보가 있으면 출력 시도
        Invoke(nameof(PrintAllPlayerRecords), 0.5f);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        AddChatSystemMessage($"{otherPlayer.NickName}님이 퇴장했습니다.");

        if (announcedRecordActors.Contains(otherPlayer.ActorNumber))
        {
            announcedRecordActors.Remove(otherPlayer.ActorNumber);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps == null)
            return;

        if (!changedProps.ContainsKey(PROP_RECORD_TEXT))
            return;

        PrintPlayerRecord(targetPlayer);
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

    private void UpdateMyRecordProperty()
    {
        if (!PhotonNetwork.InRoom)
            return;

        string recordText = PlayerDataService.GetMyRecordText();

        Hashtable props = new Hashtable();
        props[PROP_RECORD_TEXT] = recordText;

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void PrintAllPlayerRecords()
    {
        if (!PhotonNetwork.InRoom)
            return;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            PrintPlayerRecord(player);
        }
    }

    private void PrintPlayerRecord(Player player)
    {
        if (player == null)
            return;

        if (announcedRecordActors.Contains(player.ActorNumber))
            return;

        if (player.CustomProperties == null)
            return;

        if (!player.CustomProperties.ContainsKey(PROP_RECORD_TEXT))
            return;

        string recordText = player.CustomProperties[PROP_RECORD_TEXT].ToString();

        AddChatSystemMessage($"{player.NickName} 전적: {recordText}");

        announcedRecordActors.Add(player.ActorNumber);
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