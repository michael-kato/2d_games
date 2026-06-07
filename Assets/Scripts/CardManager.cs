using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class CardManager : MonoBehaviour, IPointerClickHandler
{
    public GameObject lootDrop;
    private bool _isRotating;
    private CanvasGroup _canvasGroup;
    public Sprite mysterySprite;
    public Sprite revealSprite;
    private bool _isRevealed;
    private Image _image;
    private Image _cellImage;
    private Light2D _light;
    
    [SerializeField] public List<Sprite> frames;
    [SerializeField] public float frameRate = 12f;
    [SerializeField] public float flipDuration = 0.33f;
    private Coroutine _animationCoroutine;

    void Awake()
    {
        _image = GetComponent<Image>();
        // TODO: should probably move this script to the Cell instead...
        _cellImage = transform.parent.GetComponent<Image>();
        _canvasGroup = transform.parent.GetComponent<CanvasGroup>();
        _light = transform.parent.GetComponent<Light2D>();
        
        _image.sprite = mysterySprite;
        transform.localEulerAngles = Vector3.zero;
        transform.localScale = Vector3.one;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isRevealed)
        {
            _isRevealed = true;
            PlayFlipAnimation(false);
            GameManager.CheckGuess(this.GameObject());
        }
    }
    
    public void SwapSprite()
    {
        _image.sprite = _isRevealed ? revealSprite : mysterySprite;
    }
    
    public void Reset()
    {
        StartCoroutine(DoReset());
    }

    private void PlayFlipAnimation(bool reverse)
    {
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(AnimateFlip(reverse));
    }

    IEnumerator AnimateFlip(bool reverse)
    {
        float elapsed = 0;
        bool swapped = false;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flipDuration;
            if (reverse) t = 1 - t;

            // Rotation
            float yRotation = Mathf.Lerp(0, 180, t);
            transform.localEulerAngles = new Vector3(0, yRotation, 0);

            // Scale (pulse effect)
            float scale = 1f;
            if (t < 0.25f) scale = Mathf.Lerp(1, 1.1f, t / 0.25f);
            else scale = Mathf.Lerp(1.1f, 1, (t - 0.25f) / 0.75f);
            transform.localScale = new Vector3(scale, scale, 1);

            // Swap sprite at midpoint
            if (!swapped && t >= 0.65f) // 0.216 / 0.333 is approx 0.65
            {
                swapped = true;
                SwapSprite();
            }

            yield return null;
        }

        // Final state
        float finalT = reverse ? 0 : 1;
        transform.localEulerAngles = new Vector3(0, Mathf.Lerp(0, 180, finalT), 0);
        transform.localScale = Vector3.one;
        if (!swapped) SwapSprite();
    }

    IEnumerator DoReset()
    {
        // small wait to give the player a chance to remember the cards
        yield return new WaitForSeconds(0.7f);
        
        _isRevealed = false; 
        PlayFlipAnimation(true);
        yield return new WaitForSeconds(flipDuration);
        
        _image.sprite = mysterySprite;
    }

    public void Vaporize()
    {
        StartCoroutine(DoVaporize());
    }

    IEnumerator DoVaporize()
    {
        _light.enabled = true;
        
        float elapsed = 0;
        float duration = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Lerp(1, 0, elapsed / duration);
            
            _cellImage.color = new Color(1, 1, 1, progress);
            _light.intensity = 1 - progress;
            yield return null;
        }
        
        var vfx = GetComponent<ParticleSystem>();
        vfx.Play();
        CameraShake.Instance.AddTrauma(5);
        
        yield return new WaitForSeconds(0.2f);
        
        _light.enabled = false;
        
        // disable visual
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        
        this.enabled = false;
        
        // drop loot!
        lootDrop.SetActive(true);
    }
}
