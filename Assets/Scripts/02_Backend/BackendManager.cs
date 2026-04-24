using UnityEngine;
using BackEnd;

// 뒤끝 SDK를 초기화하고, 초기화 성공 여부를 콘솔 로그로 확인하는 스크립트
// 프로젝트 시작 시 BackEnd 기능을 사용하기 전에 먼저 실행되어야 함

public class BackendManager : MonoBehaviour
{
    void Start()
    {
        var bro = Backend.Initialize();

        if (bro.IsSuccess())
        {
            Debug.Log("뒤끝 초기화 성공");
        }
        else
        {
            Debug.LogError("뒤끝 초기화 실패 : " + bro);
        }
    }
}