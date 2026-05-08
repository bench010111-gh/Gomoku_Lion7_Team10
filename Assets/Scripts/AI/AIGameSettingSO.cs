using UnityEngine;
public enum Difficulty
{
    EASY, 
    NORMAL, 
    HARD
}

[CreateAssetMenu(menuName = "GameSettings/AIGameSettingSO", fileName = "AIGameSettingSO")]
public class AIGameSettingSO : ScriptableObject
{
    [Header("난이도")]
    public Difficulty difficulty;
    [Header("선공")]
    public bool isFirstMove; 

    public void Set(Difficulty difficulty, bool isFirstMove)
    {
        this.difficulty = difficulty;
        this.isFirstMove = isFirstMove;
    }

    public Difficulty GetDifficulty() => difficulty; 
    public bool IsFirstMove() => isFirstMove;
}
