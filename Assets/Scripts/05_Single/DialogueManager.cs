// DialogueManager.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialogueUI;

    [SerializeField] private TMP_Text nameBox;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private Image speakerImage;

    [Header("Next Indicator")]
    [SerializeField] private RectTransform nextArrow;
    [SerializeField] private float arrowFloatAmount = 10f;
    [SerializeField] private float arrowFloatSpeed = 3f;

    [Header("Dialogue Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Dialogue Data")]
    [SerializeField] private List<DialogueLine> dialogueLines = new();

    [Header("Status")]
    public bool isDialogueActive;
    public bool isTyping;

    private int currentLineIndex;

    private Coroutine typingCoroutine;

    private Vector2 arrowStartPos;

    void Start()
    {
        // Arrow setup
        if (nextArrow != null)
        {
            arrowStartPos = nextArrow.anchoredPosition;
            nextArrow.gameObject.SetActive(false);
        }

        // Start opening dialogue immediately
        StartDialogue();
    }

    void Update()
    {
        if (!isDialogueActive)
            return;

        // Press any key
        if (Input.anyKeyDown)
        {
            // If typing -> instantly complete text
            if (isTyping)
            {
                CompleteTyping();
            }
            // If already finished -> next line
            else
            {
                NextDialogue();
            }
        }

        // Floating arrow animation
        if (nextArrow != null && nextArrow.gameObject.activeSelf)
        {
            float yOffset =
                Mathf.Sin(Time.time * arrowFloatSpeed)
                * arrowFloatAmount;

            nextArrow.anchoredPosition =
                arrowStartPos + new Vector2(0, yOffset);
        }
    }

    public void StartDialogue()
    {
        if (dialogueLines.Count == 0)
            return;

        currentLineIndex = 0;

        isDialogueActive = true;

        dialogueUI.SetActive(true);

        ShowLine();
    }

    void ShowLine()
    {
        DialogueLine line = dialogueLines[currentLineIndex];

        nameBox.text = line.speakerName;

        dialogueText.text = "";

        speakerImage.sprite = line.speakerPortrait;

        if (nextArrow != null)
        {
            nextArrow.gameObject.SetActive(false);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine =
            StartCoroutine(TypeText(line.dialogueText));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;

        dialogueText.text = "";

        // Korean-safe typing
        for (int i = 0; i <= text.Length; i++)
        {
            dialogueText.text = text.Substring(0, i);

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // Show arrow after typing complete
        if (nextArrow != null)
        {
            nextArrow.gameObject.SetActive(true);
        }
    }

    void CompleteTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text =
            dialogueLines[currentLineIndex].dialogueText;

        isTyping = false;

        // Show arrow immediately
        if (nextArrow != null)
        {
            nextArrow.gameObject.SetActive(true);
        }
    }

    void NextDialogue()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    void EndDialogue()
    {
        isDialogueActive = false;

        dialogueUI.SetActive(false);

        dialogueText.text = "";
        nameBox.text = "";

        if (nextArrow != null)
        {
            nextArrow.gameObject.SetActive(false);
        }
    }
}