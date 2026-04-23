using UnityEngine;
using BackEnd;

public static class PlayerDataService
{
    private const string TableName = "USER_DATA";

    public static bool CreateDefaultData(string nickname)
    {
        Param param = new Param();
        param.Add("nickname", nickname);
        param.Add("winCount", 0);
        param.Add("loseCount", 0);
        param.Add("drawCount", 0);

        var bro = Backend.PlayerData.InsertData(TableName, param);
        return bro.IsSuccess();
    }
}