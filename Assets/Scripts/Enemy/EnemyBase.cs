using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    protected enum State { Idle, Chase, Attack, Dead }

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Detection")]
    public float detectionRange = 20f;

    [Header("Navigation")]
    public float moveSpeed = 4f;

    protected NavMeshAgent _agent;
    protected Transform    _player;
    protected State        _state = State.Idle;
    protected Animation    _anim;
    protected float        _health;

    private string _currentAnim;

    protected virtual void Awake()
    {
        _health = maxHealth;
        _agent  = GetComponent<NavMeshAgent>();
        _anim   = GetComponentInChildren<Animation>();
    }

    public virtual void TakeDamage(float damage)
    {
        if (_state == State.Dead) return;
        _health -= damage;
        if (_health <= 0f) Die();
    }

    protected virtual void Die()
    {
        _state = State.Dead;
        _agent.ResetPath();
        _agent.enabled = false;
        PlayAnim("Death");
        Destroy(gameObject, 2f);
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning($"{name}: No GameObject tagged 'Player' found. Tag your Player object.");

        _agent.speed = moveSpeed;
        PlayAnim("Idle");
    }

    protected virtual void Update()
    {
        if (_state == State.Dead || _player == null) return;
        Tick(Vector3.Distance(transform.position, _player.position));
    }

    // Each subclass drives its own behavior here.
    protected abstract void Tick(float distToPlayer);

    protected void PlayAnim(string animName)
    {
        if (_anim == null || _anim[animName] == null || _currentAnim == animName) return;
        _currentAnim = animName;
        _anim.CrossFade(animName, 0.15f);
    }

    // Returns true if nothing blocks the sightline to the player.
    protected bool HasLineOfSight()
    {
        if (_player == null) return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 target = _player.position + Vector3.up;
        Vector3 dir = target - origin;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude + 0.1f))
            return hit.transform.IsChildOf(_player);

        return false;
    }
}
