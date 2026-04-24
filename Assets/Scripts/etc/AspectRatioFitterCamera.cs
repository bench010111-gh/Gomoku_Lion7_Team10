using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioFitterCamera : MonoBehaviour
{
    public float targetAspect = 16f / 9f; // 1920:1080

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyAspect();
    }

    void Update()
    {
        ApplyAspect();
    }

    private void ApplyAspect()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            // 창이 더 세로로 김 -> 위아래는 꽉 차고 좌우에 여백
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        else
        {
            // 창이 더 가로로 김 -> 좌우는 꽉 차고 위아래에 여백
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
    }
}