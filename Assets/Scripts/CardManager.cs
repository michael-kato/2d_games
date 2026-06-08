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
    
    [SerializeField] public AnimationClip flipClip;
    [SerializeField] public AnimationClip flipHardClip;
    [SerializeField] public float frameRate = 12f;
    [SerializeField] public float flipDuration = 0.33f;
    [SerializeField] public float idleAnimationSpeed = 1f;
    [SerializeField] public float idleAnimationIntensity = 5f;
    private Coroutine _animationCoroutine;
    private Coroutine _idleCoroutine;

    void Awake()
    {
        _image = GetComponent<Image>();
        // TODO: should probably move this script to the Cell instead...
        _cellImage = transform.parent.GetComponent<Image>();
        _canvasGroup = transform.parent.GetComponent<CanvasGroup>();
        _light = transform.parent.GetComponent<Light2D>();
    }

    void Start()
    {
        _image.sprite = mysterySprite;
        transform.localEulerAngles = Vector3.zero;
        transform.localScale = Vector3.one;
        
        _idleCoroutine = StartCoroutine(AnimateIdle());
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isRevealed)
        {
            _isRevealed = true;
            bool isMatch = GameManager.IsNextGuessMatch(this.gameObject);
            PlayFlipAnimation(false, isMatch);
            AudioManager.Instance?.PlayFlip();
            GameManager.CheckGuess(this.gameObject);
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

    private void PlayFlipAnimation(bool reverse, bool hard = false)
    {
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(AnimateFlip(reverse, hard));
    }

    IEnumerator AnimateIdle()
    {
        // Random offset so they don't all pulse at once
        yield return new WaitForSeconds(Random.Range(0f, 1f));

        float elapsed = 0;
        while (true)
        {
            if (!_isRotating)
            {
                elapsed += Time.deltaTime * idleAnimationSpeed;
                float tilt = Mathf.Sin(elapsed) * idleAnimationIntensity;
                transform.localEulerAngles = new Vector3(0, _isRevealed ? 180 : 0, tilt);
                
                // Also a subtle scale pulse
                float pulse = 1.0f + Mathf.Sin(elapsed * 2f) * 0.02f;
                transform.localScale = new Vector3(pulse, pulse, 1);
            }
            yield return null;
        }
    }

    IEnumerator AnimateOomph()
    {
        float elapsed = 0;
        float duration = 0.5f;
        Vector3 initialScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // A quick punch out and back in
            float punch = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            transform.localScale = initialScale * punch;
            
            yield return null;
        }
        transform.localScale = initialScale;
    }

    IEnumerator AnimateFlip(bool reverse, bool hard = false)
    {
        _isRotating = true;
        float elapsed = 0;
        bool swapped = false;

        AnimationClip clip = hard ? flipHardClip : flipClip;
        if (clip == null) clip = flipClip; // fallback
        
        float duration = clip != null ? clip.length : flipDuration;

        // Get the event time from the clip if it exists
        float eventTime = 0.5f; // fallback
        if (clip != null && clip.events.Length > 0)
        {
            foreach (var ev in clip.events)
            {
                if (ev.functionName == "SwapSprite")
                {
                    eventTime = ev.time / clip.length;
                    break;
                }
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Rotation
            float lerpT = reverse ? 1 - t : t;
            
            float yRotation;
            if (hard && !reverse)
            {
                // Replicate the 0 -> 180 -> 170 -> 180 overshoot from the anim
                if (lerpT < 0.77f) // 0.333 / 0.433
                {
                    yRotation = Mathf.Lerp(0, 180, lerpT / 0.77f);
                }
                else
                {
                    float overshootT = (lerpT - 0.77f) / 0.23f;
                    if (overshootT < 0.5f) yRotation = Mathf.Lerp(180, 170, overshootT * 2);
                    else yRotation = Mathf.Lerp(170, 180, (overshootT - 0.5f) * 2);
                }
            }
            else
            {
                yRotation = Mathf.Lerp(0, 180, lerpT);
            }

            float tilt = transform.localEulerAngles.z;
            transform.localEulerAngles = new Vector3(0, yRotation, tilt);

            // Scale (pulse effect)
            float scale = 1f;
            if (lerpT < 0.25f) scale = Mathf.Lerp(1, 1.1f, lerpT / 0.25f);
            else scale = Mathf.Lerp(1.1f, 1, (lerpT - 0.25f) / 0.75f);
            transform.localScale = new Vector3(scale, scale, 1);

            // Swap sprite based on animation event time
            // When reversing, we want to swap at the same visual point
            if (!swapped && (reverse ? t >= (1 - eventTime) : t >= eventTime))
            {
                swapped = true;
                SwapSprite();
            }

            yield return null;
        }

        // Final state
        float finalY = reverse ? 0 : 180;
        transform.localEulerAngles = new Vector3(0, finalY, 0);
        transform.localScale = Vector3.one;
        if (!swapped) SwapSprite();
        
        _isRotating = false;
    }

    IEnumerator DoReset()
    {
        // small wait to give the player a chance to remember the cards
        yield return new WaitForSeconds(0.7f);
        
        _isRevealed = false; 
        PlayFlipAnimation(true);
        yield return new WaitForSeconds(flipDuration);
    }

    public void Vaporize()
    {
        StartCoroutine(DoVaporize());
    }

    IEnumerator DoVaporize()
    {
        yield return StartCoroutine(AnimateOomph());
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
