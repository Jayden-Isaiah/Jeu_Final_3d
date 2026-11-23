using UnityEngine;

public class FlashlightManager : MonoBehaviour
{
    [SerializeField] private Light lightSource;

    [Header("Flicker Settings")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 10f;

    private float baseIntensity;
    private float noiseSeed;

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseSeed);
        lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}