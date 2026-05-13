using UnityEngine;
using BackEnd;
using LitJson;

// 뒤끝 USER_DATA 테이블에 신규 유저의 기본 전적 데이터를 생성하고,
// 게임 종료 후 승/패/무 및 복기용 MATCH_HISTORY를 저장하는 서비스 클래스
public static class PlayerDataService
{
    private const string UserDataTableName = "USER_DATA";
    private const string MatchHistoryTableName = "MATCH_HISTORY";

    public static bool CreateDefaultData(string nickname)
    {
        Param param = new Param();
        param.Add("nickname", nickname);
        param.Add("winCount", 0);
        param.Add("loseCount", 0);
        param.Add("drawCount", 0);

        var bro = Backend.PlayerData.InsertData(UserDataTableName, param);
        return bro.IsSuccess();
    }

    public static bool ApplyMatchResult(string nickname, bool isWin, bool isLose, bool isDraw)
    {
        var bro = Backend.PlayerData.GetMyData(UserDataTableName, 1);

        if (!bro.IsSuccess())
        {
            Debug.LogError("USER_DATA 조회 실패: " + bro);
            return false;
        }

        JsonData rows = bro.FlattenRows();

        if (rows.Count <= 0)
        {
            bool created = CreateDefaultData(nickname);

            if (!created)
            {
                Debug.LogError("USER_DATA 기본 데이터 생성 실패");
                return false;
            }

            bro = Backend.PlayerData.GetMyData(UserDataTableName, 1);

            if (!bro.IsSuccess())
            {
                Debug.LogError("USER_DATA 재조회 실패: " + bro);
                return false;
            }

            rows = bro.FlattenRows();

            if (rows.Count <= 0)
            {
                Debug.LogError("USER_DATA 생성 후에도 데이터가 없습니다.");
                return false;
            }
        }

        JsonData row = rows[0];

        string inDate = row["inDate"].ToString();

        int winCount = GetInt(row, "winCount");
        int loseCount = GetInt(row, "loseCount");
        int drawCount = GetInt(row, "drawCount");

        if (isWin)
            winCount++;

        if (isLose)
            loseCount++;

        if (isDraw)
            drawCount++;

        Param updateParam = new Param();
        updateParam.Add("nickname", nickname);
        updateParam.Add("winCount", winCount);
        updateParam.Add("loseCount", loseCount);
        updateParam.Add("drawCount", drawCount);

        var updateBro = Backend.PlayerData.UpdateMyData(UserDataTableName, inDate, updateParam);

        if (!updateBro.IsSuccess())
        {
            Debug.LogError("전적 업데이트 실패: " + updateBro);
            return false;
        }

        Debug.Log($"전적 업데이트 성공 - 승:{winCount}, 패:{loseCount}, 무:{drawCount}");
        return true;
    }

    public static string GetMyRecordText()
    {
        var bro = Backend.PlayerData.GetMyData(UserDataTableName, 1);

        if (!bro.IsSuccess())
        {
            Debug.LogError("전적 조회 실패: " + bro);
            return "전적 조회 실패";
        }

        JsonData rows = bro.FlattenRows();

        if (rows.Count <= 0)
        {
            return "0승 0무 0패";
        }

        JsonData row = rows[0];

        int winCount = GetInt(row, "winCount");
        int loseCount = GetInt(row, "loseCount");
        int drawCount = GetInt(row, "drawCount");

        return $"{winCount}승 {drawCount}무 {loseCount}패";
    }

    public static bool SaveMatchHistory(
        string matchId,
        string roomName,
        string myNickname,
        string opponentNickname,
        string myStone,
        string result,
        string winnerStone,
        bool isDraw,
        bool isResign,
        string movesJson
    )
    {
        Param param = new Param();
        param.Add("matchId", matchId);
        param.Add("roomName", roomName);
        param.Add("createdAt", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        param.Add("myNickname", myNickname);
        param.Add("opponentNickname", opponentNickname);
        param.Add("myStone", myStone);
        param.Add("result", result);
        param.Add("winnerStone", winnerStone);
        param.Add("isDraw", isDraw);
        param.Add("isResign", isResign);
        param.Add("movesJson", movesJson);

        var bro = Backend.PlayerData.InsertData(MatchHistoryTableName, param);

        if (!bro.IsSuccess())
        {
            Debug.LogError("기보 저장 실패: " + bro);
            return false;
        }

        Debug.Log("기보 저장 성공");
        return true;
    }

    private static int GetInt(JsonData row, string key)
    {
        if (row == null)
            return 0;

        if (!row.Keys.Contains(key))
            return 0;

        int value;
        if (int.TryParse(row[key].ToString(), out value))
            return value;

        return 0;
    }
}