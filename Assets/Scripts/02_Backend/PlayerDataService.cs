using UnityEngine;
using BackEnd;

// 뒤끝 USER_DATA 테이블에 신규 유저의 기본 전적 데이터를 생성하는 서비스 클래스
// 로그인 후 유저 데이터가 없을 때 기본 닉네임, 승/패/무 값을 초기값(0)으로 저장

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