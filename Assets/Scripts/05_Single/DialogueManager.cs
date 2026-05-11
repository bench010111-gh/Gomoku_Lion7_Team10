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

    /*
    =========================================
    OPTIONAL FADE SYSTEM
    =========================================

    If you want full-screen fade in/out:

    1. Create FadeScreen under Canvas
    2. Add:
        - Black Image
        - CanvasGroup
        - FadeManager.cs

    3. Drag FadeManager into this slot

    4. Uncomment the fade lines below
    */

    [SerializeField] private FadeManager fadeManager;

    [Header("Status")]
    public bool isDialogueActive;
    public bool isTyping;

    private int currentLineIndex;

    private Coroutine typingCoroutine;

    private Vector2 arrowStartPos;

    /*
    =========================================
    NORMAL START
    =========================================
    */

    //void Start()
    //{
    //    // Arrow setup
    //    if (nextArrow != null)
    //    {
    //        arrowStartPos = nextArrow.anchoredPosition;
    //        nextArrow.gameObject.SetActive(false);
    //    }

    //    // Normal dialogue start
    //    StartDialogue();
    //}

    /*
    =========================================
    FADE VERSION START
    =========================================
    */

    //Uncomment this and comment the normal
    //Start() above if using FadeManager.

    IEnumerator Start()
    {
        if (nextArrow != null)
        {
            arrowStartPos = nextArrow.anchoredPosition;
            nextArrow.gameObject.SetActive(false);
        }

        yield return StartCoroutine(fadeManager.FadeIn());

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

    /*
    =========================================
    OPTIONAL FADE OUT EXAMPLE
    =========================================

    If you want fade out after dialogue ends:

    1. Uncomment this coroutine
    2. Replace EndDialogue(); call in NextDialogue()
       with:
       StartCoroutine(EndDialogueWithFade());

    IEnumerator EndDialogueWithFade()
    {
        EndDialogue();

        yield return StartCoroutine(fadeManager.FadeOut());

        // Load next scene
        // Show result screen
        // Start gameplay
    }
    */
}