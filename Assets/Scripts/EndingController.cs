using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingController : MonoBehaviour
{
    [Header("Cthulhu")]
    [SerializeField] private GameObject cthulhuPrefab;
    [SerializeField] private Transform cthulhuParent;
    [SerializeField] private float riseDuration = 6f;
    [SerializeField] private float startYOffset = 4f;
    [SerializeField] private float endYOffset = 0f;
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = new Vector3(8f, 8f, 8f);

    [Header("Ending Fade")]
    [SerializeField] private Image blackFadeImage;
    [SerializeField] private TextMeshProUGUI theEndText;
    [SerializeField] private float fadeToBlackDuration = 2f;
    [SerializeField] private float textFadeDuration = 1.5f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    private bool _endingStarted;

    private void OnEnable()
    {
        GameManager.OnSummonCthulhu += StartEnding;
    }

    private void OnDisable()
    {
        GameManager.OnSummonCthulhu -= StartEnding;
    }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        SetImageAlpha(blackFadeImage, 0f);
        SetTextAlpha(theEndText, 0f);
    }

    private void StartEnding()
    {
        if (_endingStarted)
        {
            return;
        }

        _endingStarted = true;
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        GameObject cthulhu = SpawnCthulhuBelowScreen();

        yield return StartCoroutine(RiseAndScaleCthulhu(cthulhu.transform));

        yield return StartCoroutine(FadeImage(blackFadeImage, 0f, 1f, fadeToBlackDuration));

        yield return StartCoroutine(FadeText(theEndText, 0f, 1f, textFadeDuration));
    }

    private GameObject SpawnCthulhuBelowScreen()
    {
        Vector3 bottomCenter = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, Mathf.Abs(targetCamera.transform.position.z)));
        bottomCenter.z = 0f;
        bottomCenter.y -= startYOffset;

        GameObject cthulhu = Instantiate(cthulhuPrefab, bottomCenter, Quaternion.identity, cthulhuParent);
        cthulhu.transform.localScale = startScale;

        return cthulhu;
    }

    private IEnumerator RiseAndScaleCthulhu(Transform cthulhu)
    {
        Vector3 startPosition = cthulhu.position;

        Vector3 endPosition = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Abs(targetCamera.transform.position.z)));
        endPosition.z = startPosition.z;
        endPosition.y += endYOffset;

        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cthulhu.position = Vector3.Lerp(startPosition, endPosition, smoothT);
            cthulhu.localScale = Vector3.Lerp(startScale, endScale, smoothT);

            yield return null;
        }

        cthulhu.position = endPosition;
        cthulhu.localScale = endScale;
    }

    private IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        if (image == null)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetImageAlpha(image, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetImageAlpha(image, to);
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
    {
        if (text == null)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTextAlpha(text, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTextAlpha(text, to);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}
