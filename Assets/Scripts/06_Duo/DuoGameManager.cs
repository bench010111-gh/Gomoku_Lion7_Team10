using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DuoGameManager : MonoBehaviour
{
    //무르기 기능을 위해 둔 위치와 돌 오브젝트를 기억하는 클래스
    private class MoveRecord
    {
        public int x;
        public int y;
        public GameObject stoneObj;
    }

    [Header("Board Setting")]
    public Vector2 boardOrigin = Vector2.zero;      //보드의 좌측 하단 시작 좌표
    public Vector2 cellSize = new Vector2(1f, 1f);  //그리드 한 칸의 크기
    public Transform boardRoot;                     //생성된 돌들이 담길 부모 오브젝트

    [Header("Board Prefabs")]
    public GameObject blackStonePrefab;             //흑돌 프리팹
    public GameObject whiteStonePrefab;             //백돌 프리팹
    public GameObject forbiddenMarkerPrefab;        //금수 표시 마커 프리팹
    public GameObject lastMoveMarkerPrefab;         //마지막 착수 마커 프리팹

    [Header("Board UI")]
    public TMP_Text turnText;                       //현재 턴 텍스트
    public TMP_Text statusText;                     //왼쪽 패널 현재 상태 메세지 표시

    [Header("Game Over UI")]
    public GameObject gameOverPanel;                //게임 종료 시 승자 선택 팝업창 표시
    public TMP_Text gameOverText;                   //팝업창 내부 승리 문구

    [Header("Button UI")]
    public TMP_Text startButtonText;                //게임 시작 버튼 텍스트

    [Header("Score UI")]
    public TMP_Text p1BlackScoreText;               //p1의 흑돌승리 점수
    public TMP_Text p1WhiteScoreText;               //p1의 백돌승리 점수
    public TMP_Text p2BlackScoreText;               //p2의 흑돌승리 점수
    public TMP_Text p2WhiteScoreText;               //p2의 백돌승리 점수

    [Header("Hand Cursor")]
    public Transform handCursorTransform;           //마우스를 따라다닐 손 오브젝트
    public SpriteRenderer handSpriteRenderer;       //손 이미지 렌더러
    public Sprite blackHandSprite;                  //흑돌 든 손 이미지
    public Sprite whiteHandSprite;                  //백돌 든 손 이미지
    public Vector3 handOffset = Vector3.zero;       //마우스 끝부분과 돌 위치를 맞추기 위한 미세 조종값

    [Header("System Cursor (UI Mode)")]
    public Sprite defaultPointerSprite;             //평소 손 이미지 스프라이트
    public Sprite clickPointerSprite;               //클릭 시 손 이미지 스프라이트

    private BoardData boardData = new BoardData();      //바둑판 배열 데이터 관리
    private GomokuRule rule;                            //오목 룰(금수, 승패 판별) 관리자
    private StoneType currentTurn = StoneType.Black;    //현재 턴 저장
    private bool isGameOver = false;                    //게임 종료 상태 여부

    private Coroutine statusCoroutine;                                      //상태 메세지 타이머 코루틴
    private List<GameObject> forbiddenMarkers = new List<GameObject>();     //금수 마커들
    private GameObject currentLastMoveMarker;                               //마지막 착수 마커
    private Stack<MoveRecord> moveHistory = new Stack<MoveRecord>();        //착수 기록을 넣어둔 스택

    private int p1BlackScore = 0;
    private int p1WhiteScore = 0;
    private int p2BlackScore = 0;
    private int p2WhiteScore = 0;
    private StoneType winColor = StoneType.Empty;       //이번 판에 이긴 돌의 색깔

    private void Start()
    {
        rule = new GomokuRule(BoardData.Size);
        UpdateScoreUI();
        
        isGameOver = true;                      //게임시작 버튼을 누르기 전에 클릭 방지

        if (startButtonText != null)
        {
            startButtonText.text = "게임 시작";
        }

        SetStatus("[게임 시작] 버튼을 눌러주세요.");
    }

    private void Update()
    {
        UpdateCursorVisibility();       //손 커서 업데이트

        //대기 중이거나 끝난 상태면 마우스 화살표를 켜고 클릭 처리를 넘김
        if (isGameOver)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            //마우스 UI 위에 있지 않을 때만 돌을 두기
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                TryClickBoard();
            }
        }
    }

    private void TryClickBoard()
    {
        //마우스의 화면 좌표를 게임 속 2D 월드 좌표로 변환
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 localPos = (Vector2)mousePos - boardOrigin;

        //반올림을 통해 가장 가까운 교차점(그리드) 인덱스를 찾음
        int x = Mathf.RoundToInt(localPos.x / cellSize.x);
        int y = Mathf.RoundToInt(localPos.y / cellSize.y);

        //보드 범위 안이고 빈칸이라면 착수
        if (boardData.IsInside(x, y) && boardData.GetCell(x, y) == StoneType.Empty)
        {
            if (handCursorTransform != null)
            {
                StartCoroutine(HandClickAnimationRoutine()); //애니메이션 실행
            }

            PlaceStone(x, y);
        }
    }

    private void PlaceStone(int x, int y)
    {
        //흑돌 차례일 때 금수(33, 44, 장목) 검사
        if (currentTurn == StoneType.Black)
        {
            if (rule.IsForbidden(boardData.GetArray(), x, y, currentTurn, out string reason))
            {
                SetStatus($"금수 자리입니다! ({reason})", 1.5f);
                return;
            }
        }

        //보드 데이터 갱신 및 돌 생성
        boardData.SetCell(x, y, currentTurn);
        GameObject placeStone = SpawnStoneVisual(x, y, currentTurn);

        //무르기를 위해 장부에 기록 추가
        moveHistory.Push(new MoveRecord { x = x, y = y, stoneObj = placeStone });

        //승리 조건 검사
        if (rule.CheckWin(boardData.GetArray(), x, y, currentTurn))
        {
            EndGame($"{(currentTurn == StoneType.Black ? "흑돌" : "백돌")} 승리!", currentTurn);
            return;
        }


        //무승부 검사(판이 꽉 참)
        if (rule.IsDraw(boardData.GetPlacedStoneCount()))
        {
            EndGame("무승부!", StoneType.Empty);
            return;
        }

        SwitchTurn();   //턴 넘기기
    }

    public void UndoMove()
    {
        //뺄 데이터(현재가 첫 수일)가 없거나 게임 종료 상태면 무시
        if (isGameOver || moveHistory.Count == 0)
        {
            SetStatus("지금은 무를 수 없습니다.", 1.5f);
            return;
        }

        //리스트에서 마지막 수를 꺼냄
        MoveRecord lastMove = moveHistory.Pop();

        //데이터를 비우고 오브젝트 파괴
        boardData.SetCell(lastMove.x, lastMove.y, StoneType.Empty);

        if (lastMove.stoneObj != null)
        {
            Destroy(lastMove.stoneObj);
        }

        //턴 되돌리기
        currentTurn = (currentTurn == StoneType.Black) ? StoneType.White : StoneType.Black;

        //마지막 착수 마커 이전 위치로 롤백
        if (moveHistory.Count > 0)
        {
            MoveRecord prevMove = moveHistory.Peek();
            Vector2 pos = boardOrigin + new Vector2(prevMove.x * cellSize.x, prevMove.y * cellSize.y);
            if (currentLastMoveMarker != null)
            {
                currentLastMoveMarker.transform.position = pos;
                currentLastMoveMarker.transform.SetAsLastSibling();
            }
        }
        else //판이 비었다면(마지막 한 수) 마커 완전 삭제
        {
            if (currentLastMoveMarker != null)
            {
                Destroy(currentLastMoveMarker);
                currentLastMoveMarker = null;
            }
        }

        //화면 갱신
        UpdateUI();
        UpdateForbiddenMarkers();

        SetStatus("무르기를 사용했습니다.", 1.5f);
    }

    private void SwitchTurn()
    {
        currentTurn = (currentTurn == StoneType.Black) ? StoneType.White : StoneType.Black;
        UpdateUI();
        UpdateForbiddenMarkers();   //바뀐 턴에 맞게 금수 마커 갱신
    }

    //마우스가 UI나 화면 밖으로 나갔을 때 시스템 커서로 바꿔주는 로직
    private void UpdateCursorVisibility()
    {
        if (handSpriteRenderer != null && !handSpriteRenderer.enabled)
        {
            handSpriteRenderer.enabled = true;
        }

        bool isOutsideScreen = Input.mousePosition.x <= 0 || Input.mousePosition.x >= Screen.width ||
                               Input.mousePosition.y <= 0 || Input.mousePosition.y >= Screen.height;

        if (isOutsideScreen)
        {
            if (!Cursor.visible)
                Cursor.visible = true;      //일반 마우스로 변경
        }
       else
        {
            if (Cursor.visible)
                Cursor.visible = false;     //손 이미지로 커서 변경

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = -5f;
            if (handCursorTransform != null)
            {
                handCursorTransform.position = mouseWorldPos + handOffset;
            }

            bool isOverUI = EventSystem.current.IsPointerOverGameObject();

            if (isGameOver || isOverUI)
            {
                if (Input.GetMouseButton(0))
                {
                    handSpriteRenderer.sprite = clickPointerSprite;
                }
                else
                {
                    handSpriteRenderer.sprite = defaultPointerSprite;
                }
            }
            else
            {
                handSpriteRenderer.sprite = (currentTurn == StoneType.Black) ? blackHandSprite : whiteHandSprite;
            }
        }
    }

    private void UpdateForbiddenMarkers()
    {
        //기존 마커 제거
        foreach (var marker in forbiddenMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }

        forbiddenMarkers.Clear();

        if (currentTurn != StoneType.Black || isGameOver) return;   //백돌 턴이거나 끝났으면 생성 안 함

        //빈칸을 순회하며 금수 자리면 마커 생성
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
                            textComp.text = reason; //왜 금수인지 텍스트(33, 44, 장목) 표시
                        }
                        forbiddenMarkers.Add(marker);
                    }
                }     
            }
        }
    }

    //돌 프리팹을 실제로 생성하고 마지막 착수 마커를 씌워주는 역할
    private GameObject SpawnStoneVisual(int x, int y, StoneType color)
    {
        GameObject prefab = (color == StoneType.Black) ? blackStonePrefab : whiteStonePrefab;
        Vector2 pos = boardOrigin + new Vector2(x * cellSize.x, y * cellSize.y);
        GameObject newStone = Instantiate(prefab, pos, Quaternion.identity, boardRoot);

        if (lastMoveMarkerPrefab != null)
        {
            if (currentLastMoveMarker == null)
            {
                currentLastMoveMarker = Instantiate(lastMoveMarkerPrefab, pos, Quaternion.identity, boardRoot);
            }
            else
            {
                currentLastMoveMarker.transform.position = pos;

                currentLastMoveMarker.transform.SetAsLastSibling();     //마커가 돌 위로 올라오게 렌더링
            }
        }

        return newStone;
    }

    private void EndGame(string message, StoneType winner)
    {
        isGameOver = true;
        winColor = winner;

        UpdateForbiddenMarkers();           //게임 끝나면 금수 마커 숨김

        if (gameOverText != null)
        {
            gameOverText.text = message;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);  //결과창(플레이어1/2 선택) 띄우기
        }
    }
    
    public void RestartGame()
    { 
        boardData.ClearBoard();             //보드 데이터, 씬의 돌 오브젝트, 무르기 기록 싹 초기화
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
        moveHistory.Clear();

        UpdateUI();
        UpdateForbiddenMarkers();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (startButtonText != null)
        {
            startButtonText.text = "다시 시작";
        }

        SetStatus("게임 시작!", 1.5f);
    }

    //착수 시 손목 스냅을 주며 클릭하는 느낌을 내는 코루틴
    private IEnumerator HandClickAnimationRoutine()
    {
        Vector3 originalScale = handCursorTransform.localScale;
        Vector3 pressScale = originalScale * 0.85f;     //작아짐

        Quaternion originalRot = handCursorTransform.rotation;
        Quaternion pressRot = originalRot * Quaternion.Euler(0, 0, 15f);    //살짝 기울어짐

        float downDuration = 0.04f;     //착수할때 속도
        float timer = 0f;

        while (timer < downDuration)
        {
            timer += Time.deltaTime;
            float t = timer / downDuration;

            handCursorTransform.localScale = Vector3.Lerp(originalScale, pressScale, t);
            handCursorTransform.rotation = Quaternion.Lerp(originalRot, pressRot, t);
            yield return null;
        }

        timer = 0f;
        float upDuration = 0.07f;       //손을 떼는 속도

        while (timer < upDuration)
        {
            timer += Time.deltaTime;
            float t = timer / upDuration;

            handCursorTransform.localScale = Vector3.Lerp(pressScale, originalScale, t);
            handCursorTransform.rotation = Quaternion.Lerp(pressRot, originalRot, t);
            yield return null;
        }

        handCursorTransform.localScale = originalScale;
        handCursorTransform.rotation = originalRot;
    }


    private void UpdateUI()
    {
        if (turnText != null)
        {
            turnText.text = $"{(currentTurn == StoneType.Black ? "흑돌 차례" : "백돌 차례")}";
            turnText.color = (currentTurn == StoneType.Black) ? Color.black : Color.white;
        }
            
    }

    //일정 시간동안 떳다가 사라지는 상태 메시지
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

    //게임 종료 팝업에서 승리자를 골랐을 때 점수를 올리고 팝업을 닫는 함수
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

    private void OnDestroy()
    {
        Cursor.visible = true; // 마우스 무조건 켜기
        Cursor.lockState = CursorLockMode.None; // 혹시라도 마우스가 화면 중앙에 갇혔을(Locked) 경우를 대비해 잠금 해제
    }
}

