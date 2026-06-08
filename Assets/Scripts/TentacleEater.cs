using System.Collections;
using UnityEngine;

public class TentacleEater : MonoBehaviour
{
    public float detectionRadius = 0.5f;
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

    void FixedUpdate()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.activeLoot.Count == 0) return;

        if (currentFood == null && _tentacleController.canEat)
        {
            float sqrRadius = detectionRadius * detectionRadius;
            Vector2 myPos = transform.position;

            for (int i = 0; i < gm.activeLoot.Count; i++)
            {
                Loot loot = gm.activeLoot[i];
                if (loot == null) continue;

                if (Vector2.SqrMagnitude(myPos - (Vector2)loot.transform.position) <= sqrRadius)
                {
                    GrabFood(loot.transform);
                    return;
                }
            }
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
        if (food == null) yield break;

        _tentacleController.DisableHunting();
        
        try
        {
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.AddTrauma(10f);
            }
            
            AudioManager.Instance?.PlayOmnom();

            // Use a local variable to safely check for the particle system
            var system = food.GetComponent<ParticleSystem>();
            if (system != null) system.Play();

            var scale = food.localScale;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (food == null) yield break;
                    scale *= 0.9f;
                    food.localScale = scale;
                    yield return new WaitForSeconds(0.02f);
                }
                if (food == null) yield break;
                yield return new WaitForSeconds(0.5f);
            }
        }
        finally
        {
            currentFood = null;
            if (food != null) Destroy(food.gameObject); // TODO: pool
            
            _tentacleController.EnableHunting();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}