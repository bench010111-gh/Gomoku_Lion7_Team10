using UnityEngine;

// 오목판의 현재 상태를 15x15 배열로 저장하고, 각 칸의 돌 정보를 읽고 쓰는 공용 데이터 클래스
// 좌표 범위 검사와 보드 초기화 기능을 제공
// 공통 규칙 및 각 모드의 게임 진행 스크립트에서 함께 사용

//열거형타입들을 따로 정리해둔 스크립트 추가
//public enum StoneType
//{
//    Empty = 0,
//    Black = 1,
//    White = 2
//}


[System.Serializable]
public class BoardData
{
    public const int Size = 15;

    private StoneType[,] board = new StoneType[Size, Size];

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    public StoneType GetCell(int x, int y)
    {
        if (!IsInside(x, y))
        {
            Debug.LogWarning($"GetCell: 범위를 벗어난 좌표 ({x}, {y})");
            return StoneType.Empty;
        }

        return board[x, y];
    }

    public void SetCell(int x, int y, StoneType stone)
    {
        if (!IsInside(x, y))
        {
            Debug.LogWarning($"SetCell: 범위를 벗어난 좌표 ({x}, {y})");
            return;
        }

        board[x, y] = stone;
    }

    public void ClearBoard()
    {
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                board[x, y] = StoneType.Empty;
            }
        }
    }
}