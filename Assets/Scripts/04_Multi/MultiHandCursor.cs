using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MultiHandCursor : MonoBehaviour
{
    [Header("Hand Cursor")]
    public Transform handCursorTransform;
    public SpriteRenderer handSpriteRenderer;

    [Header("Hand Sprites")]
    public Sprite bareHandSprite;        // 평소 맨손
    public Sprite blackStoneHandSprite;  // 흑돌 들고 있는 손
    public Sprite whiteStoneHandSprite;  // 백돌 들고 있는 손
    public Sprite buttonPressHandSprite; // 버튼 클릭 순간 손

    [Header("Panels")]
    public GameObject settingPanel;      // 설정 팝업 패널
    public GameObject resultPopupPanel;  // 승패 결과 패널

    [Header("Position")]
    public Vector3 handOffset = Vector3.zero;
    public float handZPosition = -5f;

    [Header("Button Press")]
    public float buttonPressDuration = 0.12f;

    private const string PROP_BLACK_ACTOR = "blackActor";
    private const string PROP_WHITE_ACTOR = "whiteActor";
    private const string PROP_GAME_STARTED = "gameStarted";
    private const string PROP_CURRENT_TURN = "currentTurn";

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private float buttonPressEndTime = 0f;

    private void OnEnable()
    {
        Cursor.visible = false;

        if (handSpriteRenderer != null)
            handSpriteRenderer.enabled = true;
    }

    private void OnDisable()
    {
        Cursor.visible = true;

        if (handSpriteRenderer != null)
            handSpriteRenderer.enabled = false;
    }

    private void Update()
    {
        UpdateHandPosition();
        UpdateButtonPressState();
        UpdateHandSprite();
    }

    private void UpdateHandPosition()
    {
        if (Camera.main == null)
            return;

        if (handCursorTransform == null)
            return;

        bool isOutsideScreen =
            Input.mousePosition.x <= 0 ||
            Input.mousePosition.x >= Screen.width ||
            Input.mousePosition.y <= 0 ||
            Input.mousePosition.y >= Screen.height;

        if (isOutsideScreen)
        {
            Cursor.visible = true;

            if (handSpriteRenderer != null)
                handSpriteRenderer.enabled = false;

            return;
        }

        Cursor.visible = false;

        if (handSpriteRenderer != null)
            handSpriteRenderer.enabled = true;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = handZPosition;

        handCursorTransform.position = mouseWorldPos + handOffset;
    }

    private void UpdateButtonPressState()
    {
        if (Input.GetMouseButtonDown(0) && IsPointerOverButton())
        {
            buttonPressEndTime = Time.unscaledTime + buttonPressDuration;
        }
    }

    private void UpdateHandSprite()
    {
        if (handSpriteRenderer == null)
            return;

        // 1. 버튼 클릭 순간에는 무조건 누르는 손
        if (Time.unscaledTime < buttonPressEndTime)
        {
            SetSprite(buttonPressHandSprite != null ? buttonPressHandSprite : bareHandSprite);
            return;
        }

        // 2. 설정 패널 또는 결과 패널이 켜져 있으면 맨손
        if (IsAnyPopupActive())
        {
            SetSprite(bareHandSprite);
            return;
        }

        // 3. 버튼 위에 올려져 있으면 맨손
        if (IsPointerOverButton())
        {
            SetSprite(bareHandSprite);
            return;
        }

        // 4. Photon 방 정보가 없으면 맨손
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            SetSprite(bareHandSprite);
            return;
        }

        StoneType myStone = GetMyStone();
        StoneType currentTurn = GetCurrentTurn();

        // 5. 게임 시작 전, 관전자, 내 차례 아님 → 맨손
        if (!IsGameStarted() || myStone == StoneType.Empty || myStone != currentTurn)
        {
            SetSprite(bareHandSprite);
            return;
        }

        // 6. 내 차례면 내 돌 색에 맞는 손
        if (myStone == StoneType.Black)
        {
            SetSprite(blackStoneHandSprite != null ? blackStoneHandSprite : bareHandSprite);
        }
        else if (myStone == StoneType.White)
        {
            SetSprite(whiteStoneHandSprite != null ? whiteStoneHandSprite : bareHandSprite);
        }
        else
        {
            SetSprite(bareHandSprite);
        }
    }

    private bool IsAnyPopupActive()
    {
        if (settingPanel != null && settingPanel.activeInHierarchy)
            return true;

        if (resultPopupPanel != null && resultPopupPanel.activeInHierarchy)
            return true;

        return false;
    }

    private bool IsPointerOverButton()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject obj = uiRaycastResults[i].gameObject;

            if (obj == null)
                continue;

            if (obj.GetComponentInParent<Button>() != null)
                return true;
        }

        return false;
    }

    private void SetSprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        if (handSpriteRenderer.sprite == sprite)
            return;

        handSpriteRenderer.sprite = sprite;
    }

    private StoneType GetMyStone()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return StoneType.Empty;

        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

        if (myActor == GetBlackActor())
            return StoneType.Black;

        if (myActor == GetWhiteActor())
            return StoneType.White;

        return StoneType.Empty;
    }

    private int GetBlackActor()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return -1;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_BLACK_ACTOR))
            return (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_BLACK_ACTOR];

        return -1;
    }

    private int GetWhiteActor()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return -1;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_WHITE_ACTOR))
            return (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_WHITE_ACTOR];

        return -1;
    }

    private bool IsGameStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_GAME_STARTED))
            return (bool)PhotonNetwork.CurrentRoom.CustomProperties[PROP_GAME_STARTED];

        return false;
    }

    private StoneType GetCurrentTurn()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return StoneType.Black;

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_CURRENT_TURN))
            return (StoneType)(int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_CURRENT_TURN];

        return StoneType.Black;
    }
}