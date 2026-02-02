using System.Collections.Generic;
using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    [Header("Settings")]
    public float amount = 10f;
    public float layerDifference = 1f;
    public float smoothSpeed = 2f;
    public float maxClamp = 10f; // Maximum world units it can travel
    
    public List<Transform> layers;
    private List<Vector3> _startPositions;

    void Start()
    {
        _startPositions = new List<Vector3>();
        foreach (Transform t in layers)
        {
            _startPositions.Add(t.position);
        }
    }

    void Update()
    {
        Vector3 targetOffset = Vector3.zero;

        // 1. Check if mouse is inside the game window
        if (IsMouseInWindow())
        {
            // Get Mouse Offset (-0.5 to 0.5)
            float xOffset = (Input.mousePosition.x / Screen.width) - 0.5f;
            float yOffset = (Input.mousePosition.y / Screen.height) - 0.5f;

            // We calculate the raw offset before applying it to layers
            targetOffset = ClampParallax(new Vector3(xOffset, yOffset, 0));
        }
        // If mouse is out, targetOffset remains Vector3.zero (the center)

        for (int i = 0; i < layers.Count; i++)
        {
            float intensity = amount + (i * layerDifference);
            
            // 2. Apply intensity and Clamp the total movement
            Vector3 desiredMove =  ClampParallax(targetOffset * intensity);
            
            // 3. Target is Start + Clamped Offset
            Vector3 targetPos = _startPositions[i] + desiredMove;

            // 4. Smooth Move from CURRENT position to TARGET position
            layers[i].localPosition = Vector3.Lerp(layers[i].localPosition, targetPos, Time.deltaTime * smoothSpeed);
        }
    }

    private bool IsMouseInWindow()
    {
        Vector3 mp = Input.mousePosition;
        return mp.x >= 0 && mp.x <= Screen.width && mp.y >= 0 && mp.y <= Screen.height;
    }

    private Vector3 ClampParallax(Vector3 v)
    {
        v.x = Mathf.Clamp(v.x, -maxClamp, maxClamp);
        v.y = Mathf.Clamp(v.y, -maxClamp, maxClamp);
        return v;
    }
}