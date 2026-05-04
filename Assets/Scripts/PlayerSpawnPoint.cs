using UnityEngine;

public sealed class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private bool lockSpawnToSideScrollerPlane = true;

    public Vector3 SpawnPosition
    {
        get
        {
            Vector3 position = transform.position;
            if (lockSpawnToSideScrollerPlane)
            {
                position.z = 0f;
            }

            return position;
        }
    }
}
