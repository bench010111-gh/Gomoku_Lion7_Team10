using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    //씬 전환 버튼에 인스턴스 참조해서 사용바람 예시 -> SceneTransitionManager.Instance.ChangeScene("씬이름");
    private static SceneTransitionManager _instance;

    public static SceneTransitionManager Instance
    {
        get
        {
            if(_instance != null) return _instance;

            _instance = FindFirstObjectByType<SceneTransitionManager>();

            if (_instance == null)
            {
                Debug.LogWarning("SceneTransitionManager가 씬에 없습니다! 셔터 기능이 작동하지 않습니다.");
            }

            return _instance;
        }
    }

    private bool isTransitioning = false;

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
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if(_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isTransitioning)
        {
            if (doorPanel != null)
            {
                doorPanel.anchoredPosition = new Vector2(0, doorPanel.rect.height);
                doorPanel.gameObject.SetActive(false);
            }
            return;
        }

        isTransitioning = false;
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
        if (isTransitioning) return;

        isTransitioning = true;
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
