using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AIGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public AIGameSettingSO settingSO;
    private int depth;
    private int timeLimitMs;

    [Header("Player")]
    public StoneType playerStone;
    public StoneType aiStone;

    [Header("AI")]
    public AI ai;
    private int[,] intBoard;
    private bool isThinking = false;
    public float delay = 0.5f;

    [Header("Board Settings")]
    //public Vector2 boardOrigin = new Vector2(-4, -3.227f);
    //public Vector2 boardOrigin = new Vector2(-3.9831f, -3.2897f);
    public Vector2 boardOrigin;

    // public Vector2 cellSize = new Vector2(0.57f, 0.5f);
    //public Vector2 cellSize = new Vector2(0.5698f, 0.5011f);
    public Vector2 cellSize; 
    public Transform boardRoot;

    [Header("Board Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;
    public GameObject forbiddenMarkerPrefab;

    [Header("BoardUI")]
    public TMP_Text turnText;
    public TMP_Text statusText;
    public TMP_Text timerText;

    [Header("Timer Settings")]
    public float timeLimit = 30f;
    private float currentTimer;
    private bool isTimerRunning;
    private bool isCountingDown = false;

    [Header("GameOver UI")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;
    public TMP_Text resultText;

    private BoardData boardData;
    private GomokuRule rule;
    private StoneType currentTurn = StoneType.Black;
    private bool isGameOver = false;

    private Coroutine statusCoroutine;
    private List<GameObject> forbiddenMarkers = new List<GameObject>();

    private WaitForSeconds wait;

    [SerializeField] Transform aiHand;
    [SerializeField] SpriteRenderer aiHandSr;

    [Header("Sprites")]
    [SerializeField] Sprite blackStoneHand;
    [SerializeField] Sprite whiteStoneHand;
    [SerializeField] Sprite blackStoneBowl;
    [SerializeField] Sprite whiteStoneBowl; 


    [SerializeField] GameObject lastPlacedStoneMarkerPrefab;
    private GameObject lastPlacedStoneMarkerObject;

    [SerializeField] PlayerMouse playerMouse;
    [SerializeField] Image aiBowlImg; 
    [SerializeField] Image playerBowlImg; 
    void Start()
    {
        Init();
        RestartGame();
    }

    void Update()
    {
        if (isGameOver || isCountingDown) return;

        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime;
            UpdateTimerUI();
            if (currentTimer <= 0f) { isTimerRunning = false; OnTimeOut(); }
        }

        if (currentTurn == playerStone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // UI 위에서 클릭했을 때에는 Click이 되지 않도록. 
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                if (playerMouse.IsHoldingStone)
                {
                    bool success = TryClickBoard();
                    playerMouse.DropStone(); 
                }
            }
        }
        else
        {
            if (!isThinking)
                RunAI();
        }
    }

    #region Initialization 
    private void Init()
    {
        boardData = new BoardData();
        rule = new GomokuRule(BoardData.Size);

        intBoard = new int[BoardData.Size, BoardData.Size];
        wait = new WaitForSeconds(delay);

        SetOrder();
        SetAIParams(settingSO.GetDifficulty());
        SetHandSprite();
        SetBowlImage(); 
        SpawnLastPlacedStoneMarker();
        playerMouse.SetStoneType(playerStone);
    }

    private void SetAIParams(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.EASY:
                depth = 1;
                timeLimitMs = 500;
                break;
            case Difficulty.NORMAL:
                depth = 3;
                timeLimitMs = 2000;
                break;
            case Difficulty.HARD:
                depth = 10;
                timeLimitMs = 3000;
                break;
        }
    }
    private void SetOrder()
    {
        aiStone = settingSO.IsFirstMove() ? StoneType.Black : StoneType.White;
        playerStone = aiStone == StoneType.Black ? StoneType.White : StoneType.Black;
    }
    private void SetHandSprite()
    {
        aiHandSr.sprite = aiStone == StoneType.Black ? blackStoneHand : whiteStoneHand;
    }
    private void SetBowlImage()
    {
        aiBowlImg.sprite = aiStone == StoneType.Black ? blackStoneBowl : whiteStoneBowl; 
        playerBowlImg.sprite = playerStone == StoneType.Black ? blackStoneBowl : whiteStoneBowl;
    }
    #endregion 

    #region Input
    private bool TryClickBoard()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 localPos = (Vector2)mousePos - boardOrigin;

        int x = Mathf.RoundToInt(localPos.x / cellSize.x);
        int y = Mathf.RoundToInt(localPos.y / cellSize.y);

        if (boardData.IsInside(x, y) && boardData.GetCell(x, y) == StoneType.Empty)
            return PlaceStone(x, y, playerStone);
        
        else
        {
            Debug.Log("이미 돌이 있는 자리입니다.");
            return false; 
        }
    }
    public void OnClickStoneBowl()
    {
        if (isGameOver) return;

        if (!playerMouse.IsHoldingStone)
            playerMouse.PickupStone();
    }
    #endregion

    #region Logic 
    private bool PlaceStone(int x, int y, StoneType stone)
    {
        if (currentTurn != stone)
        {
            Debug.Log("현재 차례에 둘 수 없습니다.");
            return false;
        }

        if (currentTurn == StoneType.Black)
        {
            if (rule.IsForbidden(boardData.GetArray(), x, y, currentTurn, out string reason))
            {
                SetStatus($"금수 자리입니다! ({reason})", 1.5f);
                return false;
            }
        }

        boardData.SetCell(x, y, currentTurn);
        SpawnStoneVisual(x, y, currentTurn);
        UpdateLastPlacedStoneMarker(x, y);

        if (rule.CheckWin(boardData.GetArray(), x, y, currentTurn))
        {
            // 이펙트 추가 
            EndGame($"{(currentTurn == StoneType.Black ? "흑돌" : "백돌")} 승리!");
            return true;
        }

        if (rule.IsDraw(boardData.GetPlacedStoneCount()))
        {
            EndGame("무승부!");
            return true;
        }

        SwitchTurn();
        return true; 
    }
    private void SwitchTurn()
    {
        currentTurn = (currentTurn == StoneType.Black) ? StoneType.White : StoneType.Black;
        UpdateTurnTextUI();
        //SetStatus(""); // 턴이 바뀌면 이전 상태 메시지 지우기
        ResetTimer();

        UpdateForbiddenMarkers();
    }
    #endregion 

    #region GameFlow 
    public void RestartGame()
    {
        boardData.ClearBoard();

        isGameOver = false;
        currentTurn = StoneType.Black;

        if (boardRoot != null)
        {
            foreach (Transform child in boardRoot)
            {
                Destroy(child.gameObject);
            }
        }

        UpdateTurnTextUI();
        ResetTimer();

        UpdateForbiddenMarkers();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        statusText.gameObject.SetActive(true);
        turnText.gameObject.SetActive(false);

        StartCoroutine(StartCountDownRoutine());
    }
    private void EndGame(string message)
    {
        isGameOver = true;
        isTimerRunning = false;

        UpdateForbiddenMarkers();

        if (gameOverText != null)
            gameOverText.text = message;

        if (resultText != null)
            resultText.text = currentTurn == aiStone ? "패배" : "승리";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
    #endregion

    #region UI 
    private IEnumerator StartCountDownRoutine()
    {
        isCountingDown = true;

        UpdateTurnTextUI();
        if (timerText != null)
            timerText.text = "준비";

        SetStatus("3");
        yield return new WaitForSeconds(1f);

        SetStatus("2");
        yield return new WaitForSeconds(1f);

        SetStatus("1");
        yield return new WaitForSeconds(1f);

        SetStatus("게임 시작!");
        yield return new WaitForSeconds(1.5f);

        statusText.gameObject.SetActive(false); 
        turnText.gameObject.SetActive(true);

        isCountingDown = false;
        ResetTimer();

        UpdateForbiddenMarkers();
    }
    private void SetStatus(string msg, float duration = 0f)
    {
        if (statusText != null)
        {
            statusText.text = msg;

            if (statusCoroutine != null)
                StopCoroutine(statusCoroutine);

            if (duration > 0f)
                statusCoroutine = StartCoroutine(ClearStatusRoutine(duration));
        }
    }
    private IEnumerator ClearStatusRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusText != null)
            statusText.text = "";
    }
    private void UpdateTurnTextUI()
    {
        if (turnText != null)
        {
            turnText.text = $"{(currentTurn == StoneType.Black ? "흑" : "백")}";
            turnText.color = (currentTurn == StoneType.Black) ? Color.black : Color.white;
        }

    }
    private void UpdateTimerUI()
    {
        if (timerText != null && !isCountingDown)
        {
            timerText.text = $"00:{Mathf.CeilToInt(currentTimer)}";

            if (currentTimer <= 10)
                timerText.color = Color.red; 

        }
    }
    private void UpdateForbiddenMarkers()
    {
        foreach (var marker in forbiddenMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }

        forbiddenMarkers.Clear();

        if (currentTurn != StoneType.Black || isGameOver) return;

        for (int x = 0; x < BoardData.Size; x++)
        {
            for (int y = 0; y < BoardData.Size; y++)
            {
                if (boardData.GetCell(x, y) == StoneType.Empty)
                {
                    if (rule.IsForbidden(boardData.GetArray(), x, y, StoneType.Black, out string reason))
                    {
                        Vector2 pos = boardOrigin + new Vector2(x * cellSize.x, y * cellSize.y);
                        GameObject marker = Instantiate(forbiddenMarkerPrefab, pos, Quaternion.identity, boardRoot);

                        TMP_Text textComp = marker.GetComponentInChildren<TMP_Text>();
                        if (textComp != null)
                        {
                            textComp.text = reason;
                        }
                        forbiddenMarkers.Add(marker);
                    }
                }
            }
        }
    }
    private void OnTimeOut()
    {
        Debug.Log("시간초과");
        SetStatus($"{(currentTurn == StoneType.Black ? "흑" : "백")}! 초읽기 시간 초과! 턴 변경", 1.5f);
        SwitchTurn();
    }
    private void ResetTimer()
    {
        currentTimer = timeLimit;
        isTimerRunning = true;
        timerText.color = Color.black; 
        UpdateTimerUI();
    }
    private void SpawnStoneVisual(int x, int y, StoneType color)
    {
        GameObject prefab = (color == StoneType.Black) ? blackStonePrefab : whiteStonePrefab;
        Vector2 pos = boardOrigin + new Vector2(x * cellSize.x, y * cellSize.y);
        Instantiate(prefab, pos, Quaternion.identity, boardRoot);
    }
    private void SpawnLastPlacedStoneMarker()
    {
        lastPlacedStoneMarkerObject = Instantiate(lastPlacedStoneMarkerPrefab, Vector2.zero, Quaternion.identity);
        lastPlacedStoneMarkerObject.SetActive(false);
    }
    private void UpdateLastPlacedStoneMarker(int x, int y)
    {
        lastPlacedStoneMarkerObject?.SetActive(false);

        if (isGameOver)
            return;

        Vector2 pos = boardOrigin + new Vector2(x * cellSize.x, y * cellSize.y);
        lastPlacedStoneMarkerObject.transform.position = pos;
        lastPlacedStoneMarkerObject.SetActive(true);
    }
    #endregion 

    #region AI 
    private void RunAI()
    {
        isThinking = true;
        StartCoroutine(DoAICoroutine());
    }
    private IEnumerator DoAICoroutine()
    {
        intBoard = ToIntBoard(boardData.GetArray());

        Vector2Int result = new Vector2Int(-1, -1);
        bool done = false;

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            result = ai.GetBestMove(intBoard, (int)aiStone, timeLimitMs, depth);
            done = true;
        });

        yield return new WaitUntil(() => done);

        yield return wait;

        yield return StartCoroutine(MoveHand(result.x, result.y));

        isThinking = false;
    }
    private IEnumerator MoveHand(int x, int y)
    {
        float moveDuration = 0.8f;
        Ease moveEase = Ease.OutQuad; // 서서히 멈추는 효과

        Vector2 startPos = Vector2.zero;
        Vector2 targetPos = boardOrigin + new Vector2(x * cellSize.x, y * cellSize.y);


        aiHand.position = startPos;
        aiHand.localScale = Vector3.one * 1.5f;
        aiHand.gameObject.SetActive(true);

        Sequence aiSequence = DOTween.Sequence();

        aiSequence.Append(
            aiHand.DOMove(targetPos, moveDuration).SetEase(moveEase)
        ).Join(
            aiHand.DOScale(1.0f, moveDuration).SetEase(moveEase)
        ).AppendCallback(() => {
            // 이동이 끝나면 돌 놓기
            PlaceStone(x, y, aiStone);
            aiHand.DOPunchPosition(Vector3.down * 0.1f, 0.2f);
        });

        yield return aiSequence.WaitForCompletion();

        yield return new WaitForSeconds(0.3f);

        aiHand.gameObject.SetActive(false);
    }
    #endregion

    #region Helper 
    //StoneType 배열을 int 배열로 변환
    private int[,] ToIntBoard(StoneType[,] stoneBoard)
    {
        for (int x = 0; x < BoardData.Size; x++)
            for (int y = 0; y < BoardData.Size; y++)
                intBoard[x, y] = (int)stoneBoard[x, y];

        return intBoard;
    }
    public void LoadMainScene()
    {
        SceneManager.LoadScene("03_MainLobbyScene");
    }
    #endregion 
}
