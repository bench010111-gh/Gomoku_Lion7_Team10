public class GomokuModel
{
    public const int BoardSize = 15;
    private StoneColor[,] board = new StoneColor[BoardSize, BoardSize];

    private int placedStoneCount = 0;

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
        if (color == StoneColor.Black && IsForbidden(x, y, color))
        {
            return PlaceResult.Forbidden;
        }

        board[x,y] = color;
        placedStoneCount++;
        return PlaceResult.Success;
    }

    public StoneColor GetStone(int x, int y)
    {
        return board[x, y];
    }

    //승리판별 함수
    public bool CheckWin(int x, int y, StoneColor color)
    {
        int[,] directions = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };

        for (int i = 0; i < 4; i++)
        {
            int dx = directions[i, 0];
            int dy = directions[i, 1];

            int count = 1;

            count += CountStones(x, y, dx, dy, color);

            count += CountStones(x, y, -dx, -dy, color);

            //흑돌은 무조건 5수여야 승리
            if (color == StoneColor.Black && count == 5) 
                return true;
            //백돌은 5수 이상이라면 승리
            if (color == StoneColor.White && count >= 5)
                return true;
        }

        return false;
    }

    //오목판이 가득 찼는지 판별하는 함수
    public bool IsBoardFull()
    {
        return placedStoneCount >= BoardSize * BoardSize;
    }

    //착수시 돌의 개수를 판별하기 위한 함수
    private int CountStones(int startX, int startY, int dx, int dy, StoneColor color)
    {
        int count = 0;

        int nextX = startX + dx;
        int nextY = startY + dy;

        while (nextX >= 0 && nextX < BoardSize && nextY >= 0 && nextY < BoardSize && board[nextX, nextY] == color)
        {
            count++;

            nextX += dx;
            nextY += dy;
        }

        return count;
    }

    //금수 판정
    private bool IsForbidden(int x, int y, StoneColor color)
    {
        board[x, y] = color; //가상착수

        int openThreeCount = 0; //열린 3목 체크
        int fourCount = 0; //4수 체크
        bool isOverline = false; //6수 이상인지 체크

        int[,] directions = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };

        for (int i = 0; i < 4; i++)
        {
            int dx = directions[i, 0];
            int dy = directions[i, 1];

            //축에 놓인 돌들의 모양을 분석하여 패턴을 분석
            LinePattern pattern = AnalyzeAxis(x, y, dx, dy, color);

            if (pattern == LinePattern.Overline) isOverline = true;
            else if (pattern == LinePattern.Four) fourCount++;
            else if (pattern == LinePattern.OpenThree) openThreeCount++;
        }

        board[x, y] = StoneColor.None; //가상착수 제거(원상태 복구)

        if (isOverline) return true; //장목 금수
        if (fourCount >= 2) return true; // 44금수
        if (openThreeCount >= 2) return true; // 33금수

        return false;
    }

    //가상 착수 후 축에 대해서 분석
    private LinePattern AnalyzeAxis(int x, int y, int dx, int dy, StoneColor color)
    {
        string line = GetAxisString(x, y, dx, dy, color);

        if (line.Contains("XXXXXX"))
            return LinePattern.Overline; //6목이상
        if (line.Contains("XXXXX"))
            return LinePattern.None; //5목은 문제없으니 none

        //착수시 5목이 되는 빈칸이 몇개인지 구함
        int myThreats = CountWinningSpaces(line);
        if (myThreats > 0)
            return LinePattern.Four; //한개라도 있으면 무조건 4

        int openFourCreators = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '_')
            {
                string testLine = line.Substring(0, i) + "X" + line.Substring(i + 1);

                if (CountWinningSpaces(testLine) >= 2)
                {
                    openFourCreators++;
                }
            }
        }

        if (openFourCreators >= 2)
            return LinePattern.OpenThree;

        return LinePattern.None;
    }

    //5목이 완성되는 빈칸의 개수를 새는 함수
    private int CountWinningSpaces(string line)
    {
        int count = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '_')
            {
                string testLine = line.Substring(0, i) + "X" + line.Substring(i + 1);

                if (testLine.Contains("XXXXX") && !testLine.Contains("XXXXXX"))
                    count++;
            }
        }

        return count;
    }

    private string GetAxisString(int x, int y, int dx, int dy, StoneColor color)
    {
        string result = "";

        for (int i = -5; i < 5; i++)
        {
            int nx = x + dx * i;
            int ny = y + dy * i;

            if (nx < 0 || nx >= BoardSize || ny < 0 || ny >= BoardSize)
            {
                result += "O";
            }
            else
            {
                StoneColor current = board[nx, ny];
                if (current == color) result += "X";
                else if (current == StoneColor.None) result += "_";
                else result += "O";
            }
        }

        return result;
    }
}
