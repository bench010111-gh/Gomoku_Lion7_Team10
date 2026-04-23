using UnityEngine;
using BackEnd;

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