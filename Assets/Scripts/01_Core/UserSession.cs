using UnityEngine;

// 로그인한 사용자 정보를 씬 전환 이후에도 유지하기 위한 싱글톤 세션 스크립트
// userId와 nickname을 전역적으로 보관하며, 중복 생성 시 기존 인스턴스를 유지

public class UserSession : MonoBehaviour
{
    public static UserSession Instance;

    public string userId;
    public string nickname;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}