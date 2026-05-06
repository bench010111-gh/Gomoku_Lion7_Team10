using UnityEngine;

// 멀티 테스트용 임시 규칙 스크립트
// 현재는 보드 범위, 빈 칸 여부, 자기 차례인지 여부만 검사
public class TempRule
{
    public bool CanPlaceStone(BoardData boardData, int x, int y, StoneType currentTurn, StoneType myStone)
    {
        if (boardData == null)
            return false;

        if (!boardData.IsInside(x, y))
            return false;

        if (boardData.GetCell(x, y) != StoneType.Empty)
            return false;

        if (currentTurn != myStone)
            return false;

        return true;
    }
}