using TMPro;
using UnityEngine;
using BackEnd;

// 메인 로비에서 현재 로그인한 플레이어의 닉네임, 전적, 승률 정보를 뒤끝 USER_DATA 테이블에서 불러와 표시하는 스크립트
// 로그인 세션 정보와 저장된 전적 데이터를 기반으로 메인 로비 UI를 갱신

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
            nicknameText.text = $"회원명: {UserSession.Instance.nickname}";
        }

        var bro = Backend.PlayerData.GetMyData(TableName, new string[] { "winCount", "loseCount", "drawCount" }, 1);

        if (!bro.IsSuccess())
        {
            winRateText.text = "승률 불러오기 실패";
            Debug.LogError("회원정보 조회 실패: " + bro);
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