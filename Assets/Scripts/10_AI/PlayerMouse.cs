using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PlayerMouse : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas parentCanvas; 
    private Coroutine punchCoroutine;

    [Header("PlayerHand")]
    [SerializeField] private Image handImage; 

    [Header("Punch Settings")]
    [SerializeField] private float punchScaleStrength = 1.2f; 
    [SerializeField] private float punchDuration = 0.2f;

    [Header("Hand Sprites")]
    [SerializeField] Sprite baseHand;
    [SerializeField] Sprite blackStoneHand;
    [SerializeField] Sprite whiteStoneHand;

    private Sprite storedStoneSprite; 
    private bool isHoldingStone;
    public bool IsHoldingStone => isHoldingStone;

    bool isPlayingEffect = false; 

    private void Awake()
    {
        Cursor.visible = false;

        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogError("PlayerMouse 스크립트가 붙은 오브젝트는 Canvas의 자식이어야 합니다!");
            enabled = false;
        }
    }

    private void Start()
    {
        handImage.sprite = baseHand;
        isHoldingStone = false;
    }

    void Update()
    {
        if (parentCanvas != null)
        {
            Vector2 screenPos = Input.mousePosition;
            Vector2 localPoint;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPos,
                parentCanvas.worldCamera, 
                out localPoint))
            {
                rectTransform.anchoredPosition = localPoint;
            }
        }

        if (Input.GetMouseButtonDown(0))
            PlayClickEffect(); 
    }

    public void SetStoneType(StoneType stoneType)
    {
        storedStoneSprite = stoneType == StoneType.Black ? blackStoneHand : whiteStoneHand; 
    }

    public void PickupStone()
    {
        isHoldingStone = true;
        handImage.sprite = storedStoneSprite;
    }
    public void DropStone()
    {
        if (!IsHoldingStone)
            return;

        isHoldingStone = false;
        handImage.sprite = baseHand;
    }

    public void PlayClickEffect()
    {
        if (isPlayingEffect)
            return; 

        if (punchCoroutine != null)
            StopCoroutine(punchCoroutine);

        isPlayingEffect = true;
        punchCoroutine = StartCoroutine(PlayStoneClickEffect());
    }
    public IEnumerator PlayStoneClickEffect()
    {
        Vector3 strength = Vector3.one * (punchScaleStrength - 1f); 
        yield return this.rectTransform.DOPunchScale(strength, punchDuration, 10, 1f).WaitForCompletion();

        isPlayingEffect = false; 
        punchCoroutine = null;
    }
    
    public void OnActiveCursor()
    {
        Cursor.visible = true; 
    }
}