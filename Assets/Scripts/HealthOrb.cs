using UnityEngine;

public class HealthOrb : MonoBehaviour
{
    public float healAmount = 25f;

    [HideInInspector] public HealthOrbSpawner spawner;

    void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);
        transform.position = new Vector3(
            transform.position.x,
            spawner != null
                ? spawner.transform.position.y + 0.5f + Mathf.Sin(Time.time * 2f) * 0.15f
                : transform.position.y,
            transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var ph = other.GetComponent<PlayerHealth>()
              ?? other.GetComponentInParent<PlayerHealth>();
        var audio = other.GetComponent<PlayerAudio>()
                 ?? other.GetComponentInParent<PlayerAudio>();

        if (ph != null) ph.Heal(healAmount);
        if (audio != null) audio.PlayHeal();
        if (spawner != null) spawner.OrbPickedUp();

        Destroy(gameObject);
    }
}
