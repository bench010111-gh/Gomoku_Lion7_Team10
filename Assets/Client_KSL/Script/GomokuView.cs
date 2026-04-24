using UnityEngine;

public class GomokuView : MonoBehaviour
{
    [Header("오목알 프리팹")]
    public GameObject blackPrefab;
    public GameObject whitePrefab;

    [Header("오목판 설정")]
    public float cellSize = 1.0f;
    public Vector2 offset;

    public Vector2Int GetGridIndex(Vector3 mousePos)
    {
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        int x = Mathf.RoundToInt((worldPos.x - offset.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - offset.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public void DrawStone(int x, int y, StoneColor Color)
    {
        GameObject prefab = (Color == StoneColor.Black) ? blackPrefab : whitePrefab;
        Vector3 spawnPos = new Vector3(x * cellSize + offset.x, y * cellSize + offset.y, 0);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}
