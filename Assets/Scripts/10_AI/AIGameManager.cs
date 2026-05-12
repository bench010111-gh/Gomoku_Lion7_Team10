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
    #region Inspector 
    [Header("Game Settings")]
    public AIGameSettingSO settingSO;

    [Header("Player")]
    public StoneType playerStone;
    public StoneType aiStone;

    [Header("AI")]
    public AI ai;
    public float delay = 0.5f;

    [Header("AI Difficulty")]
    [SerializeField] private int depth;
    [SerializeField] private int timeLimitMs;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueData easyIntroDialogue;
    [SerializeField] private DialogueData normalIntroDialogue;
    [SerializeField] private DialogueData hardIntroDialogue;

    [SerializeField] private DialogueData easyWinDialogue;
    [SerializeField] private DialogueData normalWinDialogue;
    [SerializeField] private DialogueData hardWinDialogue;

    [Header("Board")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Vector2 boardOrigin = new Vector2(-3.9948f, -3.2802f);
    [SerializeField] private Vector2 cellSize = new Vector2(0.57f, 0.4993f);

    [Header("Objects Enabled After Dialogue")]
    [SerializeField] private List<GameObject> gameplayObjects;

    [Header("Stone Prefabs")]
    [SerializeField] private GameObject blackStonePrefab;
    [SerializeField] private GameObject whiteStonePrefab;

    [Header("Board Effects")]
    [SerializeField] private GameObject forbiddenMarkerPrefab;
    [SerializeField] private GameObject lastPlacedStoneMarkerPrefab;

    [Header("Player Input")]
    [SerializeField] private PlayerMouse playerMouse; 

    [Header("UI Turn")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text timerText;

    [Header("UI GameOver")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private TMP_Text resultText;

    [Header("UI Bowl")]
    [SerializeField] private Image aiBowlImg; 
    [SerializeField] private Image playerBowlImg;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;

    [Header("AI Hand")]
    [SerializeField] private Transform aiHand;
    [SerializeField] private SpriteRenderer aiHandSr;

    [Header("Hand Sprites")]
    [SerializeField] private Sprite blackStoneHand;
    [SerializeField] private Sprite whiteStoneHand;

    [Header("Bowl Sprites")]
    [SerializeField] private Sprite blackStoneBowl;
    [SerializeField] private Sprite whiteStoneBowl;

    [Header("Timer")]
    [SerializeField] private float timeLimit = 30f;

    #endregion

    #region Runtime 
    private BoardData boardData;
    private GomokuRule rule;

    private StoneType currentTurn;
    
    private bool isGameOver = false;
    private bool isThinking;
    private bool isTimerRunning; 
    private bool isCountingDown;

    private float currentTimer;

    // StoneType 타입 보드를 AI에 적용하기 위한 배열 
    private int[,] intBoard;

    private Coroutine statusCoroutine;
    private WaitForSeconds wait;

    private GameObject lastPlacedStoneMarkerObject;
    private readonly List<GameObject> forbiddenMarkers = new List<GameObject>();
    #endregion

    private IEnumerator StartDialogueRoutine()
    {
        switch (settingSO.GetDifficulty())
        {
            case Difficulty.EASY:
                dialogueManager.SetDialogue(easyIntroDialogue);
                break;

            case Difficulty.NORMAL:
                dialogueManager.SetDialogue(normalIntroDialogue);
                break;

            case Difficulty.HARD:
                dialogueManager.SetDialogue(hardIntroDialogue);
                break;
        }

        dialogueManager.StartDialogue();

        yield return new WaitUntil(() => dialogueManager.IsFinished);

        SetGameplayObjects(true);

        yield return StartCoroutine(StartCountDownRoutine());
    }

    private IEnumerator WinDialogueRoutine()
    {
        switch (settingSO.GetDifficulty())
        {
            case Difficulty.EASY:
                dialogueManager.SetDialogue(easyWinDialogue);
                break;

            case Difficulty.NORMAL:
                dialogueManager.SetDialogue(normalWinDialogue);
                break;

            case Difficulty.HARD:
                dialogueManager.SetDialogue(hardWinDialogue);
                break;
        }

        dialogueManager.StartDialogue();

        yield return new WaitUntil(() => dialogueManager.IsFinished);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    void Start()
    {
        Init();
        RestartGame();
    }

    void Update()
    {
        if (dialogueManager != null && dialogueManager.isDialogueActive)
        {
            return;
        }

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

        currentTurn = StoneType.Black; 

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
                timeLimitMs = 100;
                break;
            case Difficulty.NORMAL:
                depth = 3;
                timeLimitMs = 5000;
                break;
            case Difficulty.HARD:
                depth = 10;
                timeLimitMs = 3000;
                break;
        }
    }
    private void SetOrder()
    {
        playerStone = settingSO.IsFirstMove() ? StoneType.Black : StoneType.White;
        aiStone = playerStone == StoneType.Black ? StoneType.White : StoneType.Black;
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

    private void SetGameplayObjects(bool active)
    {
        foreach (GameObject obj in gameplayObjects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
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

        AudioManager.Instance.PlayStoneSound();
        boardData.SetCell(x, y, currentTurn);
        SpawnStoneVisual(x, y, currentTurn);
        UpdateLastPlacedStoneMarker(x, y);

        if (rule.CheckWin(boardData.GetArray(), x, y, currentTurn))
        {
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

        ClearLastPlacedStoneMarker();

        UpdateTurnTextUI();
        ResetTimer();

        UpdateForbiddenMarkers();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        statusText.gameObject.SetActive(true);
        turnText.gameObject.SetActive(false);

        SetGameplayObjects(false);

        StartCoroutine(StartDialogueRoutine());
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

        if (currentTurn == playerStone)
            AudioManager.Instance.PlayWinSound();
        else
            AudioManager.Instance.PlayLossSound();

        //if (gameOverPanel != null)
        //    gameOverPanel.SetActive(true);

        if (currentTurn == playerStone)
        {
            SetGameplayObjects(false);

            StartCoroutine(WinDialogueRoutine());
        }
        else
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
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
            timerText.text = $"00:{Mathf.CeilToInt(currentTimer):00}";

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
    private void ClearLastPlacedStoneMarker()
    {
        lastPlacedStoneMarkerObject.SetActive(false);
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
        playerMouse.OnActiveCursor(); 
        SceneManager.LoadScene("03_MainLobbyScene");
    }
    #endregion 
}
