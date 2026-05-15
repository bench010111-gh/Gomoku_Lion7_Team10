using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;

// 멀티 게임 씬에서 색상 배정, 준비/시작, 턴 관리,
// GomokuRule 기반 착수 규칙 검사, 금수 표시, 승패/무승부/기권/퇴장/시간초과 처리,
// 전적 저장 및 복기용 기보 저장을 담당하는 스크립트
public class MultiGameManager : MonoBehaviourPunCallbacks
{
    [Header("Board")]
    public Transform boardRoot;
    public Vector2 boardOrigin = Vector2.zero;
    public float cellSize = 1f;

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;

    [Header("UI")]
    public TMP_Text myStoneText;
    public TMP_Text turnText;
    public TMP_Text statusText;

    [Header("Turn Timer")]
    public int turnDurationSeconds = 30;

    [Header("Forbidden Mark")]
    public GameObject forbiddenMarkPrefab;
    public Transform forbiddenMarkRoot;
    public bool showForbiddenOnlyToBlackPlayer = true;

    [Header("Result Popup")]
    public GameObject resultPopupPanel;
    public TMP_Text resultTitleText;
    public TMP_Text resultMessageText;

    private BoardData boardData = new BoardData();
    private GomokuRule gomokuRule = new GomokuRule(BoardData.Size);

    private readonly List<GameObject> forbiddenMarks = new List<GameObject>();
    private readonly List<OmokMoveRecord> moveRecords = new List<OmokMoveRecord>();

    private bool hasSavedCurrentMatchResult = false;
    private bool isLeavingAfterLoss = false;
    private bool hasHandledTurnTimeout = false;

    private const string PROP_BLACK_ACTOR = "blackActor";
    private const string PROP_WHITE_ACTOR = "whiteActor";
    private const string PROP_BLACK_READY = "blackReady";
    private const string PROP_WHITE_START = "whiteStart";
    private const string PROP_GAME_STARTED = "gameStarted";
    private const string PROP_CURRENT_TURN = "currentTurn";
    private const string PROP_TURN_START_TIME = "turnStartTime";

    private void Start()
    {
        boardData.ClearBoard();
        moveRecords.Clear();
        hasSavedCurrentMatchResult = false;
        isLeavingAfterLoss = false;
        hasHandledTurnTimeout = false;

        if (PhotonNetwork.IsMasterClient)
        {
            InitializeRoomPropertiesIfNeeded();
        }

        HideResultPopup();
        UpdateUI();
        RefreshForbiddenMarks();
    }

    private void Update()
    {
        UpdateTurnTimerUI();

        if (!IsGameStarted())
            return;

        CheckTurnTimeout();

        if (!Input.GetMouseButtonDown(0))
            return;

        TryClickBoard();
    }

    // -----------------------------
    // Public methods for leave handling
    // -----------------------------
    public bool IsPlayingAsPlayer()
    {
        return IsGameStarted() && GetMyStone() != StoneType.Empty;
    }

    public void RequestLeaveRoomAsLoss()
    {
        if (isLeavingAfterLoss)
            return;

        isLeavingAfterLoss = true;

        if (!PhotonNetwork.InRoom)
            return;

        if (!IsPlayingAsPlayer())
        {
            PhotonNetwork.LeaveRoom();
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

        StartCoroutine(LeaveRoomAfterResultSaved());
    }

    private IEnumerator LeaveRoomAfterResultSaved()
    {
        yield return new WaitForSeconds(1.0f);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
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

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_TURN_START_TIME))
        {
            props[PROP_TURN_START_TIME] = PhotonNetwork.Time;
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
        RefreshForbiddenMarks();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            bool gameWasStarted = IsGameStarted();
            StoneType leftStone = GetStoneByActor(otherPlayer.ActorNumber);

            if (gameWasStarted && leftStone != StoneType.Empty)
            {
                StoneType winner = leftStone == StoneType.Black ? StoneType.White : StoneType.Black;
                string leftStoneText = leftStone == StoneType.Black ? "흑" : "백";

                if (PhotonChatManager.Instance != null)
                {
                    PhotonChatManager.Instance.BroadcastSystemMessage(
                        $"{otherPlayer.NickName} ({leftStoneText}) 님이 방을 나갔습니다. 상대 승리로 처리됩니다."
                    );
                }

                HandleGameOverAsMaster(winner, false, true, otherPlayer.NickName, false);
                ResetRoomAfterPlayerLeft(false);
            }
            else
            {
                ResetRoomAfterPlayerLeft(true);
            }
        }

        UpdateUI();
        RefreshForbiddenMarks();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(PROP_CURRENT_TURN) ||
            propertiesThatChanged.ContainsKey(PROP_TURN_START_TIME))
        {
            hasHandledTurnTimeout = false;
        }

        UpdateUI();
        RefreshForbiddenMarks();
    }

    // -----------------------------
    // Turn Timer
    // -----------------------------
    private void StartTurnTimerOnMaster()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Hashtable props = new Hashtable();
        props[PROP_TURN_START_TIME] = PhotonNetwork.Time;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        hasHandledTurnTimeout = false;
    }

    private double GetTurnStartTime()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return PhotonNetwork.Time;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_TURN_START_TIME))
            return Convert.ToDouble(PhotonNetwork.CurrentRoom.CustomProperties[PROP_TURN_START_TIME]);

        return PhotonNetwork.Time;
    }

    private int GetRemainingTurnSeconds()
    {
        if (!IsGameStarted())
            return turnDurationSeconds;

        double elapsed = PhotonNetwork.Time - GetTurnStartTime();
        int remaining = Mathf.CeilToInt((float)(turnDurationSeconds - elapsed));

        return Mathf.Clamp(remaining, 0, turnDurationSeconds);
    }

    private void UpdateTurnTimerUI()
    {
        if (turnText == null)
            return;

        StoneType turn = GetCurrentTurn();

        if (!IsGameStarted())
        {
            turnText.text = $"현재 차례 : {(turn == StoneType.Black ? "흑" : "백")}";
            return;
        }

        int remaining = GetRemainingTurnSeconds();
        turnText.text = $"현재 차례 : {(turn == StoneType.Black ? "흑" : "백")} {remaining}초";
    }

    private void CheckTurnTimeout()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (hasHandledTurnTimeout)
            return;

        if (!IsGameStarted())
            return;

        int remaining = GetRemainingTurnSeconds();

        if (remaining > 0)
            return;

        hasHandledTurnTimeout = true;
        HandleTurnTimeoutAsMaster();
    }

    private void HandleTurnTimeoutAsMaster()
    {
        StoneType timeoutStone = GetCurrentTurn();

        if (timeoutStone == StoneType.Empty)
            return;

        StoneType winner = timeoutStone == StoneType.Black ? StoneType.White : StoneType.Black;
        string timeoutStoneText = timeoutStone == StoneType.Black ? "흑" : "백";
        string timeoutPlayerName = GetPlayerNameByStone(timeoutStone);

        if (PhotonChatManager.Instance != null)
        {
            PhotonChatManager.Instance.BroadcastSystemMessage(
                $"{timeoutPlayerName} ({timeoutStoneText}) 님의 시간이 초과되었습니다."
            );
        }

        HandleGameOverAsMaster(winner, false, false, "", true);
    }

    private string GetPlayerNameByStone(StoneType stone)
    {
        if (stone == StoneType.Black)
            return GetPlayerNameByActor(GetBlackActor());

        if (stone == StoneType.White)
            return GetPlayerNameByActor(GetWhiteActor());

        return "알 수 없음";
    }

    // -----------------------------
    // UI Actions
    // -----------------------------
    public void OnClickSwapStone()
    {
        PlayClickSound();

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
        PlayClickSound();

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
        PlayClickSound();

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

        photonView.RPC(nameof(RPC_ClearBoardVisuals), RpcTarget.All);

        Hashtable props = new Hashtable();
        props[PROP_WHITE_START] = true;
        props[PROP_GAME_STARTED] = true;
        props[PROP_CURRENT_TURN] = (int)StoneType.Black;
        props[PROP_TURN_START_TIME] = PhotonNetwork.Time;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        hasHandledTurnTimeout = false;
    }

    public void OnClickResign()
    {
        PlayClickSound();

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

    public void OnClickCloseResultPopup()
    {
        PlayClickSound();
        HideResultPopup();
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

        if (Camera.main == null)
        {
            SetStatus("Main Camera가 없습니다.");
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

        if (!CanPlaceStoneByGomokuRule(x, y, currentTurn, requestedStone, out string failReason))
        {
            SetStatus(failReason);
            return;
        }

        boardData.SetCell(x, y, requestedStone);

        photonView.RPC(nameof(RPC_PlaceStone), RpcTarget.All, x, y, (int)requestedStone);

        bool isWin = gomokuRule.CheckWin(boardData.GetArray(), x, y, requestedStone);
        Debug.Log($"[승리판정] stone={requestedStone}, x={x}, y={y}, isWin={isWin}");

        if (isWin)
        {
            Debug.Log("[승리판정] 승리 감지됨. 결과 팝업 RPC 호출");
            HandleGameOverAsMaster(requestedStone, false, false);
            return;
        }

        if (gomokuRule.IsDraw(boardData.GetPlacedStoneCount()))
        {
            HandleGameOverAsMaster(StoneType.Empty, true, false);
            return;
        }

        StoneType nextTurn = requestedStone == StoneType.Black ? StoneType.White : StoneType.Black;

        Hashtable props = new Hashtable();
        props[PROP_CURRENT_TURN] = (int)nextTurn;
        props[PROP_TURN_START_TIME] = PhotonNetwork.Time;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        hasHandledTurnTimeout = false;
    }

    private bool CanPlaceStoneByGomokuRule(int x, int y, StoneType currentTurn, StoneType requestedStone, out string failReason)
    {
        failReason = "";

        if (!boardData.IsInside(x, y))
        {
            failReason = "착수 불가: 보드 범위를 벗어났습니다.";
            return false;
        }

        if (boardData.GetCell(x, y) != StoneType.Empty)
        {
            failReason = $"착수 불가: 이미 돌이 있습니다. ({x}, {y})";
            return false;
        }

        if (currentTurn != requestedStone)
        {
            failReason = "착수 불가: 현재 턴이 아닙니다.";
            return false;
        }

        if (requestedStone == StoneType.Black && IsForbiddenOnCopiedBoard(x, y, StoneType.Black, out string forbiddenReason))
        {
            if (string.IsNullOrEmpty(forbiddenReason))
                failReason = $"착수 불가: 금수 자리입니다. ({x}, {y})";
            else
                failReason = $"착수 불가: {forbiddenReason} 금수입니다. ({x}, {y})";

            return false;
        }

        return true;
    }

    private bool IsForbiddenOnCopiedBoard(int x, int y, StoneType stone, out string reason)
    {
        reason = "";

        StoneType[,] copiedBoard = CopyBoardArray();

        try
        {
            return gomokuRule.IsForbidden(copiedBoard, x, y, stone, out reason);
        }
        catch (Exception e)
        {
            Debug.LogError($"금수 검사 중 오류 발생: ({x}, {y}) / {e.Message}");
            reason = "금수 검사 오류";
            return true;
        }
    }

    private StoneType[,] CopyBoardArray()
    {
        StoneType[,] source = boardData.GetArray();
        StoneType[,] copy = new StoneType[BoardData.Size, BoardData.Size];

        for (int x = 0; x < BoardData.Size; x++)
        {
            for (int y = 0; y < BoardData.Size; y++)
            {
                copy[x, y] = source[x, y];
            }
        }

        return copy;
    }

    // -----------------------------
    // Resign / Game Over
    // -----------------------------
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
        StoneType resignedStone = GetStoneByActor(actorNumber);

        if (resignedStone == StoneType.Empty)
            return;

        StoneType winner = resignedStone == StoneType.Black ? StoneType.White : StoneType.Black;

        string playerName = GetPlayerNameByActor(actorNumber);
        string stoneText = resignedStone == StoneType.Black ? "흑" : "백";

        if (PhotonChatManager.Instance != null)
        {
            PhotonChatManager.Instance.BroadcastSystemMessage($"{playerName} ({stoneText}) 님이 기권했습니다.");
        }

        HandleGameOverAsMaster(winner, false, true);
    }

    private void HandleGameOverAsMaster(
        StoneType winner,
        bool isDraw,
        bool isResign = false,
        string opponentNicknameOverride = "",
        bool isTimeout = false
    )
    {
        string resultMessage;

        if (isDraw)
        {
            resultMessage = "무승부입니다.";
        }
        else if (isTimeout)
        {
            string winnerText = winner == StoneType.Black ? "흑" : "백";
            resultMessage = $"시간 초과. {winnerText}돌이 승리했습니다.";
        }
        else
        {
            string winnerText = winner == StoneType.Black ? "흑" : "백";
            resultMessage = $"{winnerText}돌이 승리했습니다.";
        }

        if (PhotonChatManager.Instance != null)
        {
            PhotonChatManager.Instance.BroadcastSystemMessage(resultMessage);
        }

        string matchId = Guid.NewGuid().ToString("N");

        Debug.Log($"[게임종료] winner={winner}, isDraw={isDraw}, isResign={isResign}, isTimeout={isTimeout}, matchId={matchId}");

        photonView.RPC(
            nameof(RPC_ShowGameResult),
            RpcTarget.All,
            (int)winner,
            isDraw,
            isResign,
            matchId,
            opponentNicknameOverride,
            isTimeout
        );

        Hashtable props = new Hashtable();
        props[PROP_BLACK_READY] = false;
        props[PROP_WHITE_START] = false;
        props[PROP_GAME_STARTED] = false;
        props[PROP_CURRENT_TURN] = (int)StoneType.Black;
        props[PROP_TURN_START_TIME] = PhotonNetwork.Time;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    [PunRPC]
    private void RPC_ShowGameResult(
        int winnerValue,
        bool isDraw,
        bool isResign,
        string matchId,
        string opponentNicknameOverride,
        bool isTimeout
    )
    {
        Debug.Log($"[결과팝업 RPC] 호출됨 winnerValue={winnerValue}, isDraw={isDraw}, isResign={isResign}, isTimeout={isTimeout}, matchId={matchId}");

        StoneType winner = (StoneType)winnerValue;

        ClearForbiddenMarks();

        string title;
        string message;

        if (isDraw)
        {
            title = "무승부";
            message = "보드가 가득 차서 무승부입니다.";
        }
        else if (isTimeout)
        {
            StoneType myStone = GetMyStone();

            if (myStone == winner)
            {
                title = "승리";
                message = "상대의 시간이 초과되어 승리했습니다!";
            }
            else if (myStone == StoneType.Empty)
            {
                string winnerText = winner == StoneType.Black ? "흑" : "백";
                title = "게임 종료";
                message = $"시간 초과로 {winnerText}돌이 승리했습니다.";
            }
            else
            {
                title = "패배";
                message = "시간 초과로 패배했습니다.";
            }
        }
        else
        {
            StoneType myStone = GetMyStone();
            string winnerText = winner == StoneType.Black ? "흑" : "백";

            if (myStone == winner)
            {
                title = "승리";
                message = isResign ? "상대가 기권/퇴장하여 승리했습니다!" : "승리했습니다!";
            }
            else if (myStone == StoneType.Empty)
            {
                title = "게임 종료";
                message = $"{winnerText}돌이 승리했습니다.";
            }
            else
            {
                title = "패배";
                message = isResign ? "기권/퇴장으로 패배했습니다." : "패배했습니다.";
            }
        }

        SaveResultAndReplay(matchId, winner, isDraw, isResign, opponentNicknameOverride);

        PlayResultSound(winner, isDraw);

        ShowResultPopup(title, message);
        SetStatus(message);
    }

    private void PlayResultSound(StoneType winner, bool isDraw)
    {
        if (AudioManager.Instance == null)
            return;

        if (isDraw)
        {
            AudioManager.Instance.PlayPopupSound();
            return;
        }

        StoneType myStone = GetMyStone();

        if (myStone == winner)
        {
            AudioManager.Instance.PlayWinSound();
        }
        else if (myStone == StoneType.Empty)
        {
            AudioManager.Instance.PlayPopupSound();
        }
        else
        {
            AudioManager.Instance.PlayLossSound();
        }
    }

    private void SaveResultAndReplay(
        string matchId,
        StoneType winner,
        bool isDraw,
        bool isResign,
        string opponentNicknameOverride = ""
    )
    {
        if (hasSavedCurrentMatchResult)
            return;

        hasSavedCurrentMatchResult = true;

        StoneType myStone = GetMyStone();

        if (myStone == StoneType.Empty)
            return;

        string myNickname = PhotonNetwork.NickName;

        string opponentNickname = string.IsNullOrEmpty(opponentNicknameOverride)
            ? GetOpponentNickname()
            : opponentNicknameOverride;

        bool isWin = !isDraw && myStone == winner;
        bool isLose = !isDraw && myStone != winner;

        string result;

        if (isDraw)
            result = "Draw";
        else if (isWin)
            result = "Win";
        else
            result = "Lose";

        string myStoneText = myStone == StoneType.Black ? "Black" : "White";

        string winnerStoneText;

        if (isDraw)
            winnerStoneText = "None";
        else
            winnerStoneText = winner == StoneType.Black ? "Black" : "White";

        OmokMoveRecordList moveList = new OmokMoveRecordList();
        moveList.moves = new List<OmokMoveRecord>(moveRecords);

        string movesJson = JsonUtility.ToJson(moveList);

        bool recordUpdated = PlayerDataService.ApplyMatchResult(
            myNickname,
            isWin,
            isLose,
            isDraw
        );

        bool historySaved = PlayerDataService.SaveMatchHistory(
            matchId,
            PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "UnknownRoom",
            myNickname,
            opponentNickname,
            myStoneText,
            result,
            winnerStoneText,
            isDraw,
            isResign,
            movesJson
        );

        Debug.Log($"[결과 저장] 전적 저장={recordUpdated}, 기보 저장={historySaved}, result={result}, moves={moveRecords.Count}");
    }

    private string GetOpponentNickname()
    {
        StoneType myStone = GetMyStone();

        int opponentActor = -1;

        if (myStone == StoneType.Black)
            opponentActor = GetWhiteActor();
        else if (myStone == StoneType.White)
            opponentActor = GetBlackActor();

        if (opponentActor == -1)
            return "알 수 없음";

        return GetPlayerNameByActor(opponentActor);
    }

    private void ShowResultPopup(string title, string message)
    {
        Debug.Log($"[결과팝업 표시] title={title}, message={message}, panel={(resultPopupPanel != null ? resultPopupPanel.name : "NULL")}");

        if (resultPopupPanel != null)
            resultPopupPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = title;

        if (resultMessageText != null)
            resultMessageText.text = message;
    }

    private void HideResultPopup()
    {
        if (resultPopupPanel != null)
            resultPopupPanel.SetActive(false);
    }

    // -----------------------------
    // Reset
    // -----------------------------
    private void ResetRoomAfterPlayerLeft(bool clearBoard)
    {
        int playerCount = PhotonNetwork.PlayerList.Length;

        Hashtable props = new Hashtable();
        props[PROP_BLACK_READY] = false;
        props[PROP_WHITE_START] = false;
        props[PROP_GAME_STARTED] = false;
        props[PROP_CURRENT_TURN] = (int)StoneType.Black;
        props[PROP_TURN_START_TIME] = PhotonNetwork.Time;

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

        if (clearBoard)
        {
            photonView.RPC(nameof(RPC_ClearBoardVisuals), RpcTarget.All);
        }
    }

    private void ResetMatchStateOnMaster(bool clearBoard)
    {
        Hashtable props = new Hashtable();
        props[PROP_BLACK_READY] = false;
        props[PROP_WHITE_START] = false;
        props[PROP_GAME_STARTED] = false;
        props[PROP_CURRENT_TURN] = (int)StoneType.Black;
        props[PROP_TURN_START_TIME] = PhotonNetwork.Time;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        if (clearBoard)
        {
            photonView.RPC(nameof(RPC_ClearBoardVisuals), RpcTarget.All);
        }
    }

    // -----------------------------
    // RPC Visuals
    // -----------------------------
    [PunRPC]
    private void RPC_PlaceStone(int x, int y, int stoneValue)
    {
        StoneType stone = (StoneType)stoneValue;

        boardData.SetCell(x, y, stone);

        moveRecords.Add(new OmokMoveRecord
        {
            turnIndex = moveRecords.Count + 1,
            x = x,
            y = y,
            stone = stoneValue
        });

        SpawnStoneVisual(x, y, stone);

        PlayStoneSound();

        UpdateUI();
        RefreshForbiddenMarks();
    }

    private void PlayStoneSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStoneSound();
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }
    }

    [PunRPC]
    private void RPC_ClearBoardVisuals()
    {
        boardData.ClearBoard();
        moveRecords.Clear();
        hasSavedCurrentMatchResult = false;
        isLeavingAfterLoss = false;
        hasHandledTurnTimeout = false;

        ClearForbiddenMarks();
        HideResultPopup();

        if (boardRoot != null)
        {
            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(boardRoot.GetChild(i).gameObject);
            }
        }

        UpdateUI();
        RefreshForbiddenMarks();
    }

    private void SpawnStoneVisual(int x, int y, StoneType stone)
    {
        GameObject prefab = null;

        if (stone == StoneType.Black)
            prefab = blackStonePrefab;
        else if (stone == StoneType.White)
            prefab = whiteStonePrefab;

        if (prefab == null)
            return;

        Vector2 pos = boardOrigin + new Vector2(x * cellSize, y * cellSize);

        Instantiate(prefab, pos, Quaternion.identity, boardRoot);
    }

    // -----------------------------
    // Forbidden Mark Visuals
    // -----------------------------
    private void RefreshForbiddenMarks()
    {
        ClearForbiddenMarks();

        if (!IsGameStarted())
            return;

        if (GetCurrentTurn() != StoneType.Black)
            return;

        if (showForbiddenOnlyToBlackPlayer && GetMyStone() != StoneType.Black)
            return;

        for (int x = 0; x < BoardData.Size; x++)
        {
            for (int y = 0; y < BoardData.Size; y++)
            {
                if (boardData.GetCell(x, y) != StoneType.Empty)
                    continue;

                if (IsForbiddenOnCopiedBoard(x, y, StoneType.Black, out _))
                {
                    SpawnForbiddenMark(x, y);
                }
            }
        }
    }

    private void SpawnForbiddenMark(int x, int y)
    {
        if (forbiddenMarkPrefab == null)
            return;

        Transform parent = forbiddenMarkRoot != null ? forbiddenMarkRoot : boardRoot;

        if (parent == null)
            return;

        Vector2 pos = boardOrigin + new Vector2(x * cellSize, y * cellSize);

        GameObject mark = Instantiate(forbiddenMarkPrefab, pos, Quaternion.identity, parent);
        forbiddenMarks.Add(mark);
    }

    private void ClearForbiddenMarks()
    {
        for (int i = forbiddenMarks.Count - 1; i >= 0; i--)
        {
            if (forbiddenMarks[i] != null)
                Destroy(forbiddenMarks[i]);
        }

        forbiddenMarks.Clear();
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

        if (myStoneText != null)
        {
            if (myStone == StoneType.Black)
                myStoneText.text = "내 돌: 흑";
            else if (myStone == StoneType.White)
                myStoneText.text = "내 돌: 백";
            else
                myStoneText.text = "내 돌: 없음";
        }

        if (!IsGameStarted())
        {
            StoneType turn = GetCurrentTurn();

            if (turnText != null)
            {
                turnText.text = $"현재 차례 : {(turn == StoneType.Black ? "흑" : "백")}";
            }

            if (GetBlackReady() && !GetWhiteStart())
                SetStatus("흑 준비 완료. 백이 시작 버튼을 눌러야 합니다.");
            else if (!GetBlackReady())
                SetStatus("흑이 준비 버튼을 눌러야 합니다.");
            else
                SetStatus("게임 시작 대기 중");
        }
        else
        {
            StoneType turn = GetCurrentTurn();
            int remaining = GetRemainingTurnSeconds();

            if (turnText != null)
            {
                turnText.text = $"현재 차례 : {(turn == StoneType.Black ? "흑" : "백")} {remaining}초";
            }

            SetStatus($"게임 진행 중");
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}