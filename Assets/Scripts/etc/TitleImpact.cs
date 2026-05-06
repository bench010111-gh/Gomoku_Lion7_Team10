using UnityEngine;

public class TitleImpact : MonoBehaviour
{
    private bool hasLanded = false;
    public UIShake uiShake;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            hasLanded = true;

            // Camera shake
            CameraShake camShake = Camera.main.GetComponent<CameraShake>();
            if (camShake != null)
            {
                StartCoroutine(camShake.Shake(0.2f, 0.15f));
            }

            // UI shake
            if (uiShake != null)
            {
                StartCoroutine(uiShake.Shake(0.2f, 10f)); // magnitude is in UI pixels
            }
        }
    }
}