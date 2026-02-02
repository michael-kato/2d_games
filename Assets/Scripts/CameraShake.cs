using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Settings")]
    public float shakeIntensity = 0.5f;
    public float shakeFrequency = 25f;
    public float recoverySpeed = 1.5f; // How fast the shake stops

    private float _trauma = 0f;
    private Vector3 _initialPosition;

    void Awake() => Instance = this;

    void Start() => _initialPosition = transform.localPosition;

    public void AddTrauma(float amount)
    {
        // Clamp trauma between 0 and 1
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    void Update()
    {
        if (_trauma > 0)
        {
            // Exponential shake (Trauma squared or cubed feels more organic)
            float shake = _trauma * _trauma;

            // Generate a unique offset based on Perlin Noise for "smooth" shaking
            float offsetX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0) - 0.5f) * shakeIntensity * shake;
            float offsetY = (Mathf.PerlinNoise(0, Time.time * shakeFrequency) - 0.5f) * shakeIntensity * shake;

            transform.localPosition = _initialPosition + new Vector3(offsetX, offsetY, 0);

            // Decay trauma over time
            _trauma = Mathf.Clamp01(_trauma - Time.deltaTime * recoverySpeed);
        }
        else
        {
            // Return to original position
            transform.localPosition = _initialPosition;
        }
    }
}