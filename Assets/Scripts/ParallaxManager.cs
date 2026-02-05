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

        if (IsMouseInWindow())
        {
            // Get Mouse Offset
            float xOffset = (Input.mousePosition.x / Screen.width) - 0.5f;
            float yOffset = (Input.mousePosition.y / Screen.height) - 0.5f;

            targetOffset = ClampParallax(new Vector3(xOffset, yOffset, 0));
        }

        for (int i = 0; i < layers.Count; i++)
        {
            float intensity = amount + (i * layerDifference);
            Vector3 desiredMove =  ClampParallax(targetOffset * intensity);
            Vector3 targetPos = _startPositions[i] + desiredMove;

            var t = layers[i].localPosition;
            targetPos.z = t.z; // preserve the z-offset

            layers[i].localPosition = Vector3.Lerp(t, targetPos, Time.deltaTime * smoothSpeed);
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