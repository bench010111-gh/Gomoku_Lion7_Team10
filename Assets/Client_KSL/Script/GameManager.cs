using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour //게임의 흐름 통제(금수 테스트를 위한 임시 스크립트)
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
        //게임이 종료되었으면 마우스 클릭이 안되도록 막음
        if (isGameOver) return;

        if (Input.GetMouseButtonDown(0))
        {
            //마우스로 클릭한 좌표를 바둑판 x,y 인덱스로 변환
            Vector2Int gridPos = view.GetGridIndex(Input.mousePosition);

            //착수시도 후 결과값(가능, 금수, 승리, 무승부)을 가져옴
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

                //돌을 뒀을때 가득 찼으면 무승부
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

    //게임이 종료(승리 혹은 무승부)되면 호출되는 함수
    private void EndGame(string message)
    {
        isGameOver = true;

        turnText.text = message;
        turnText.color = Color.red;

        Debug.Log(message);
    }

    //턴이 바뀔때 마다 화면의 텍스트 등을 바꿔주는 함수
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
