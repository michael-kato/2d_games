using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardManager : MonoBehaviour, IPointerClickHandler
{
    
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
    public void Reset()
    {
        // Stop existing flips to prevent the "_isRotating" lock
        StopAllCoroutines(); 
        StartCoroutine(DoReset());
    }

    IEnumerator DoReset()
    {
        _isSelected = false; 
        yield return new WaitForSeconds(0.3f);
        _animator.SetTrigger("OnClick");
    }

    // old! 
    public IEnumerator FlipCard(float adjustment)
    {
        _isRotating = true;
    
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, adjustment, 0));
    
        float elapsed = 0.0f;
        while (elapsed < speed)
        {
            float t = elapsed / speed;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
        
            // swap sprites on flip
            float dot = Vector3.Dot(transform.forward, _cameraTransform.forward);
            if (dot > 0) {
                _imageComponent.sprite = frontSprite;
            } else {
                _imageComponent.sprite = backSprite;
            }

            elapsed += Time.deltaTime;
            yield return null; 
        }

        transform.rotation = endRotation;
        _isRotating = false;
    }

    public void Vaporize()
    {
        StartCoroutine(DoVaporize());
    }

    IEnumerator DoVaporize()
    {
        yield return new WaitForSeconds(0.2f);
        
        var vfx = GetComponent<ParticleSystem>();
        vfx.Play();
        yield return new WaitForSeconds(0.3f);
        _imageComponent.sprite = null;
        _imageComponent.material = null;
        _imageComponent.color = new Color(0,0,0,0);
        this.enabled = false;
    }
}
