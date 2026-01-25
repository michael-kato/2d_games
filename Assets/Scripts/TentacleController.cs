using UnityEngine;

public class TentacleAmbience : MonoBehaviour
{
    [Header("References")]
    public Transform ikTarget;
    private Vector3 anchorPoint;

    [Header("Movement Settings")]
    public float movementRadius = 10f;    // How far from the anchor it can reach
    public float moveSpeed = 0.1f;       // Slow, organic speed
    public float decisionInterval;  // Change targets every 5 seconds

    [Header("Perlin Wiggle")]
    public float wiggleAmount = 0.3f;
    public float wiggleSpeed = 1.0f;

    private Vector3 currentDestination;
    private float timer;
    private Vector2 noiseOffset;

    void Start()
    {
        decisionInterval = Random.Range(2f, 10f);
        noiseOffset = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));
        PickNewRandomPoint();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= decisionInterval)
        {
            PickNewRandomPoint();
            timer = 0;
        }

        // 1. Calculate the Perlin Wiggle for micro-movement
        float noiseX = Mathf.PerlinNoise(Time.time * wiggleSpeed + noiseOffset.x, 0) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0, Time.time * wiggleSpeed + noiseOffset.y) - 0.5f;
        Vector3 wiggle = new Vector3(noiseX, noiseY, 0) * wiggleAmount;

        // 2. Smoothly move the IK Target toward the destination
        // Using Lerp with a slow speed gives that "drifting" feel
        ikTarget.position = Vector3.Lerp(ikTarget.position, currentDestination + wiggle, Time.deltaTime * moveSpeed);
    }

    void PickNewRandomPoint()
    {
        // Pick a random direction within a hemisphere (facing away from the screen edge)
        Vector2 randomCircle = Random.insideUnitCircle * movementRadius;
        
        // We add this to the anchorPoint.position so the tentacle stays 
        // tethered to its specific screen-edge location.
        currentDestination = anchorPoint + (Vector3)randomCircle;
    }
}