using UnityEngine;

public class TentacleController : MonoBehaviour
{
    [Header("References")]
    public Transform ikTarget;


    [Header("Movement")]
    public float movementRadius = 4f;
    public float idleSpeed = 0.5f;
    public float followSpeed = 2.0f; // Faster when following mouse
    public float decisionInterval;

    [Header("Behaviors")]
    [Range(0, 1)] public float mouseFollowChance = 0.33f;

    private Vector3 currentDestination;
    private float timer;
    private bool isFollowingMouse = false;
    private Camera _mainCamera;
    
    private Vector3 _defaultPosition;

    void Start()
    {
        _mainCamera = Camera.main;
        _defaultPosition = ikTarget.transform.position;
        _defaultPosition.y -= 0.3f;
        
        // Offset the start timer so all tentacles don't think at once
        decisionInterval =- Random.Range(2, 5);
        timer = Random.Range(0, decisionInterval);
        PickNewBehavior();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= decisionInterval)
        {
            PickNewBehavior();
            timer = 0;
        }

        float activeSpeed = idleSpeed;

        if (isFollowingMouse)
        {
            // Update destination to mouse position
            currentDestination = GetMouseWorldPos();
            activeSpeed = followSpeed;
            
            // Constraint: Don't let it stretch too far from anchor
            currentDestination = ClampToRadius(currentDestination);
        }

        // Smoothly move the IK Handle
        ikTarget.position = Vector3.Lerp(ikTarget.position, currentDestination, Time.deltaTime * activeSpeed);
    }

    void PickNewBehavior()
    {
        // The "1/3 of the time" roll
        isFollowingMouse = Random.value < mouseFollowChance;

        if (!isFollowingMouse)
        {
            // Pick a random idle point
            Vector2 randomCircle = Random.insideUnitCircle * movementRadius;
            currentDestination = _defaultPosition + (Vector3)randomCircle;
        }
    }

    Vector3 ClampToRadius(Vector3 target)
    {
        Vector3 offset = target - _defaultPosition;
        return _defaultPosition + Vector3.ClampMagnitude(offset, movementRadius);
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 p = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0;
        return p;
    }

    // Visualize the reach in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_defaultPosition, movementRadius);
    }
}