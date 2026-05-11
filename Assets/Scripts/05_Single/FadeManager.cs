// FadeManager.cs
using System.Collections;
using UnityEngine;

public class FadeManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [SerializeField] private float fadeSpeed = 2f;

    public IEnumerator FadeIn()
    {
        float time = 1f;

        while (time > 0f)
        {
            time -= Time.deltaTime * fadeSpeed;

            fadeCanvasGroup.alpha = time;

            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    public IEnumerator FadeOut()
    {
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * fadeSpeed;

            fadeCanvasGroup.alpha = time;

            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }
}