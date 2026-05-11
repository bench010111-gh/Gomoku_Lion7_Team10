using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    //씬 전환 버튼에 인스턴스 참조해서 사용바람 예시 -> SceneTransitionManager.Instance.ChangeScene("씬이름");
    public static SceneTransitionManager Instance;

    [Header("Transition UI")]
    public Canvas transitionCanvas;
    public RectTransform doorPanel;

    [Header("Animation Settings")]
    public float doorAnimSpeed = 0.5f;
    public float bounceDuration = 0.2f;
    public float bounceHeight = 30f;
    public float bounceSpeed = 40f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(OpenDoorRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✨✨ 1. 입장 연출 (새로운 씬이 열릴 때마다 자동 실행)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        StartCoroutine(OpenDoorRoutine());
    }

    private IEnumerator OpenDoorRoutine()
    {
        if (doorPanel == null) yield break;

        transitionCanvas.sortingOrder = 999;
        doorPanel.gameObject.SetActive(true);

        Vector2 startPos = Vector2.zero;
        Vector2 endPos = new Vector2(0, doorPanel.rect.height);

        doorPanel.anchoredPosition = startPos;

        float timer = 0f;
        while (timer < doorAnimSpeed)
        {
            timer += Time.deltaTime;
            float t = timer / doorAnimSpeed;
            t = t * t;
            doorPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        doorPanel.anchoredPosition = endPos;
        doorPanel.gameObject.SetActive(false);
    }

    public void ChangeScene(string sceneName)
    {
        StartCoroutine(CloseDoorAndGoRoutine(sceneName));
    }

    private IEnumerator CloseDoorAndGoRoutine(string sceneName)
    {
        transitionCanvas.sortingOrder = 999;

        doorPanel.gameObject.SetActive(true);
        Vector2 startPos = new Vector2(0, doorPanel.rect.height);
        Vector2 endPos = Vector2.zero;
        doorPanel.anchoredPosition = startPos;

        bool hasPlayedSound = false;
        float soundOffset = 0.08f;
        float timer = 0f;

        while (timer < doorAnimSpeed)
        {
            timer += Time.deltaTime;
            float t = timer / doorAnimSpeed;
            t = t * t;
            doorPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            if (!hasPlayedSound && timer >= (doorAnimSpeed - soundOffset))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayShutterSound();
                hasPlayedSound = true;
            }
            yield return null;
        }

        if (!hasPlayedSound && AudioManager.Instance != null) AudioManager.Instance.PlayShutterSound();

        float shakeTimer = 0f;
        while (shakeTimer < bounceDuration)
        {
            shakeTimer += Time.deltaTime;
            float damping = 1.0f - (shakeTimer / bounceDuration);
            float bounceY = Mathf.Sin(shakeTimer * bounceSpeed) * bounceHeight * damping;
            doorPanel.anchoredPosition = endPos + new Vector2(0, bounceY);
            yield return null;
        }

        doorPanel.anchoredPosition = endPos;
        yield return new WaitForSeconds(0.75f);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(sceneName);

        
    }
}
