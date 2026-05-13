using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OmokReplayManager : MonoBehaviour
{
    [Header("Board")]
    public Transform boardRoot;
    public Vector2 boardOrigin = Vector2.zero;
    public float cellSize = 1f;

    [Header("Stone Prefabs")]
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;

    [Header("UI")]
    public TMP_Text replayInfoText;

    private List<OmokMoveRecord> moves = new List<OmokMoveRecord>();
    private int currentIndex = 0;

    public void LoadReplay(string movesJson)
    {
        ClearBoard();

        if (string.IsNullOrEmpty(movesJson))
        {
            SetInfo("기보 데이터가 없습니다.");
            return;
        }

        OmokMoveRecordList list = JsonUtility.FromJson<OmokMoveRecordList>(movesJson);

        if (list == null || list.moves == null || list.moves.Count <= 0)
        {
            SetInfo("빈 기보입니다.");
            return;
        }

        moves = list.moves;
        currentIndex = 0;

        SetInfo($"불러오기 완료: 총 {moves.Count}수");
    }

    public void OnClickNextMove()
    {
        PlayClickSound();

        if (moves == null || moves.Count <= 0)
        {
            SetInfo("기보가 없습니다.");
            return;
        }

        if (currentIndex >= moves.Count)
        {
            SetInfo("마지막 수입니다.");
            return;
        }

        OmokMoveRecord move = moves[currentIndex];
        SpawnStone(move.x, move.y, (StoneType)move.stone);

        currentIndex++;

        string stoneText = (StoneType)move.stone == StoneType.Black ? "흑" : "백";
        SetInfo($"{currentIndex}/{moves.Count}수 - {stoneText} ({move.x}, {move.y})");
    }

    public void OnClickPrevReset()
    {
        PlayClickSound();

        ClearBoard();

        currentIndex = 0;

        SetInfo($"처음으로 돌아감: 0/{moves.Count}수");
    }

    public void OnClickAutoReplayAll()
    {
        PlayClickSound();

        ClearBoard();

        currentIndex = 0;

        for (int i = 0; i < moves.Count; i++)
        {
            OmokMoveRecord move = moves[i];
            SpawnStone(move.x, move.y, (StoneType)move.stone);
        }

        currentIndex = moves.Count;

        SetInfo($"전체 복기 표시 완료: {moves.Count}/{moves.Count}수");
    }

    private void SpawnStone(int x, int y, StoneType stone)
    {
        GameObject prefab = null;

        if (stone == StoneType.Black)
            prefab = blackStonePrefab;
        else if (stone == StoneType.White)
            prefab = whiteStonePrefab;

        if (prefab == null)
        {
            Debug.LogWarning("복기 돌 프리팹이 연결되지 않았습니다.");
            return;
        }

        Vector2 pos = boardOrigin + new Vector2(x * cellSize, y * cellSize);
        Instantiate(prefab, pos, Quaternion.identity, boardRoot);
    }

    private void ClearBoard()
    {
        if (boardRoot == null)
            return;

        for (int i = boardRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(boardRoot.GetChild(i).gameObject);
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }
    }

    private void SetInfo(string message)
    {
        if (replayInfoText != null)
            replayInfoText.text = message;

        Debug.Log(message);
    }
}