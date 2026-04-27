public class GomokuModel //오목판 상태 저장 스크립트
{
    public const int BoardSize = 15;
    private StoneColor[,] board = new StoneColor[BoardSize, BoardSize];

    private int placedStoneCount = 0;

    private GomokuRule rule;

    public GomokuModel()
    {
        rule = new GomokuRule(BoardSize);
    }

    //착수 가능 판단하는 함수
    public PlaceResult TryPlaceStone(int x, int y, StoneColor color)
    {
        //오목판의 범위를 벗어나면 둘 수 없음
        if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize)
            return PlaceResult.OutOfBounds;

        //이미 돌이 있다면 둘 수 없음
        if (board[x,y] != StoneColor.None)
            return PlaceResult.AlreadyPlaced;

        //흑돌이라면 렌주롤에 대한 금수 검사를 해야함 백돌이라면 조건이 없으니 상관없음
        if (color == StoneColor.Black && rule.IsForbidden(board, x, y, color))
        {
            return PlaceResult.Forbidden;
        }

        board[x,y] = color;
        placedStoneCount++;
        return PlaceResult.Success;
    }

    //승리했는지 확인하는 함수
    public bool CheckWin(int x, int y, StoneColor color)
    {
        return rule.CheckWin(board, x, y, color);
    }

    //무승부인지 확인하는 함수
    public bool IsDraw()
    {
        return rule.IsDraw(placedStoneCount);
    }

    //특정 좌표에 무슨 돌이 있는지 알려주기 위한 함수
    public StoneColor GetStone(int x, int y)
    {
        return board[x, y];
    } 
}
