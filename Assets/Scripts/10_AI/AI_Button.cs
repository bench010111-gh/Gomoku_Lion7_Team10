using UnityEngine;
using UnityEngine.UI;

public class AI_Button : MonoBehaviour
{
    [SerializeField] Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        if(button != null)
            button.onClick.AddListener(() => AudioManager.Instance.PlayClickSound());
    }
}
