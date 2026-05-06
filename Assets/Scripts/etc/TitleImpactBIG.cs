using System.Collections;
using UnityEngine;

public class TitleImpactBIG : MonoBehaviour
{
    private bool hasLanded = false;
    public UIShake uiShake;
    public AudioClip boomSFX;
    public AudioClip mainBGM;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            hasLanded = true;

            // BOOM
            AudioManager.Instance.PlaySFX(boomSFX);

            // 1초 후 BGM 시작
            AudioManager.Instance.PlayBGM(mainBGM, 1.5f);

            // Camera shake
            CameraShake camShake = Camera.main.GetComponent<CameraShake>();
            if (camShake != null)
            {
                StartCoroutine(camShake.Shake(0.2f, 0.15f));
            }

            // UI shake
            if (uiShake != null)
            {
                StartCoroutine(uiShake.Shake(0.2f, 10f));
            }
        }
    }

    IEnumerator PlayBGMDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.PlayBGM(mainBGM);
    }
}