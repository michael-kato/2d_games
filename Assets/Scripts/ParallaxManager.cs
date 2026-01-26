using System.Collections.Generic;
using UnityEngine;

public class MouseParallax : MonoBehaviour
{
    [Header("Settings")]
    public float amount = 0.1f; // How much it moves
    public float smoothSpeed = 2f; // How "heavy" the movement feels
    public GameObject bg1;
    public GameObject bg2;
    public GameObject bg3;
    
    private Transform _bg1;
    private Transform _bg2;
    private Transform _bg3;
    private List<Transform> _updates;
    
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position; // ??
        _bg1 = bg1.transform;
        _bg2 = bg2.transform;
        _bg3 = bg3.transform;
        _updates = new List<Transform>() {_bg1, _bg2, _bg3};
    }

    void Update()
    {
        // 1. Get Mouse Offset from the center of the screen
        Vector3 mousePos = Input.mousePosition;
        float xOffset = (mousePos.x - (Screen.width / 2)) / Screen.width;
        float yOffset = (mousePos.y - (Screen.height / 2)) / Screen.height;

        for (int i = 0; i < _updates.Count; i++)
        {
            Transform t = _updates[i];
            float a = amount + 0.03f * i;
            
            // 2. Calculate the new target position
            Vector3 targetPos = startPos - new Vector3(xOffset * a, yOffset * a, 0);

            // 3. Smoothly move (Lerp) to the target
            t.position = Vector3.Lerp(t.position, targetPos, Time.deltaTime * smoothSpeed);
        }

    }
}