using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AIPopup : MonoBehaviour
{
    [SerializeField] AIGameSettingSO setting;

    [Header("난이도")]
    [SerializeField] Button hard;
    [SerializeField] Button normal;
    [SerializeField] Button easy;

    [Header("순서")]
    [SerializeField] Button firstMove;
    [SerializeField] Button secondMove;

    [Header("게임시작")]
    [SerializeField] Button gameStart; 

    Color32 selected = new Color32(255, 255, 255, 255);
    Color32 unselected = new Color32(160, 160, 160, 255);

    void OnEnable()
    {
        Difficulty currentDifficulty = setting.GetDifficulty();
        bool isFirstMove = setting.IsFirstMove();

        switch (currentDifficulty)
        {
            case Difficulty.HARD:
                UpdateDifficultyButtons(hard);
                break;

            case Difficulty.NORMAL:
                UpdateDifficultyButtons(normal);
                break;

            case Difficulty.EASY:
                UpdateDifficultyButtons(easy);
                break;
        }

        if (isFirstMove)
            UpdateOrderButtons(firstMove);
        else
            UpdateOrderButtons(secondMove);
    }
    void Start()
    {
        hard.onClick.AddListener(()=>
        {
            OnUpdateDifficulty(Difficulty.HARD);
            UpdateDifficultyButtons(hard);
        });
        normal.onClick.AddListener(() =>
        {
            OnUpdateDifficulty(Difficulty.NORMAL);
            UpdateDifficultyButtons(normal);
        });
        easy.onClick.AddListener(()=> 
        { 
            OnUpdateDifficulty(Difficulty.EASY);
            UpdateDifficultyButtons(easy); 
        });

        firstMove.onClick.AddListener(() =>
        {
            OnUpdateOrder(true);
            UpdateOrderButtons(firstMove); 
        });
        secondMove.onClick.AddListener(()=>
        {
            OnUpdateOrder(false);
            UpdateOrderButtons(secondMove);
        });

        gameStart.onClick.AddListener(() => LoadAIScene()); 
    }

    void OnUpdateDifficulty(Difficulty value)
    {
        setting.SetDifficulty(value); 
    }
    void OnUpdateOrder(bool isFirstMove)
    {
        setting.SetOrder(isFirstMove);
    }
    void UpdateDifficultyButtons(Button selectedButton)
    {
        SetButtonColor(hard, unselected);
        SetButtonColor(normal, unselected);
        SetButtonColor(easy, unselected);

        SetButtonColor(selectedButton, selected);
    }
    void UpdateOrderButtons(Button selectedButton)
    {
        SetButtonColor(firstMove, unselected);
        SetButtonColor(secondMove, unselected);

        SetButtonColor(selectedButton, selected);
    }
    void SetButtonColor(Button button, Color color)
    {
        button.image.color = color;
    }
    void LoadAIScene()
    {
        SceneTransitionManager.Instance.ChangeScene("08_AIGameScene");
        //SceneManager.LoadScene("08_AIGameScene");
    }

    public void OnDeactive()
    {
        this.gameObject.SetActive(false);
    }
}
