using UnityEngine;

public class HealthOrbSpawner : MonoBehaviour
{
    public GameObject orbPrefab;
    public float respawnDelay = 15f;

    private bool _onCooldown;
    private float _timer;

    void Start() => SpawnOrb();

    void Update()
    {
        if (!_onCooldown) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f) SpawnOrb();
    }

    void SpawnOrb()
    {
        _onCooldown = false;
        var orb = Instantiate(orbPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        orb.GetComponent<HealthOrb>().spawner = this;
    }

    public void OrbPickedUp()
    {
        _onCooldown = true;
        _timer = respawnDelay;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _onCooldown
            ? new Color(1f, 0.3f, 0.3f, 0.6f)
            : new Color(0.2f, 1f, 0.3f, 0.8f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.45f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.65f);
    }
}
