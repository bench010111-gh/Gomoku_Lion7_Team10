using UnityEngine;

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