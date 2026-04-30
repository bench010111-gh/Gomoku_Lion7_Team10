using System.Collections;
using UnityEngine;

public class AI_GameSimulator : MonoBehaviour
{
    const int BOARD_SIZE = 15;

    int[,] board = new int[BOARD_SIZE, BOARD_SIZE];
    AI_Cell[,] visualBoard = new AI_Cell[BOARD_SIZE, BOARD_SIZE];

    float interval = 0.6f;

    [SerializeField] AI ai;
    [SerializeField] AI_Cell cellPrefab;

    [SerializeField] int timeLimit = 3000;
    [SerializeField] int maxDepth = 10;

    // 인스펙터 보드판 데이터 (225칸 = 15x15, 0:빈칸 1:흑 2:백)
    public int[] presetBoard = new int[BOARD_SIZE * BOARD_SIZE];

    int currentTurn = 1;
    int myTurn = -1;
    int aiTurn = -1;

    void Awake()
    {
        CreateBoard();
        PlacePresetStones();
        RandomOrder();
    }

    void PlacePresetStones()
    {
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                int val = presetBoard[y * BOARD_SIZE + x];
                if (val == 0) continue;

                board[x, y] = val;
                visualBoard[x, y].ChangeColor(val);
                Debug.Log($"프리셋 배치: ({x}, {y}) player={val}");
            }
        }
    }

    void CreateBoard()
    {
        float offset = (BOARD_SIZE - 1) * interval / 2f;

        for (int y = 0; y < BOARD_SIZE; y++)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                float xPos = x * interval - offset;
                float yPos = y * interval - offset;

                GameObject cellObj = Instantiate(cellPrefab.gameObject, new Vector3(xPos, yPos, 0), Quaternion.identity, this.transform);
                cellObj.name = $"{x},{y}";

                AI_Cell cell = cellObj.GetComponent<AI_Cell>();
                cell.Init(x, y);
                cell.cellClicked += OnCellClicked;

                visualBoard[x, y] = cell;
            }
        }
    }

    void RandomOrder()
    {
        //int random = Random.Range(1, 3);
        //myTurn = random;
        //aiTurn = myTurn == 1 ? 2 : 1;

        myTurn = 2;
        aiTurn = 1;
        
        Debug.Log(myTurn == 1 ? "플레이어 흑돌 선공" : "AI 흑돌 선공");
        if (aiTurn == 1) DoAITurn();
    }

    void OnCellClicked(int x, int y)
    {
        if (currentTurn != myTurn) return;
        if (board[x, y] != 0) { Debug.Log($"{x},{y} 칸에는 이미 돌이 존재합니다."); return; }
        PlaceStone(new Vector2Int(x, y), myTurn);
        currentTurn = aiTurn;
        Debug.Log("턴 변경: AI 턴");
        DoAITurn();
    }

    void DoAITurn() { StartCoroutine(DoAITurnCoroutine()); }

    IEnumerator DoAITurnCoroutine()
    {
        Debug.Log("AI 생각 중...");
        yield return null;
        Vector2Int bestSelection = ai.GetBestMove(board, aiTurn, timeLimit, maxDepth);
        PlaceStone(bestSelection, aiTurn);
        Debug.Log("AI 착수 완료");
        currentTurn = myTurn;
        Debug.Log("턴 변경: 플레이어턴");
    }

    void PlaceStone(Vector2Int pos, int player)
    {
        if (board[pos.x, pos.y] != 0)
            Debug.LogError($"이미 돌이 두어진 자리입니다. {pos.x}, {pos.y}");
        board[pos.x, pos.y] = player;
        visualBoard[pos.x, pos.y].ChangeColor(player);
        Debug.Log($"{pos.x}, {pos.y} 착수");
    }
}