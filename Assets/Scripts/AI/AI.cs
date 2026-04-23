using UnityEngine;

public class AI : MonoBehaviour
{
    const int FIVE = 100000;
    const int OPEN_FOUR = 10000;
    const int CLOSED_FOUR = 5000;
    const int OPEN_THREE = 1000;
    const int CLOSED_THREE = 500;
    const int OPEN_TWO = 100;
    const int CENTER = 10;

    public int Evaluate(int x, int y, int player, int[,] board)
    {
        int totalScore = 0;
        return totalScore; 
    }

    bool IsValidPosition(int x, int y)
    {
        return x >= 0 && y >= 0 && x < 15 && y < 15; 
    }

    int CountConnected(int x, int y, int dirX, int dirY, int[,] board)
    {
        int nx = x + dirX;
        int ny = y + dirY;

        if (!IsValidPosition(nx, ny)) return 0;
        if (board[nx, ny] != 1) return 0;

        return 1 + CountConnected(nx, ny, dirX, dirY, board); 
    }

    bool HasConnect6(int x, int y, int[,] board)
    {
        int[] dx = { 1, 0, 1, 1 };
        int[] dy = { 0, 1, 1, -1 };

        for(int i=0; i<4; i++)
        {
            int count = 1;

            count += CountConnected(x, y, dx[i], dy[i], board);
            count += CountConnected(x, y, -dx[i], -dy[i], board);

            if (count >= 6) return true; 
        }

        return false; 
    }

    bool HasFive(int x, int y, int[,] board)
    {
        int[] dx = { 1, 0, 1, 1 };
        int[] dy = { 0, 1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            int count = 1;

            count += CountConnected(x, y, dx[i], dy[i], board);
            count += CountConnected(x, y, -dx[i], -dy[i], board);

            if (count == 5) return true; 
        }

        return false; 
    }

    int CountFour(int x, int y, int[,] board)
    {
        for(int i=0; i<4; i++)
        {
            // Slide window 
        }

        return 0; 
    }

    bool HasDoubleFour(int x, int y, int[,] board)
    {
        return CountFour(x, y, board) >= 2; 
    }

    int CountOpenThree(int x, int y, int[,] board)
    {
        return 0;  
    }

    bool HasDoubleOpenThree(int x, int y, int[,] board)
    {
        return CountOpenThree(x, y, board) >= 2; 
    }
}

