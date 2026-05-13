using UnityEngine;

public class SpawnNode : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}
