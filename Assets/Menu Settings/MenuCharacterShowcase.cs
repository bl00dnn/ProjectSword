using UnityEngine;

public sealed class MenuCharacterShowcase : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 7.5f;
    [SerializeField] private float rotationAmplitude = 8f;

    private Quaternion startRotation;

    private void Awake()
    {
        startRotation = transform.rotation;
    }

    private void Update()
    {
        float yaw = Mathf.Sin(Time.time * rotationSpeed * 0.08f) * rotationAmplitude;
        transform.rotation = startRotation * Quaternion.Euler(0f, yaw, 0f);
    }
}
