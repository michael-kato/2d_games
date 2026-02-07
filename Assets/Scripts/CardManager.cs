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
    private Image _imageComponent;
    public Sprite mysterySprite;
    public Sprite revealSprite;
    private bool _isRevealed;
    private Animator _animator;
    private Image _cellImage;

    [SerializeField] public List<Sprite> frames;
    public float framesPerSecond = 12f;
    private float _timer;
    private int _index;

    void Awake()
    {
    
    _imageComponent = GetComponent<Image>();
    _animator = GetComponent<Animator>();
    float randomOffset = Random.Range(0f, 1f);
    _animator.Play("card_idle", 0, randomOffset);
        
    //_lootDrop.GetComponent<SpriteRenderer>().sprite = backSprite;
    _cellImage = GetComponentInParent<Image>();

    }

    void Start()
    {
    }
    
    void Update()
    {
        if (_isRevealed) return;
        
        _timer += Time.deltaTime;
        if (_timer >= 1f / framesPerSecond)
        {
            _timer -= 1f / framesPerSecond;
            _index = (_index + 1) % frames.Count; // Loop back to start
            _imageComponent.sprite = frames[_index];
        }
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
        if (_isRevealed)
        {
            _imageComponent.sprite = revealSprite;
        }
        else
        {
            _isRevealed = false;

        }
    }
    
    public void Reset()
    {
        //StopAllCoroutines(); 
        StartCoroutine(DoReset());
    }

    IEnumerator DoReset()
    {
        _isRevealed = false; 
        yield return new WaitForSeconds(0.7f);
        
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
        yield return new WaitForSeconds(0.4f);
        
        var vfx = GetComponent<ParticleSystem>();
        vfx.Play();
        yield return new WaitForSeconds(0.3f);
        
        // disable visual
        _imageComponent.sprite = null;
        _imageComponent.material = null;
        _imageComponent.color = new Color(0,0,0,0);
        _cellImage.sprite = null;
        _cellImage.material = null;
        _cellImage.color = new Color(0,0,0,0);
        this.enabled = false;
        
        // drop loot!
        lootDrop.SetActive(true);
    }
}
