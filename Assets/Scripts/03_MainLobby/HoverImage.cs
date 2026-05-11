using UnityEngine;
using UnityEngine.EventSystems;

public class HoverImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject targetImage;

    void Start()
    {
        targetImage.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetImage.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetImage.SetActive(false);
    }
}