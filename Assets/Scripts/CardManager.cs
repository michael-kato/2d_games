using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardManager : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] GameObject lootDrop;
    private bool _isSelected;
    float speed = 0.2f;
    private bool _isRotating;
    private Image _imageComponent;
    public Sprite frontSprite;
    public Sprite backSprite;
    private Transform _cameraTransform;
    private Animator _animator;
    
    
    void Start()
    {
        _imageComponent = GetComponent<Image>();
        _cameraTransform = Camera.main.transform;
        _animator = GetComponent<Animator>();
        
        float randomOffset = Random.Range(0f, 1f);
        _animator.Play("card_idle", 0, randomOffset);
    }
    
    void Update()
    {
        
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isSelected)
        {
            _isSelected = true;
            _animator.SetTrigger("OnClick");
            GameLogic.CheckGuess(this.GameObject());
        }
    }
    
    public void SwapSprite() {
        _imageComponent.sprite = _isSelected ? backSprite : frontSprite;
    }
    
    public void Reset()
    {
        //StopAllCoroutines(); 
        StartCoroutine(DoReset());
    }

    IEnumerator DoReset()
    {
        _isSelected = false; 
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
        this.enabled = false;
        
        // drop loot!
        lootDrop.SetActive(true);
    }
}
