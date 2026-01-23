using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    
    private bool _isSelected;
    float speed = 0.2f;
    private bool _isRotating;
    private Sprite _sprite;
    public Sprite frontSprite;
    public Sprite backSprite;
    
    void Start()
    {
        _sprite = GetComponent<SpriteRenderer>().sprite;
    }
    
    void Update()
    {
        
    }
    
    void OnMouseDown()
    {
        if (!_isSelected)
        {
            _isSelected = true;
            StartCoroutine(FlipCard(180));
            GameLogic.CheckGuess(this.GameObject());
        }
    }
    public void Reset()
    {
        // Stop existing flips to prevent the "_isRotating" lock
        StopAllCoroutines(); 
        _isRotating = false;
        StartCoroutine(DoReset());
    }

    IEnumerator DoReset()
    {
        _isSelected = false; 
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(FlipCard(180)); 
    }

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
            float dot = Vector3.Dot(transform.forward, Camera.main.transform.forward);
            if (dot > 0) {
                _sprite = frontSprite;
            } else {
                _sprite = backSprite;
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
        yield return new WaitForSeconds(0.3f);
    
        Destroy(gameObject); 
    }
}
