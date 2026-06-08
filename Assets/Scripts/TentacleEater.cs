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
                _tentacleController.ikTarget.position = currentFood.position;
                StartCoroutine(Swallow(currentFood));
            }
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
        food.linearDamping = 10f;
        food.gravityScale = 0.1f;
        yield return slowDownDelay;
    }

    IEnumerator Swallow(Transform food)
    {
        _tentacleController.DisableHunting();
        
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.AddTrauma(10f);
        }
        
        AudioManager.Instance?.PlayOmnom();

        var system = food.GetComponent<ParticleSystem>();
        system.Play();

        var scale = food.localScale;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                scale *= 0.9f;
                food.localScale = scale;
                yield return new WaitForSeconds(0.02f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        currentFood = null;
        Destroy(food.gameObject); // TODO: pool
        
        _tentacleController.EnableHunting(); 
    }
}