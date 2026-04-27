using UnityEngine;

/// <summary>
/// Pursues the player and attacks when within melee range.
/// </summary>
public class MeleeEnemy : EnemyBase
{
    [Header("Melee")]
    public float meleeRange = 1.8f;
    public float attackCooldown = 1.4f;

    private float _attackTimer;

    protected override void Tick(float distToPlayer)
    {
        _attackTimer -= Time.deltaTime;

        if (distToPlayer <= meleeRange)
        {
            _agent.ResetPath();
            _state = State.Attack;
            FacePlayer();

            if (_attackTimer <= 0f)
            {
                PlayAnim("Attack");
                PerformMeleeAttack();
                _attackTimer = attackCooldown;
            }
        }
        else
        {
            _state = State.Chase;
            PlayAnim("Run");
            _agent.SetDestination(_player.position);
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // -----------------------------------------------------------------------
    // TODO: Implement melee hitbox, hit detection, and damage application here.
    // -----------------------------------------------------------------------
    private void PerformMeleeAttack()
    {
        Debug.Log($"{name} [MeleeEnemy]: Attack triggered — hitbox not yet implemented.");
    }
}
