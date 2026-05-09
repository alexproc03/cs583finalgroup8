using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 14f;
    public float damage = 12f;
    public float lifetime = 4f;

    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector3 direction)
    {
        _rb.linearVelocity = direction.normalized * speed;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destroy on environment geometry but not on other enemies or triggers
        if (!other.isTrigger && other.GetComponentInParent<EnemyBase>() == null)
            Destroy(gameObject);
    }
}
