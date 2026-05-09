using UnityEngine;
using UnityEngine.AI;

public class RangedEnemy : EnemyBase
{
    [Header("Positioning")]
    public float preferredRange     = 12f;
    public float rangeTolerance     = 3f;
    public float minEngagementRange = 5f;   // closer than this → always retreat

    [Header("Burst Fire")]
    public int   burstSize     = 3;
    public float burstFireRate = 0.22f;     // seconds between shots within a burst
    public float burstCooldown = 2.2f;      // seconds between bursts

    [Header("Accuracy")]
    [Tooltip("Max random angle offset per shot in degrees. Skeleton-like imprecision.")]
    public float spreadAngle = 4f;

    [Header("Strafing")]
    public float strafeSpeed          = 3f;
    public float strafeChangeDuration = 1.8f;

    [Header("Projectile")]
    public GameObject bulletPrefab;
    public float      bulletSpeed        = 35f;
    public float      bulletDamage       = 12f;
    public float      muzzleHeightOffset = 1.4f;

    // burst
    private int   _burstShotsLeft;
    private float _burstFireTimer;
    private float _burstCooldownTimer;

    // strafe
    private int   _strafeDir = 1;
    private float _strafeTimer;

    // player velocity estimation for lead-aiming
    private Vector3 _prevPlayerPos;
    private Vector3 _playerVelocity;

    protected override void Start()
    {
        base.Start();

        // Desync so groups of ranged enemies don't volley simultaneously
        _burstCooldownTimer = Random.Range(0.5f, burstCooldown);
        _strafeTimer        = Random.Range(0f, strafeChangeDuration);
        if (Random.value < 0.5f) _strafeDir = -1;

        if (_player != null) _prevPlayerPos = _player.position;
    }

    protected override void Tick(float distToPlayer)
    {
        // Track player movement for predictive lead
        if (_player != null)
        {
            _playerVelocity = (_player.position - _prevPlayerPos) / Time.deltaTime;
            _prevPlayerPos  = _player.position;
        }

        HandleMovement(distToPlayer);
        HandleBurst(distToPlayer);
    }

    // -------------------------------------------------------------------------
    //  MOVEMENT — retreat, approach, or strafe; shooting is decoupled from this
    // -------------------------------------------------------------------------
    void HandleMovement(float dist)
    {
        bool tooClose = dist < minEngagementRange;
        bool tooFar   = dist > preferredRange + rangeTolerance;
        bool hasLos   = HasLineOfSight();

        if (tooClose)
        {
            // Retreat directly away — still shoots during this if burst is active
            Vector3 awayDir    = (transform.position - _player.position).normalized;
            Vector3 retreatDest = transform.position + awayDir * 6f;
            if (NavMesh.SamplePosition(retreatDest, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
                _agent.SetDestination(navHit.position);
            _state = State.Chase;
            PlayAnim("Run");
        }
        else if (tooFar || !hasLos)
        {
            // Move to preferred stand-off distance / reacquire LOS
            Vector3 toEnemy  = (transform.position - _player.position).normalized;
            Vector3 idealPos = _player.position + toEnemy * preferredRange;
            _agent.SetDestination(idealPos);
            _state = State.Chase;
            PlayAnim("Run");
        }
        else
        {
            // In range with clear LOS — strafe to be harder to hit
            Strafe();
            _state = _burstShotsLeft > 0 ? State.Attack : State.Idle;
            PlayAnim(_burstShotsLeft > 0 ? "Attack" : "Idle");
        }
    }

    void Strafe()
    {
        _strafeTimer -= Time.deltaTime;
        if (_strafeTimer <= 0f)
        {
            _strafeDir   = -_strafeDir;
            _strafeTimer = strafeChangeDuration + Random.Range(-0.4f, 0.4f);
        }

        Vector3 toPlayer   = (_player.position - transform.position).normalized;
        Vector3 perpDir    = Vector3.Cross(toPlayer, Vector3.up) * _strafeDir;
        Vector3 strafeDest = transform.position + perpDir * strafeSpeed;

        if (NavMesh.SamplePosition(strafeDest, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
        else
        {
            // Hit a wall — flip direction immediately
            _strafeDir   = -_strafeDir;
            _strafeTimer = strafeChangeDuration;
        }
    }

    // -------------------------------------------------------------------------
    //  BURST FIRE — fully decoupled; fires in any movement state with LOS
    // -------------------------------------------------------------------------
    void HandleBurst(float dist)
    {
        bool hasLos  = HasLineOfSight();
        // Slightly generous range so retreating enemies can still complete a burst
        bool inRange = dist < preferredRange + rangeTolerance + 5f;

        if (_burstShotsLeft > 0)
        {
            _burstFireTimer -= Time.deltaTime;
            if (_burstFireTimer <= 0f)
            {
                if (hasLos) FireShot();
                _burstShotsLeft--;
                _burstFireTimer = burstFireRate;
                if (_burstShotsLeft == 0)
                    _burstCooldownTimer = burstCooldown;
            }
        }
        else
        {
            _burstCooldownTimer -= Time.deltaTime;
            if (_burstCooldownTimer <= 0f && hasLos && inRange)
            {
                _burstShotsLeft = burstSize;
                _burstFireTimer = 0f;   // first shot fires immediately this frame
            }
        }
    }

    void FireShot()
    {
        if (bulletPrefab == null) return;

        Vector3 muzzlePos = transform.position + Vector3.up * muzzleHeightOffset;
        Vector3 targetPos = _player.position + Vector3.up * 1f;   // aim at chest height

        // Predictive lead — estimate where the player will be when the bullet arrives.
        // Uses 0.5× lead so players can still dodge with reaction.
        float   travelTime = Vector3.Distance(muzzlePos, targetPos) / bulletSpeed;
        Vector3 leadPos    = targetPos + _playerVelocity * travelTime * 0.5f;
        Vector3 dir        = (leadPos - muzzlePos).normalized;

        // Random spread gives skeleton-like imprecision
        if (spreadAngle > 0f)
        {
            dir = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0f) * dir;
        }

        // Snap to face player when shooting regardless of movement direction
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        if (flatDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDir), 0.5f);

        GameObject go     = Instantiate(bulletPrefab, muzzlePos, Quaternion.LookRotation(dir));
        EnemyBullet bullet = go.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.speed  = bulletSpeed;
            bullet.damage = bulletDamage;
            bullet.Launch(dir);
        }
    }
}
