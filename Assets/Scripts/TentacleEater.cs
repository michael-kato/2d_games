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
            var t = Time.deltaTime * grabStrength;
            _tentacleController.ikTarget.position = Vector3.MoveTowards(_tentacleController.ikTarget.position, currentFood.position, t);
            
            // we use vector2 specifically because the z axis is irrelevant here
            float distToMouth = Vector2.Distance(currentFood.position, _tentacleController.ikTarget.position);
            if (distToMouth < swallowDistance && _tentacleController.canEat)
            {
                // snap to
                currentFood.position = _tentacleController.ikTarget.position;
                StartCoroutine(Swallow(currentFood));
            }
        }
        else
        {
            Debug.Log("No food found");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentFood == null && collision.CompareTag(foodTag) && _tentacleController.canEat)
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

    IEnumerator Swallow(Transform food)
    {
        currentFood = null;
        _tentacleController.canEat = false;
        
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.AddTrauma(10f);
        }
        
        var system = food.GetComponent<ParticleSystem>();
        system.Play();
        
        yield return new WaitForSeconds(2.0f);

        Destroy(food.gameObject);
        
        Debug.Log("Nom!");
        
        _tentacleController.canEat = true; 
    }

    IEnumerable PlayVFX(ParticleSystem vfx)
    {
        vfx.Play();
        yield return null;
    }
}