using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Data", fileName = "DialogueData")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines;
}