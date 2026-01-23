using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    
    private bool _isSelected;
    float speed = 0.5f;
    private bool _isRotating;
    private SpriteRenderer _sr;
    public Sprite frontSprite;
    public Sprite backSprite;
    
    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
    }
    
    void Update()
    {
        
    }
    
    void OnMouseDown()
    {
        if (!_isSelected)
        {
            _isSelected = true;
            _isRotating = true;
            StartCoroutine(FlipCard(180));
        }
        else
        {
            // flip back
            _isSelected = false;
            _isRotating = true;
            StartCoroutine(FlipCard(-180));
        }
    }
    
    public IEnumerator FlipCard(float adjustment)
    {
        if (_isRotating)
        {
            yield return new WaitForEndOfFrame();
        }
        
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, adjustment, 0));
        float elapsed = 0.0f;
        while (elapsed < speed)
        {
            float t = elapsed / speed;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            elapsed += Time.deltaTime;
            
            // Check if the card is facing away from the camera
            float dot = Vector3.Dot(transform.forward, Camera.main.transform.forward);
    
            if (dot > 0) {
                _sr.sprite = frontSprite;
            } else {
                _sr.sprite= backSprite;
            }
            
            yield return new WaitForSeconds(0.01f);
        }
        
        _isRotating = false;
        
        transform.rotation = endRotation;
        yield return new WaitForEndOfFrame();
    }
    

    void AnimateCorrectGuess()
    {
        
    }
}
