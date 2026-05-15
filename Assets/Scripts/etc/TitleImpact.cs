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

            // BOOM SFX
            AudioManager.Instance.PlaySFX(boomSFX);

            // DELAYED BGM
            StartCoroutine(PlayBGMDelayed(0.1f));

            // CAMERA SHAKE
            CameraShake camShake = Camera.main.GetComponent<CameraShake>();

            if (camShake != null)
            {
                StartCoroutine(camShake.Shake(0.2f, 0.15f));
            }

            // UI SHAKE
            if (uiShake != null)
            {
                StartCoroutine(uiShake.Shake(0.2f, 10f));
            }
        }
    }

    void OnDestroy()
    {
        // ¶¥¿¡ ´ê±âµµ Àü¿¡ ¾ÀÀ» ½ºÅµÇØ¹ö·È´Ù¸é? -> °­Á¦·Î BGM Àç»ý!
        if (!hasLanded && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(mainBGM);
        }
    }

    IEnumerator PlayBGMDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        AudioManager.Instance.PlayBGM(mainBGM);
    }
}