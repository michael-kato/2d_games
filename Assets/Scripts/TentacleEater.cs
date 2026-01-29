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
            if (distToMouth < swallowDistance && !_tentacleController.isEating)
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
    }

    IEnumerator StopFood(Rigidbody2D food)
    {
        food.drag = 10f;
        food.gravityScale = 0.1f;
        yield return slowDownDelay;
    }

    IEnumerator Swallow(GameObject food)
    {
        currentFood = null;
        _tentacleController.isEating = true;

        // TODO: play eating vfx 
        var system = food.GetComponent<ParticleSystem>();
        system.Play();
        
        yield return new WaitWhile(() => system.IsAlive(true));

        Destroy(food);
        
        // Trigger a "Chomp" VFX or Screen Shake here!
        Debug.Log("Nom!");
        
        _tentacleController.isEating = false; 
    }

    IEnumerable PlayVFX(ParticleSystem vfx)
    {
        vfx.Play();
        yield return null;
    }
}