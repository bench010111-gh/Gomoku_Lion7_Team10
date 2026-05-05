using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DuoGameManager : MonoBehaviour
{
    [Header("Board Setting")]
    public Vector2 boardOrigin = Vector2.zero;
    public Vector2 cellSize = new Vector2(1f, 1f);
    public Transform boardRoot;

    [Header("Board Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;
    public GameObject forbiddenMarkerPrefab;
    public GameObject lastMoveMarkerPrefab;

    [Header("Board UI")]
    public TMP_Text turnText;
    public TMP_Text statusText;
    public TMP_Text timerText;

    [Header("Timer Setting")]
    public float timeLimit = 30f;
    private float currentTimer;
    private bool isTimerRunning;
    private bool isCountingDown = false;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;

    [Header("Score UI")]
    public TMP_Text p1BlackScoreText;
    public TMP_Text p1WhiteScoreText;
    public TMP_Text p2BlackScoreText;
    public TMP_Text p2WhiteScoreText;

    private BoardData boardData = new BoardData();
    private GomokuRule rule;
    private StoneType currentTurn = StoneType.Black;
    private bool isGameOver = false;

    private Coroutine statusCoroutine;
    private List<GameObject> forbiddenMarkers = new List<GameObject>();
    private GameObject currentLastMoveMarker;

    private int p1BlackScore = 0;
    private int p1WhiteScore = 0;
    private int p2BlackScore = 0;
    private int p2WhiteScore = 0;
    private StoneType winColor = StoneType.Empty;

    private void Start()
    {
        rule = new GomokuRule(BoardData.Size);
        UpdateScoreUI();
        RestartGame();
    }

    private void Update()
    {
        if (isGameOver || !isTimerRunning || isCountingDown) 
            return;

        currentTimer -= Time.deltaTime;
        UpdateTimerUI();

        if (currentTimer <= 0)
        {
            currentTimer = 0;
            UpdateTimerUI();
            OnTimeOut();
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryClickBoard();
        }
    }

    private void TryClickBoard()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 localPos = (Vector2)mousePos - boardOrigin;

        int x = Mathf.RoundToInt(localPos.x / cellSize.x);
        int y = Mathf.RoundToInt(localPos.y / cellSize.y);

        if (boardData.IsInside(x, y) && boardData.GetCell(x, y) == StoneType.Empty)
        {
            PlaceStone(x, y);
        }
    }

    private void PlaceStone(int x, int y)
    {
        if (currentTurn == StoneType.Black)
        {
            if (rule.IsForbidden(boardData.GetArray(), x, y, currentTurn, out string reason))
            {
                SetStatus($"금수 자리입니다! ({reason})", 1.5f);
                return;
            }
        }

        boardData.SetCell(x, y, currentTurn);
        SpawnStoneVisual(x, y, currentTurn);

        if (rule.CheckWin(boardData.GetArray(), x, y, currentTurn))
        {
            EndGame($"{(currentTurn == StoneType.Black ? "흑돌" : "백돌")} 승리!", currentTurn);
            return;
        }

        if (rule.IsDraw(boardData.GetPlacedStoneCount()))
        {
            EndGame("무승부!", StoneType.Empty);
            return;
        }

        SwitchTurn();
    }

    private void SwitchTurn()
    {
        currentTurn = (currentTurn == StoneType.Black) ? StoneType.White : StoneType.Black;
        UpdateUI();
        ResetTimer();
        UpdateForbiddenMarkers();
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

    private void SpawnStoneVisual(int x, int y, StoneType color)
    {
        GameObject prefab = (color == StoneType.Black) ? blackStonePrefab : whiteStonePrefab;
        Vector2 pos = boardOrigin + new Vector2(x * cellSize.x, y * cellSize.y);
        Instantiate(prefab, pos, Quaternion.identity, boardRoot);

        if (lastMoveMarkerPrefab != null)
        {
            if (currentLastMoveMarker == null)
            {
                currentLastMoveMarker = Instantiate(lastMoveMarkerPrefab, pos, Quaternion.identity, boardRoot);
            }
            else
            {
                currentLastMoveMarker.transform.position = pos;

                currentLastMoveMarker.transform.SetAsLastSibling();
            }
        }
    }

    private void EndGame(string message, StoneType winner)
    {
        isGameOver = true;
        isTimerRunning = false;
        winColor = winner;

        UpdateForbiddenMarkers();

        if (gameOverText != null)
        {
            gameOverText.text = message;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }
    
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

        currentLastMoveMarker = null;

        UpdateUI();
        ResetTimer();
        UpdateForbiddenMarkers();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        StartCoroutine(StartCountDownRoutine());
    }

    private IEnumerator StartCountDownRoutine()
    {
        isCountingDown = true;

        UpdateUI();
        if (timerText != null)
            timerText.text = "준비...";

        SetStatus("3");
        yield return new WaitForSeconds(1f);

        SetStatus("2");
        yield return new WaitForSeconds(1f);

        SetStatus("1");
        yield return new WaitForSeconds(1f);

        SetStatus("게임 시작!", 1.5f);

        isCountingDown = false;
        ResetTimer();

        UpdateForbiddenMarkers();
    }
    
    private void UpdateUI()
    {
        if (turnText != null)
        {
            turnText.text = $"{(currentTurn == StoneType.Black ? "흑돌 차례" : "백돌 차례")}";
            turnText.color = (currentTurn == StoneType.Black) ? Color.black : Color.white;
        }
            
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
        {
            statusText.text = "";
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
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null && !isCountingDown)
        {
            timerText.text = $"{Mathf.CeilToInt(currentTimer)}초";
        }
    }

    public void SelectWinner(int playerNum)
    {
        if (winColor != StoneType.Empty)
        {
            if (winColor == StoneType.Black)
            {
                if (playerNum == 1)
                    p1BlackScore++;
                if (playerNum == 2)
                    p2BlackScore++;
            }
            else if (winColor == StoneType.White)
            {
                if (playerNum == 1)
                    p1WhiteScore++;
                if(playerNum == 2)
                    p2WhiteScore++;
            }

            UpdateScoreUI();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        SetStatus("다시 시작을 눌러주세요");
    }

    private void UpdateScoreUI()
    {
        if (p1BlackScoreText != null) p1BlackScoreText.text = $"흑 : {p1BlackScore}";
        if (p1WhiteScoreText != null) p1WhiteScoreText.text = $"백 : {p1WhiteScore}";
        if (p2BlackScoreText != null) p2BlackScoreText.text = $"흑 : {p2BlackScore}";
        if (p2WhiteScoreText != null) p2WhiteScoreText.text = $"백 : {p2WhiteScore}";
    }
}

