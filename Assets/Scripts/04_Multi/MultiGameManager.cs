using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

// 멀티 게임 씬에서 색상 배정, 준비/시작, 턴 관리, 임시 착수 규칙 검사 및 돌 배치 동기화를 담당하는 스크립트
// 현재는 빈 칸 여부와 자기 차례만 검사하며, 승패/금수/초읽기 등은 추후 별도 규칙 스크립트로 확장 예정

public class MultiGameManager : MonoBehaviourPunCallbacks
{
    [Header("Board")]
    public Transform boardRoot;
    public Vector2 boardOrigin = Vector2.zero;   // 좌하단 첫 교차점 기준
    public float cellSize = 1f;                  // 교차점 간격

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;

    [Header("UI")]
    public TMP_Text myStoneText;
    public TMP_Text turnText;
    public TMP_Text statusText;

    private BoardData boardData = new BoardData();
    private TempRule tempRule = new TempRule();

    private const string PROP_BLACK_ACTOR = "blackActor";
    private const string PROP_WHITE_ACTOR = "whiteActor";
    private const string PROP_BLACK_READY = "blackReady";
    private const string PROP_WHITE_START = "whiteStart";
    private const string PROP_GAME_STARTED = "gameStarted";
    private const string PROP_CURRENT_TURN = "currentTurn";

    private void Start()
    {
        boardData.ClearBoard();

        if (PhotonNetwork.IsMasterClient)
        {
            InitializeRoomPropertiesIfNeeded();
        }

        UpdateUI();
    }

    private void Update()
    {
        if (!IsGameStarted())
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        TryClickBoard();
    }

    // -----------------------------
    // Room Init
    // -----------------------------
    private void InitializeRoomPropertiesIfNeeded()
    {
        Hashtable props = new Hashtable();
        bool changed = false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_BLACK_ACTOR))
        {
            props[PROP_BLACK_ACTOR] = PhotonNetwork.MasterClient.ActorNumber;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_WHITE_ACTOR))
        {
            int whiteActor = FindOtherPlayerActorNumber();
            props[PROP_WHITE_ACTOR] = whiteActor;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_BLACK_READY))
        {
            props[PROP_BLACK_READY] = false;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_WHITE_START))
        {
            props[PROP_WHITE_START] = false;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_GAME_STARTED))
        {
            props[PROP_GAME_STARTED] = false;
            changed = true;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_CURRENT_TURN))
        {
            props[PROP_CURRENT_TURN] = (int)StoneType.Black;
            changed = true;
        }

        if (changed)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    private int FindOtherPlayerActorNumber()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != PhotonNetwork.MasterClient.ActorNumber)
                return player.ActorNumber;
        }

        return -1;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        int blackActor = GetBlackActor();
        int whiteActor = GetWhiteActor();

        if (whiteActor == -1 && newPlayer.ActorNumber != blackActor)
        {
            Hashtable props = new Hashtable();
            props[PROP_WHITE_ACTOR] = newPlayer.ActorNumber;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        UpdateUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ResetRoomAfterPlayerLeft();
        }

        UpdateUI();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        UpdateUI();
    }

    // -----------------------------
    // UI Actions
    // -----------------------------
    public void OnClickSwapStone()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            SetStatus("방장만 색상을 바꿀 수 있습니다.");
            return;
        }

        if (IsGameStarted())
        {
            SetStatus("게임 시작 후에는 색상을 바꿀 수 없습니다.");
            return;
        }

        int blackActor = GetBlackActor();
        int whiteActor = GetWhiteActor();

        Hashtable props = new Hashtable();
        props[PROP_BLACK_ACTOR] = whiteActor;
        props[PROP_WHITE_ACTOR] = blackActor;
        props[PROP_BLACK_READY] = false;
        props[PROP_WHITE_START] = false;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void OnClickReady()
    {
        if (GetMyStone() != StoneType.Black)
        {
            SetStatus("흑돌만 준비 버튼을 누를 수 있습니다.");
            return;
        }

        if (IsGameStarted())
        {
            SetStatus("이미 게임이 시작되었습니다.");
            return;
        }

        Hashtable props = new Hashtable();
        props[PROP_BLACK_READY] = true;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void OnClickStartGame()
    {
        if (GetMyStone() != StoneType.White)
        {
            SetStatus("백돌만 시작 버튼을 누를 수 있습니다.");
            return;
        }

        if (IsGameStarted())
        {
            SetStatus("이미 게임이 시작되었습니다.");
            return;
        }

        if (!GetBlackReady())
        {
            SetStatus("흑돌이 아직 준비되지 않았습니다.");
            return;
        }

        // 새 게임 시작 전 기존 돌 제거
        photonView.RPC(nameof(RPC_ClearBoardVisuals), RpcTarget.All);

        Hashtable props = new Hashtable();
        props[PROP_WHITE_START] = true;
        props[PROP_GAME_STARTED] = true;
        props[PROP_CURRENT_TURN] = (int)StoneType.Black;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void OnClickResign()
    {
        if (!IsGameStarted())
        {
            SetStatus("게임 진행 중일 때만 기권할 수 있습니다.");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            HandleResignAsMaster(PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestResign), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    // -----------------------------
    // Click / Placement
    // -----------------------------
    private void TryClickBoard()
    {
        StoneType myStone = GetMyStone();
        StoneType currentTurn = GetCurrentTurn();

        if (myStone == StoneType.Empty)
        {
            SetStatus("관전자 또는 색상 미지정 상태입니다.");
            return;
        }

        if (currentTurn != myStone)
        {
            SetStatus("지금은 내 차례가 아닙니다.");
            return;
        }

        Vector3 mouseWorld3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseWorld = new Vector2(mouseWorld3.x, mouseWorld3.y);

        if (TryGetBoardIndex(mouseWorld, out int x, out int y))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                TryPlaceStoneAsMaster(x, y, myStone);
            }
            else
            {
                photonView.RPC(nameof(RPC_RequestPlaceStone), RpcTarget.MasterClient, x, y, (int)myStone);
            }
        }
    }

    private bool TryGetBoardIndex(Vector2 worldPos, out int x, out int y)
    {
        Vector2 local = worldPos - boardOrigin;

        x = Mathf.RoundToInt(local.x / cellSize);
        y = Mathf.RoundToInt(local.y / cellSize);

        return boardData.IsInside(x, y);
    }

    [PunRPC]
    private void RPC_RequestPlaceStone(int x, int y, int stoneValue, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        StoneType requestedStone = (StoneType)stoneValue;

        int senderActor = info.Sender.ActorNumber;
        StoneType senderStone = GetStoneByActor(senderActor);

        if (senderStone != requestedStone)
            return;

        TryPlaceStoneAsMaster(x, y, requestedStone);
    }

    private void TryPlaceStoneAsMaster(int x, int y, StoneType requestedStone)
    {
        if (!IsGameStarted())
        {
            SetStatus("아직 게임이 시작되지 않았습니다.");
            return;
        }

        StoneType currentTurn = GetCurrentTurn();

        if (!tempRule.CanPlaceStone(boardData, x, y, currentTurn, requestedStone))
        {
            SetStatus($"착수 불가: ({x}, {y})");
            return;
        }

        boardData.SetCell(x, y, requestedStone);

        photonView.RPC(nameof(RPC_PlaceStone), RpcTarget.All, x, y, (int)requestedStone);

        Hashtable props = new Hashtable();
        props[PROP_CURRENT_TURN] = (int)(requestedStone == StoneType.Black ? StoneType.White : StoneType.Black);
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    [PunRPC]
    private void RPC_RequestResign(int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        int realActor = info.Sender.ActorNumber;
        HandleResignAsMaster(realActor);
    }

    private void HandleResignAsMaster(int actorNumber)
    {
        StoneType stone = GetStoneByActor(actorNumber);
        string playerName = GetPlayerNameByActor(actorNumber);
        string stoneText = stone == StoneType.Black ? "흑" : "백";

        if (PhotonChatManager.Instance != null)
        {
            PhotonChatManager.Instance.BroadcastSystemMessage($"{playerName} ({stoneText}) 님이 기권했습니다.");
        }

        ResetMatchStateOnMaster(true);
    }

    private void ResetRoomAfterPlayerLeft()
    {
        int playerCount = PhotonNetwork.PlayerList.Length;

        Hashtable props = new Hashtable();
        props[PROP_BLACK_READY] = false;
        props[PROP_WHITE_START] = false;
        props[PROP_GAME_STARTED] = false;
        props[PROP_CURRENT_TURN] = (int)StoneType.Black;

        if (playerCount == 1)
        {
            int remainingActor = PhotonNetwork.PlayerList[0].ActorNumber;
            props[PROP_BLACK_ACTOR] = remainingActor;
            props[PROP_WHITE_ACTOR] = -1;
        }
        else if (playerCount >= 2)
        {
            props[PROP_BLACK_ACTOR] = PhotonNetwork.PlayerList[0].ActorNumber;
            props[PROP_WHITE_ACTOR] = PhotonNetwork.PlayerList[1].ActorNumber;
        }
        else
        {
            props[PROP_BLACK_ACTOR] = -1;
            props[PROP_WHITE_ACTOR] = -1;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        photonView.RPC(nameof(RPC_ClearBoardVisuals), RpcTarget.All);
    }

    private void ResetMatchStateOnMaster(bool clearBoard)
    {
        Hashtable props = new Hashtable();
        props[PROP_BLACK_READY] = false;
        props[PROP_WHITE_START] = false;
        props[PROP_GAME_STARTED] = false;
        props[PROP_CURRENT_TURN] = (int)StoneType.Black;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        if (clearBoard)
        {
            photonView.RPC(nameof(RPC_ClearBoardVisuals), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_PlaceStone(int x, int y, int stoneValue)
    {
        StoneType stone = (StoneType)stoneValue;

        boardData.SetCell(x, y, stone);
        SpawnStoneVisual(x, y, stone);
        UpdateUI();
    }

    [PunRPC]
    private void RPC_ClearBoardVisuals()
    {
        boardData.ClearBoard();

        if (boardRoot != null)
        {
            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(boardRoot.GetChild(i).gameObject);
            }
        }

        UpdateUI();
    }

    private void SpawnStoneVisual(int x, int y, StoneType stone)
    {
        GameObject prefab = null;

        if (stone == StoneType.Black)
            prefab = blackStonePrefab;
        else if (stone == StoneType.White)
            prefab = whiteStonePrefab;

        if (prefab == null)
        {
            return;
        }

        Vector2 pos = boardOrigin + new Vector2(x * cellSize, y * cellSize);

        Instantiate(prefab, pos, Quaternion.identity, boardRoot);
    }

    // -----------------------------
    // Helpers
    // -----------------------------
    private StoneType GetMyStone()
    {
        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
        return GetStoneByActor(myActor);
    }

    private StoneType GetStoneByActor(int actorNumber)
    {
        if (actorNumber == GetBlackActor())
            return StoneType.Black;

        if (actorNumber == GetWhiteActor())
            return StoneType.White;

        return StoneType.Empty;
    }

    private string GetPlayerNameByActor(int actorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber)
                return player.NickName;
        }

        return "알 수 없음";
    }

    private int GetBlackActor()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_BLACK_ACTOR))
            return (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_BLACK_ACTOR];

        return -1;
    }

    private int GetWhiteActor()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_WHITE_ACTOR))
            return (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_WHITE_ACTOR];

        return -1;
    }

    private bool GetBlackReady()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_BLACK_READY))
            return (bool)PhotonNetwork.CurrentRoom.CustomProperties[PROP_BLACK_READY];

        return false;
    }

    private bool GetWhiteStart()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_WHITE_START))
            return (bool)PhotonNetwork.CurrentRoom.CustomProperties[PROP_WHITE_START];

        return false;
    }

    private bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_GAME_STARTED))
            return (bool)PhotonNetwork.CurrentRoom.CustomProperties[PROP_GAME_STARTED];

        return false;
    }

    private StoneType GetCurrentTurn()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_CURRENT_TURN))
            return (StoneType)(int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_CURRENT_TURN];

        return StoneType.Black;
    }

    private void UpdateUI()
    {
        StoneType myStone = GetMyStone();
        StoneType turn = GetCurrentTurn();

        if (myStoneText != null)
        {
            if (myStone == StoneType.Black)
                myStoneText.text = "내 돌: 흑";
            else if (myStone == StoneType.White)
                myStoneText.text = "내 돌: 백";
            else
                myStoneText.text = "내 돌: 없음";
        }

        if (turnText != null)
        {
            turnText.text = $"현재 턴: {(turn == StoneType.Black ? "흑" : "백")}";
        }

        if (!IsGameStarted())
        {
            if (GetBlackReady() && !GetWhiteStart())
                SetStatus("흑 준비 완료. 백이 시작 버튼을 눌러야 합니다.");
            else if (!GetBlackReady())
                SetStatus("흑이 준비 버튼을 눌러야 합니다.");
            else
                SetStatus("게임 시작 대기 중");
        }
        else
        {
            SetStatus($"게임 진행 중 - {(turn == StoneType.Black ? "흑" : "백")} 차례");
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}