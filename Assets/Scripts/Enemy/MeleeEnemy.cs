using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Pursues the player and attacks when within melee range.
/// </summary>
public class MeleeEnemy : EnemyBase
{
    [Header("Melee")]
    public float meleeRange = 2.5f;
    public float meleeHitRange = 3.5f;
    public float attackCooldown = 1.4f;
    public float meleeDamage = 15f;
    public float attackAnimDuration = 0.08f;
    public float attackHitDelay = 0.01f;
    public float attackBlendTime = 0.20f;
    public float attackAnimStartTime = 0.2f;

    private float _attackTimer;
    private float _pendingHitTime = -1f;

    protected override void Awake()
    {
        base.Awake();
        _agent.stoppingDistance = Mathf.Max(0.1f, meleeRange - 0.3f);
    }

    protected override void Tick(float distToPlayer)
    {
        _attackTimer -= Time.deltaTime;

        // Resolve a queued hit when the swing reaches its impact frame.
        if (_pendingHitTime > 0f && Time.time >= _pendingHitTime)
        {
            _pendingHitTime = -1f;
            if (distToPlayer <= meleeHitRange) PerformMeleeAttack();
        }

        // Mid-swing: lock state, let the Attack animation finish uninterrupted.
        // Don't rotate — orientation was committed when the swing started.
        if (attackCooldown - _attackTimer < attackAnimDuration)
        {
            _agent.ResetPath();
            return;
        }

        if (distToPlayer <= meleeRange)
        {
            _agent.ResetPath();
            _state = State.Attack;

            if (_attackTimer <= 0f)
            {
                FacePlayer();
                PlayAnim("Attack", attackBlendTime, attackAnimStartTime);
                _pendingHitTime = Time.time + attackHitDelay;
                _attackTimer = attackCooldown;
            }
            else
            {
                FacePlayer();
                PlayAnim("Idle");
            }
        }
        else
        {
            _state = State.Chase;
            _agent.SetDestination(_player.position);

            // Unreachable target (player on a pillar / off-NavMesh) — don't shuffle.
            bool unreachable = !_agent.pathPending
                            && _agent.pathStatus == NavMeshPathStatus.PathPartial;
            if (unreachable)
            {
                _agent.ResetPath();
                PlayAnim("Idle");
            }
            else
            {
                PlayAnim(_agent.velocity.sqrMagnitude > 0.1f ? "Run" : "Idle");
            }
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void PerformMeleeAttack()
    {
        if (_player == null) return;
        PlayerHealth ph = _player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(meleeDamage);
            if (_audio != null) _audio.PlayAttack();
        }
    }
}
