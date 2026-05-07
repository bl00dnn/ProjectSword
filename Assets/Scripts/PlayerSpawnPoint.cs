using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Project Sword/Player Spawn Point")]
public sealed class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private bool hideRendererOnPlay = true;
    [SerializeField] private bool disableColliderOnPlay = true;

    private void Awake()
    {
        if (hideRendererOnPlay)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        if (disableColliderOnPlay)
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.1f, 0.65f, 1f, 0.85f);
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.8f, 2f, 0.8f));
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.08f, 0.12f);

        Gizmos.color = new Color(0.1f, 0.65f, 1f, 0.25f);
        Gizmos.DrawCube(transform.position + Vector3.up, new Vector3(0.8f, 2f, 0.8f));

        Gizmos.color = Color.white;
        Vector3 forward = transform.forward;
        Gizmos.DrawLine(transform.position + Vector3.up * 1.4f, transform.position + Vector3.up * 1.4f + forward);
    }
}
