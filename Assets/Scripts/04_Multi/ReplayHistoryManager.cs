using BackEnd;
using LitJson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayHistoryManager : MonoBehaviour
{
    private const string TableName = "MATCH_HISTORY";

    [Header("Replay")]
    public OmokReplayManager replayManager;

    [Header("UI")]
    public Transform historyListContent;
    public GameObject historyListItemPrefab;
    public TMP_Text statusText;

    private void Start()
    {
        LoadMyMatchHistory();
    }

    public void LoadMyMatchHistory()
    {
        if (historyListContent == null || historyListItemPrefab == null)
        {
            SetStatus("복기 목록 UI가 연결되지 않았습니다.");
            return;
        }

        ClearHistoryList();

        var bro = Backend.PlayerData.GetMyData(
            TableName,
            new string[]
            {
                "matchId",
                "roomName",
                "createdAt",
                "myNickname",
                "opponentNickname",
                "myStone",
                "result",
                "winnerStone",
                "isDraw",
                "isResign",
                "movesJson"
            },
            50
        );

        if (!bro.IsSuccess())
        {
            SetStatus("기보 목록 조회 실패: " + bro);
            Debug.LogError("기보 목록 조회 실패: " + bro);
            return;
        }

        JsonData rows = bro.FlattenRows();

        if (rows.Count <= 0)
        {
            SetStatus("저장된 기보가 없습니다.");
            return;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            JsonData row = rows[i];

            string createdAt = GetString(row, "createdAt");
            string opponent = GetString(row, "opponentNickname");
            string result = GetString(row, "result");
            string myStone = GetString(row, "myStone");
            string isResignText = GetBool(row, "isResign") ? " / 기권" : "";
            string movesJson = GetString(row, "movesJson");

            GameObject item = Instantiate(historyListItemPrefab, historyListContent);

            TMP_Text itemText = item.GetComponentInChildren<TMP_Text>();
            Button itemButton = item.GetComponent<Button>();

            if (itemText != null)
            {
                itemText.text =
                    $"{createdAt}\n" +
                    $"상대: {opponent} / 결과: {ConvertResultText(result)}{isResignText} / 내 돌: {ConvertStoneText(myStone)}";
            }

            if (itemButton != null)
            {
                string capturedMovesJson = movesJson;
                itemButton.onClick.AddListener(() =>
                {
                    OnClickHistoryItem(capturedMovesJson);
                });
            }
        }

        SetStatus($"기보 {rows.Count}개 불러오기 완료");
    }

    private void OnClickHistoryItem(string movesJson)
    {
        if (replayManager == null)
        {
            SetStatus("OmokReplayManager가 연결되지 않았습니다.");
            return;
        }

        replayManager.LoadReplay(movesJson);
        SetStatus("기보를 불러왔습니다.");
    }

    private void ClearHistoryList()
    {
        if (historyListContent == null)
            return;

        for (int i = historyListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(historyListContent.GetChild(i).gameObject);
        }
    }

    private string GetString(JsonData row, string key)
    {
        if (row == null)
            return "";

        if (!row.Keys.Contains(key))
            return "";

        return row[key].ToString();
    }

    private bool GetBool(JsonData row, string key)
    {
        if (row == null)
            return false;

        if (!row.Keys.Contains(key))
            return false;

        bool value;
        if (bool.TryParse(row[key].ToString(), out value))
            return value;

        return false;
    }

    private string ConvertResultText(string result)
    {
        switch (result)
        {
            case "Win":
                return "승리";
            case "Lose":
                return "패배";
            case "Draw":
                return "무승부";
            default:
                return result;
        }
    }

    private string ConvertStoneText(string stone)
    {
        switch (stone)
        {
            case "Black":
                return "흑";
            case "White":
                return "백";
            default:
                return stone;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }
}