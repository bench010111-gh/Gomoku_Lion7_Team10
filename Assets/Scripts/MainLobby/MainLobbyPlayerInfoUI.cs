using TMPro;
using UnityEngine;
using BackEnd;

public class MainLobbyPlayerInfoUI : MonoBehaviour
{
    public TMP_Text nicknameText;
    public TMP_Text record;
    public TMP_Text winRateText;

    private const string TableName = "USER_DATA";

    void Start()
    {
        LoadPlayerInfo();
    }

    public void LoadPlayerInfo()
    {
        if (UserSession.Instance != null)
        {
            nicknameText.text = $"플레이어: {UserSession.Instance.nickname}";
        }

        var bro = Backend.PlayerData.GetMyData(TableName, new string[] { "winCount", "loseCount", "drawCount" }, 1);

        if (!bro.IsSuccess())
        {
            winRateText.text = "승률 불러오기 실패";
            Debug.LogError("플레이어 데이터 조회 실패: " + bro);
            return;
        }

        if (bro.FlattenRows().Count <= 0)
        {
            winRateText.text = "승률: 0% (0/0)";
            return;
        }

        var row = bro.FlattenRows()[0];

        int win = row.ContainsKey("winCount") ? int.Parse(row["winCount"].ToString()) : 0;
        int lose = row.ContainsKey("loseCount") ? int.Parse(row["loseCount"].ToString()) : 0;
        int draw = row.ContainsKey("drawCount") ? int.Parse(row["drawCount"].ToString()) : 0;

        int total = win + lose + draw;
        float rate = total > 0 ? (win * 100f / total) : 0f;

        record.text = $"전적 : {total}전 {win}승 {draw}무 {lose}패";
        winRateText.text = $"승률: {rate:0.#}% ({win}/{total})";
    }
}