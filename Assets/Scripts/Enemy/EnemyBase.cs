using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    // Fired whenever this enemy is hit (isKill=true when the hit kills it)
    public static event System.Action<bool> OnEnemyHit;

    protected enum State { Idle, Chase, Attack, Dead }

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Detection")]
    public float detectionRange = 20f;

    [Header("Navigation")]
    public float moveSpeed = 4f;

    protected NavMeshAgent       _agent;
    protected Transform          _player;
    protected CharacterController _playerCC;
    protected State              _state = State.Idle;
    protected Animator           _anim;
    protected EnemyAudio         _audio;
    protected float              _health;

    private string _currentAnim;

    protected virtual void Awake()
    {
        _health = maxHealth;
        _agent  = GetComponent<NavMeshAgent>();
        _anim   = GetComponentInChildren<Animator>();
        _audio  = GetComponentInChildren<EnemyAudio>();
    }

    public virtual void TakeDamage(float damage)
    {
        if (_state == State.Dead) return;
        _health -= damage;
        Debug.Log($"{name} took {damage} dmg, hp={_health}", this);
        if (_health <= 0f) Die();
        else OnEnemyHit?.Invoke(false);
    }

    protected virtual void Die()
    {
        OnEnemyHit?.Invoke(true);
        _state = State.Dead;
        _agent.ResetPath();
        _agent.enabled = false;
        PlayAnim("Death");
        if (_audio != null) _audio.PlayDeath();
        Debug.Log($"{name} died (Destroy in 2s)", this);
        Destroy(gameObject, 2f);
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player   = playerObj.transform;
            _playerCC = playerObj.GetComponent<CharacterController>();
        }
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

    protected void PlayAnim(string animName, float blendTime = 0.15f, float fixedTimeOffset = 0f)
    {
        if (_anim == null || _currentAnim == animName) return;
        int hash = Animator.StringToHash(animName);
        if (!_anim.HasState(0, hash)) return;
        _currentAnim = animName;
        _anim.CrossFadeInFixedTime(hash, blendTime, 0, fixedTimeOffset);
    }

    // Center of the player's collider in world space — tracks crouch height.
    protected Vector3 PlayerAimPoint()
    {
        if (_player == null) return Vector3.zero;
        float h = _playerCC != null ? _playerCC.height * 0.5f : 1f;
        return _player.position + Vector3.up * h;
    }

    // Returns true if nothing blocks the sightline to the player.
    protected bool HasLineOfSight()
    {
        if (_player == null) return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 target = PlayerAimPoint();
        Vector3 dir = target - origin;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude + 0.1f))
            return hit.transform.IsChildOf(_player);

        return false;
    }
}
