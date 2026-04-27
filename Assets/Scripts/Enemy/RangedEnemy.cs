using UnityEngine;

/// <summary>
/// Navigates to a preferred stand-off distance and fires when line-of-sight is clear.
/// Retreats when the player gets too close; repositions when LOS is blocked.
/// </summary>
public class RangedEnemy : EnemyBase
{
    [Header("Ranged Behavior")]
    public float preferredRange = 12f;
    [Tooltip("How much the distance can deviate before the enemy repositions.")]
    public float rangeTolerance = 2.5f;
    public float attackCooldown = 2f;

    private float _attackTimer;

    protected override void Tick(float distToPlayer)
    {
        _attackTimer -= Time.deltaTime;

        bool tooClose = distToPlayer < preferredRange - rangeTolerance;
        bool tooFar   = distToPlayer > preferredRange + rangeTolerance;
        bool hasLos   = HasLineOfSight();

        if (tooClose || tooFar)
        {
            // Move to the point at preferredRange along the enemy↔player axis.
            Vector3 toEnemy = (transform.position - _player.position).normalized;
            Vector3 idealPos = _player.position + toEnemy * preferredRange;
            _agent.SetDestination(idealPos);
            _state = State.Chase;
            PlayAnim("Run");
        }
        else if (!hasLos)
        {
            // In range but blocked — strafe toward player to find LOS.
            _agent.SetDestination(_player.position);
            _state = State.Chase;
            PlayAnim("Run");
        }
        else if (_attackTimer <= 0f)
        {
            // In range, clear LOS, cooled down — shoot.
            _agent.ResetPath();
            _state = State.Attack;
            PlayAnim("Attack");
            PerformRangedAttack();
            _attackTimer = attackCooldown;
        }
        else
        {
            // Holding a good position.
            _agent.ResetPath();
            _state = State.Idle;
            PlayAnim("Idle");
        }
    }

    // -----------------------------------------------------------------------
    // TODO: Instantiate and launch a projectile toward the player here.
    // -----------------------------------------------------------------------
    private void PerformRangedAttack()
    {
        Debug.Log($"{name} [RangedEnemy]: Ranged attack triggered — projectile not yet implemented.");
    }
}
