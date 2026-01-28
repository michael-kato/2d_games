using System.Collections;
using UnityEngine;

public class TentacleEater : MonoBehaviour
{
    public float swallowDistance = 0.01f; // Distance to "mouth" before it disappears
    public float grabStrength = 1.0f;
    public string foodTag = "Loot";

    private Transform currentFood;
    private TentacleController _tentacleController;
    
    private WaitForSeconds slowDownDelay = new WaitForSeconds(0.2f);
    private WaitForSeconds eatDelay = new WaitForSeconds(2f);
    void Start()
    {
        _tentacleController = GetComponentInParent<TentacleController>();
    }

    void Update()
    {
        if (currentFood != null)
        {
            // snatch up the food
            _tentacleController.ikTarget.position = Vector3.Lerp(_tentacleController.ikTarget.position, currentFood.position, Time.deltaTime * grabStrength);
            
            float distToMouth = Vector3.Distance(currentFood.position, _tentacleController.ikTarget.position);
            if (distToMouth < swallowDistance)
            {
                StartCoroutine(Swallow(currentFood.gameObject));
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
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
            StartCoroutine(StopFood(rb));
        }

        _tentacleController.isEating = true; 
    }

    IEnumerator StopFood(Rigidbody2D food)
    {
        food.drag = 10f;
        food.angularDrag = 5f;
        food.gravityScale = 0.5f; // Make it feel lighter once grabbed
        yield return slowDownDelay;
    }

    IEnumerator Swallow(GameObject food)
    {
        // TODO: play eating vfx 
        
        yield return eatDelay;

        Destroy(food);
        currentFood = null;
        
        // Trigger a "Chomp" VFX or Screen Shake here!
        Debug.Log("Nom!");
        
        _tentacleController.isEating = false; 
    }
}