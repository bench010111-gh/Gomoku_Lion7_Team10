using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GomokuModel model;
    public GomokuView view;

    private StoneColor currentTurn = StoneColor.Black;

    private bool isGameOver = false;

    [Header("테스트(돌색 고정) 설정")]
    public bool isTestMode = false;

    [Header("UI설정")]
    public TextMeshProUGUI turnText;

    private void Awake()
    {
        model = new GomokuModel();
    }

    private void Start()
    {
        UpdateTurnUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameOver) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPos = view.GetGridIndex(Input.mousePosition);

            PlaceResult result = model.TryPlaceStone(gridPos.x, gridPos.y, currentTurn);

            if (result == PlaceResult.Success)
            {
                //해당 위치에 둘 수 있으면 착수
                view.DrawStone(gridPos.x, gridPos.y, currentTurn);

                //돌을 뒀을 때 5목으로 승리했는지 판별
                if (model.CheckWin(gridPos.x, gridPos.y, currentTurn))
                {
                    EndGame(currentTurn.ToString() + "승리!");
                    return;
                }

                if (model.IsDraw())
                {
                    EndGame("무승부! 보드가 가득 찼습니다.");
                    return;
                }

                //테스트모드가 아닐때는 정상적으로 돌아감
                if (!isTestMode)
                {
                    //턴 넘김
                    currentTurn = (currentTurn == StoneColor.Black) ? StoneColor.White : StoneColor.Black;
                    UpdateTurnUI();
                }              
            }
            else if (result == PlaceResult.Forbidden)
            {
                Debug.LogWarning($"[경고] {gridPos.x}, {gridPos.y} 위치는 금수(3-3, 4-4, 장목) 자리입니다!");
                //나중에 텍스트나 ui형식으로 뜨게 변경
            }
        }
    }

    private void EndGame(string message)
    {
        isGameOver = true;

        turnText.text = message;
        turnText.color = Color.red;

        Debug.Log(message);
    }

    private void UpdateTurnUI()
    {
        turnText.text = $"현재 턴 : {currentTurn}";
        turnText.color = (currentTurn == StoneColor.Black) ? Color.black : Color.gray;
    }

    // 테스트 모드 On/Off 토글
    public void SetTestMode(bool isOn)
    {
        isTestMode = isOn;
    }

    // 특정 색상으로 턴 강제 변경
    public void ChangeFixedColor(int colorIndex) // 1: Black, 2: White
    {
        currentTurn = (StoneColor)colorIndex;
        UpdateTurnUI();
    }
}
