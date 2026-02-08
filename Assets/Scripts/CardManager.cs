using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardManager : MonoBehaviour, IPointerClickHandler
{
    public GameObject lootDrop;
    private bool _isRotating;
    private CanvasGroup _canvasGroup;
    public Sprite mysterySprite;
    public Sprite revealSprite;
    private bool _isRevealed;
    private Animator _animator;

    private Image _image;
    private Image _cellImage;
    
    [SerializeField] public List<Sprite> frames;
    private float _timer;
    private int _index;

    void Awake()
    {
        _image = GetComponent<Image>();
        _cellImage = transform.parent.GetComponent<Image>();
        _canvasGroup = transform.parent.GetComponent<CanvasGroup>();
        
        _animator = GetComponent<Animator>();
        float randomOffset = Random.Range(0f, 1f);
        _animator.Play("card_idle", 0, randomOffset);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isRevealed)
        {
            _isRevealed = true;
            _animator.SetTrigger("OnClick");
            GameManager.CheckGuess(this.GameObject());
        }
    }
    
    public void SwapSprite()
    {
        Debug.Log($"{name} - SwapSprite called. _isRevealed={_isRevealed}, sprite changing to: {(_isRevealed ? revealSprite.name : mysterySprite.name)}");
        _image.sprite = _isRevealed ? revealSprite : mysterySprite;
    }
    
    public void Reset()
    {
        StartCoroutine(DoReset());
    }

    IEnumerator DoReset()
    {
        // small wait to give the player a chance to remember the cards
        yield return new WaitForSeconds(0.7f);
        
        _isRevealed = false; 
        _animator.SetFloat("FlipSpeed", -1);
        _animator.Play("card_flip", 0, 1.0f);
        yield return new WaitForSeconds(0.33f);
        _animator.SetFloat("FlipSpeed", 1);
        
        _animator.SetTrigger("Reset");
    }

    public void Vaporize()
    {
        StartCoroutine(DoVaporize());
    }

    IEnumerator DoVaporize()
    {
        float elapsed = 0;
        float duration = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Lerp(1, 0, elapsed / duration);
            
            _cellImage.color = new Color(1, 1, 1, progress);
            yield return null;
        }
        
        var vfx = GetComponent<ParticleSystem>();
        vfx.Play();
        yield return new WaitForSeconds(0.2f);
        
        // disable visual
        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        
        this.enabled = false;
        
        // drop loot!
        lootDrop.SetActive(true);
    }
}
