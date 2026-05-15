using UnityEngine;
using System.Collections;

public class UIShake : MonoBehaviour
{
    public IEnumerator Shake(float duration, float magnitude)
    {
        RectTransform rect = GetComponent<RectTransform>();
        Vector3 originalPos = rect.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            rect.anchoredPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime; // works even if time is paused
            yield return null;
        }

        rect.anchoredPosition = originalPos;
    }
}