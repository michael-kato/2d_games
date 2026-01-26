using UnityEngine;

public class TentacleEater : MonoBehaviour
{
    [Header("Settings")]
    public float swallowDistance = 0.1f; // Distance to "mouth" before it disappears
    public float grabStrength = 5f;     // How fast it pulls the food in
    public string foodTag = "Prize";

    private Transform currentFood;
    private TentacleController _tentacleController; // Reference to your existing movement script

    void Start()
    {
        _tentacleController = GetComponentInParent<TentacleController>();
    }

    void Update()
    {
        if (currentFood != null)
        {
            // 1. Move the food precisely to the IK tip
            currentFood.position = Vector3.Lerp(currentFood.position, transform.position, Time.deltaTime * grabStrength);

            // 2. Check if the food has reached the "mouth" (the anchor point)
            float distToAnchor = Vector3.Distance(currentFood.position, _tentacleController.ikTarget.position);
            
            if (distToAnchor < swallowDistance)
            {
                Swallow(currentFood.gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only grab if we aren't already eating and it's the right kind of object
        if (currentFood == null && collision.CompareTag(foodTag))
        {
            GrabFood(collision.transform);
        }
    }

    void GrabFood(Transform food)
    {
        currentFood = food;

        // Disable physics on the tile so it doesn't fight the tentacle
        if (food.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.simulated = false;
        }

        // Optional: Force the tentacle into "Retract" mode
        // This makes the tentacle pull back toward the wall once it has food
        _tentacleController.enabled = false; 
    }

    void Swallow(GameObject food)
    {
        Destroy(food);
        currentFood = null;
        
        // Re-enable the random wandering logic
        _tentacleController.enabled = true;

        // Trigger a "Chomp" VFX or Screen Shake here!
        Debug.Log("Nom!");
    }
}