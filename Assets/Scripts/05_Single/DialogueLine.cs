// DialogueLine.cs
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Header("Speaker Info")]
    public string speakerName;

    [TextArea(3, 6)]
    public string dialogueText;

    public Sprite speakerPortrait;
}