using UnityEngine;

[DisallowMultipleComponent]
public sealed class SergiusFootstepAudio : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float walkStepInterval = 0.48f;
    [SerializeField] private float runStepInterval = 0.32f;
    [SerializeField] private float minHorizontalSpeed = 0.15f;
    [SerializeField] private float volume = 0.35f;
    [SerializeField] private float pitchVariation = 0.06f;

    private int nextClipIndex;
    private float stepTimer;

    public void Configure(CharacterController controller, AudioSource source, AudioClip[] clips)
    {
        characterController = controller;
        audioSource = source;
        footstepClips = clips;
    }

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 14f;
    }

    private void Update()
    {
        if (characterController == null || audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        if (!characterController.isGrounded || horizontalVelocity.magnitude < minHorizontalSpeed)
        {
            stepTimer = 0f;
            return;
        }

        bool running = SergiusInput.ReadSprint();
        float interval = running ? runStepInterval : walkStepInterval;
        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = interval;
        }
    }

    private void PlayFootstep()
    {
        AudioClip clip = footstepClips[nextClipIndex % footstepClips.Length];
        nextClipIndex++;

        if (clip == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        audioSource.PlayOneShot(clip, volume);
    }
}
