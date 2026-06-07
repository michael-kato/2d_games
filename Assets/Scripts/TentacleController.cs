using UnityEngine;

public class TentacleController : MonoBehaviour
{
    private Camera _mainCamera;
    private Vector3 _defaultPosition;

    [Header("IK Constraints")]
    public float maxReach; // The actual length of your tentacle sprite
    public Transform ikTarget;
    
    [Header("Behaviors")]
    public float speed = 0.5f;
    public float decisionInterval;

    [Range(0, 1)] public float mouseFollowChance = 0.03f;

    private Vector3 _currentDestination;
    private float _timer;
    private bool _isFollowingMouse;
    public bool canEat;

    public Transform tipBone; 
    public Transform secondToLastBone;

    void OnEnable() { GameManager.OnGameStarted += EnableHunting; }
    void OnDisable() { GameManager.OnGameStarted -= EnableHunting; }

    public void EnableHunting() { canEat = true; }
    public void DisableHunting() { canEat = false; }
    
    void Start()
    {
        _mainCamera = Camera.main;
        _defaultPosition = ikTarget.transform.position;
        _defaultPosition.y -= 0.3f;
        
        maxReach = Vector3.Distance(transform.position, ikTarget.transform.position) * 0.5f;
        
        // Offset the start timer so all tentacles don't think at once
        decisionInterval = Random.Range(2, 5);
        _timer = Random.Range(0, decisionInterval);
        PickNewBehavior();
        
        // Start from the root of the tentacle and find the deepest child
        tipBone = GetDeepestChild(transform);
    
        // The second-to-last bone is simply the tip's parent
        if (tipBone != null)
        {
            secondToLastBone = tipBone.parent;
        }
    }

    private Transform GetDeepestChild(Transform parent)
    {
        Transform lastChild = parent;
    
        // Keep diving down until we find a transform with no children
        while (lastChild.childCount > 0)
        {
            lastChild = lastChild.GetChild(0);
        }
    
        return lastChild;
    }

    void Update()
    {
        if (!canEat) return;
        
        _timer += Time.deltaTime;

        if (_timer >= decisionInterval)
        {
            PickNewBehavior();
            _timer = 0;
        }

        if (_isFollowingMouse)
        {
            _currentDestination = GetMouseWorldPos();
        }
        
        // don't let the tentacle reach past it's max length
        _currentDestination = ClampToRadius(_currentDestination);
        
        ikTarget.position = Vector3.Lerp(ikTarget.position, _currentDestination, Time.deltaTime * speed);
    }

    void PickNewBehavior()
    {
        // The "1/3 of the time" roll
        _isFollowingMouse = Random.value < mouseFollowChance;

        if (!_isFollowingMouse)
        {
            // Pick a random idle point
            _currentDestination = _defaultPosition + (Vector3)Random.insideUnitCircle;
        }
    }

    Vector3 ClampToRadius(Vector3 target)
    {
        Vector3 offset = target - _defaultPosition;
        return _defaultPosition + Vector3.ClampMagnitude(offset, maxReach);
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 p = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0;
        return p;
    }

    
    void LateUpdate() // Use LateUpdate so IK has finished calculating positions
    {
        Vector3 direction = (tipBone.position - secondToLastBone.position).normalized;

        // 3. For 2D: Point the Tip's 'Right' or 'Up' at the direction
        // Most 2D sprites point 'Right' (X-axis) by default
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        
        ikTarget.rotation = targetRotation;
    }
    
    
    
    // Visualize the reach in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_defaultPosition, maxReach);
    }
}