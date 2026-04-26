using System;
using System.Collections.Generic;

public class GomokuRule
{
    private int boardSize;

    public GomokuRule(int size)
    {
        boardSize = size;
    }

    //승리판별 함수
    public bool CheckWin(StoneColor[,] board, int x, int y, StoneColor color)
    {
        int[,] directions = { { 1, 0 }, { 0, 1 }, { 1, 1 }, { 1, -1 } };

        for (int i = 0; i < 4; i++)
        {
            int dx = directions[i, 0];
            int dy = directions[i, 1];

            int count = 1;

            count += CountStones(board, x, y, dx, dy, color);

            count += CountStones(board, x, y, -dx, -dy, color);

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
    public bool IsDraw(int placedStoneCount)
    {
        return placedStoneCount >= boardSize * boardSize;
    }

    //착수시 돌의 개수를 판별하기 위한 함수
    private int CountStones(StoneColor[,] board, int startX, int startY, int dx, int dy, StoneColor color)
    {
        int count = 0;

        int nextX = startX + dx;
        int nextY = startY + dy;

        while (nextX >= 0 && nextX < boardSize && nextY >= 0 && nextY < boardSize && board[nextX, nextY] == color)
        {
            count++;

            nextX += dx;
            nextY += dy;
        }

        return count;
    }

    //금수 판정
    public bool IsForbidden(StoneColor[,] board, int x, int y, StoneColor color)
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
            LinePattern pattern = AnalyzeAxis(board,x, y, dx, dy, color);

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
    private LinePattern AnalyzeAxis(StoneColor[,] board, int x, int y, int dx, int dy, StoneColor color)
    {
        string line = GetAxisString(board, x, y, dx, dy, color);

        if (line.Contains("XXXXXX"))
            return LinePattern.Overline; //6목이상
        if (line.Contains("XXXXX"))
            return LinePattern.None; //5목은 문제없으니 none

        List<int> threatIndices = GetWinningSpacesIndices(line);

        if (threatIndices.Count > 0)
        {
            if (threatIndices.Count >= 2)
            {
                bool isOpenFour = false;
                for (int a = 0; a < threatIndices.Count; a++)
                {
                    for (int b = a + 1; b < threatIndices.Count; b++)
                    {
                        if (Math.Abs(threatIndices[a] - threatIndices[b]) == 5)
                        {
                            isOpenFour = true;
                        }
                    }
                }

                if (!isOpenFour)
                    return LinePattern.Overline;
            }

            return LinePattern.Four; //위협이 1개거나 , 열린 4인 경우 정상적으로 1개의 4판정
        }


        int openFourCreators = 0;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '_')
            {
                string testLine = line.Substring(0, i) + "X" + line.Substring(i + 1);

                if (GetWinningSpacesIndices(testLine).Count >= 2)
                {
                    openFourCreators++;
                }
            }
        }

        //열린 4가 하나라도 있다면 금수판정
        if (openFourCreators >= 1)
            return LinePattern.OpenThree;

        return LinePattern.None;
    }

    //5목이 완성되는 빈칸의 개수를 새는 함수
    private List<int> GetWinningSpacesIndices(string line)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '_')
            {
                string testLine = line.Substring(0, i) + "X" + line.Substring(i + 1);
                if (testLine.Contains("XXXXX") && !testLine.Contains("XXXXXX"))
                {
                    indices.Add(i);
                }
            }
        }

        return indices;
    }

    private string GetAxisString(StoneColor[,] board, int x, int y, int dx, int dy, StoneColor color)
    {
        string result = "";

        for (int i = -5; i < 5; i++)
        {
            int nx = x + dx * i;
            int ny = y + dy * i;

            if (nx < 0 || nx >= boardSize || ny < 0 || ny >= boardSize)
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
