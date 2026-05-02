using UnityEngine;

public class MachineGun : WeaponBase
{
    public float damage             = 10f;
    public float minDamage          = 3f;
    public float falloffStart       = 15f;
    public float falloffRange       = 45f;
    public float range              = 100f;

    public float baseSpread         = 0.75f;  // degrees, accuracy when first firing
    public float maxSpread          = 4.5f;   // degrees, accuracy floor after sustained fire
    public float spreadGrowthRate   = 12f;    // degrees per second while firing
    public float spreadRecoveryRate = 20f;    // degrees per second while not firing

    private float        _currentSpread;
    private float        _lastFireTime = -10f;
    private Light        _muzzleFlash;
    private LineRenderer _tracer;

    protected override void Awake()
    {
        weaponName = "M. Gun";
        maxAmmo    = 80;
        fireRate   = 0.08f;
        reloadTime = 3f;
        hudColor   = new Color(0.25f, 0.3f, 0.35f);
        base.Awake();
    }

    void Start()
    {
        _currentSpread = baseSpread;
        BuildVisuals();
    }

    void BuildVisuals()
    {
        GameObject flashGO = new GameObject("MuzzleFlash");
        flashGO.transform.SetParent(transform, false);
        flashGO.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        _muzzleFlash           = flashGO.AddComponent<Light>();
        _muzzleFlash.type      = LightType.Point;
        _muzzleFlash.color     = new Color(1f, 0.85f, 0.4f);
        _muzzleFlash.intensity = 3.5f;
        _muzzleFlash.range     = 4f;
        _muzzleFlash.enabled   = false;

        GameObject tracerGO = new GameObject("Tracer");
        tracerGO.transform.SetParent(transform, false);
        _tracer = tracerGO.AddComponent<LineRenderer>();
        _tracer.positionCount     = 2;
        _tracer.startWidth        = 0.015f;
        _tracer.endWidth          = 0.003f;
        _tracer.useWorldSpace     = true;
        _tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _tracer.receiveShadows    = false;
        Material mat = new Material(Shader.Find("Sprites/Default"));
        _tracer.material   = mat;
        _tracer.startColor = new Color(1f, 0.95f, 0.6f, 1f);
        _tracer.endColor   = new Color(1f, 0.95f, 0.6f, 0f);
        _tracer.enabled    = false;
    }

    void Update()
    {
        bool activelyShooting = Time.time - _lastFireTime < fireRate * 2f;

        if (activelyShooting)
            _currentSpread = Mathf.Min(maxSpread, _currentSpread + spreadGrowthRate * Time.deltaTime);
        else
            _currentSpread = Mathf.Max(baseSpread, _currentSpread - spreadRecoveryRate * Time.deltaTime);

        if (!activelyShooting && _muzzleFlash != null && _muzzleFlash.enabled)
        {
            _muzzleFlash.enabled = false;
            _tracer.enabled      = false;
        }
    }

    protected override void Fire()
    {
        _lastFireTime = Time.time;

        Camera  cam    = Camera.main;
        Vector3 origin = cam.transform.position;
        Vector3 dir    = FireDirection(cam);
        Vector3 end    = origin + dir * range;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
        {
            end = hit.point;
            float t   = Mathf.Clamp01(Mathf.InverseLerp(falloffStart, falloffRange, hit.distance));
            float dmg = Mathf.Lerp(damage, minDamage, t);
            hit.collider.GetComponentInParent<EnemyBase>()?.TakeDamage(dmg);
        }

        _muzzleFlash.enabled = true;
        _tracer.enabled      = true;
        _tracer.SetPosition(0, _muzzleFlash.transform.position);
        _tracer.SetPosition(1, end);
    }

    Vector3 FireDirection(Camera cam)
    {
        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(_currentSpread * Mathf.Deg2Rad);
        return (cam.transform.forward
              + cam.transform.right * offset.x
              + cam.transform.up    * offset.y).normalized;
    }
}
