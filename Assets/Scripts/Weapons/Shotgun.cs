using System.Collections;
using UnityEngine;

public class Shotgun : WeaponBase
{
    public int   pelletCount        = 8;
    public float damagePerPellet    = 15f;
    public float minDamagePerPellet = 0.5f;
    public float falloffStart       = 3f;
    public float falloffRange       = 20f;
    public float spreadAngle        = 7f;
    public float range              = 50f;

    private Light          _muzzleFlash;
    private LineRenderer[] _tracers;

    protected override void Awake()
    {
        weaponName = "Shotgun";
        maxAmmo    = 2;
        fireRate   = 0.75f;
        reloadTime = 2f;
        hudColor   = new Color(0.72f, 0.52f, 0.25f);
        base.Awake();
    }

    void Start()
    {
        BuildVisuals();
    }

    void BuildVisuals()
    {
        GameObject flashGO = new GameObject("MuzzleFlash");
        flashGO.transform.SetParent(transform, false);
        flashGO.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        _muzzleFlash           = flashGO.AddComponent<Light>();
        _muzzleFlash.type      = LightType.Point;
        _muzzleFlash.color     = new Color(1f, 0.75f, 0.3f);
        _muzzleFlash.intensity = 7f;
        _muzzleFlash.range     = 7f;
        _muzzleFlash.enabled   = false;

        Material tracerMat = new Material(Shader.Find("Sprites/Default"));
        _tracers = new LineRenderer[pelletCount];

        for (int i = 0; i < pelletCount; i++)
        {
            GameObject go = new GameObject($"Tracer_{i}");
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount      = 2;
            lr.startWidth         = 0.02f;
            lr.endWidth           = 0.003f;
            lr.useWorldSpace      = true;
            lr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows     = false;
            lr.material           = tracerMat;
            lr.startColor         = new Color(1f, 0.8f, 0.35f, 1f);
            lr.endColor           = new Color(1f, 0.8f, 0.35f, 0f);
            lr.enabled            = false;
            _tracers[i]           = lr;
        }
    }

    protected override void Fire()
    {
        Camera  cam    = Camera.main;
        Vector3 origin = cam.transform.position;

        Vector3[] ends = new Vector3[pelletCount];

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 dir = PelletDirection(cam);
            ends[i] = origin + dir * range;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
            {
                ends[i]  = hit.point;
                float t  = Mathf.Clamp01(Mathf.InverseLerp(falloffStart, falloffRange, hit.distance));
                float dmg = Mathf.Lerp(damagePerPellet, minDamagePerPellet, t * t);
                hit.collider.GetComponentInParent<EnemyBase>()?.TakeDamage(dmg);
            }
        }

        StartCoroutine(ShowFeedback(ends));
    }

    Vector3 PelletDirection(Camera cam)
    {
        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
        return (cam.transform.forward
              + cam.transform.right * offset.x
              + cam.transform.up    * offset.y).normalized;
    }

    IEnumerator ShowFeedback(Vector3[] ends)
    {
        Vector3 muzzle = _muzzleFlash.transform.position;
        _muzzleFlash.enabled = true;

        for (int i = 0; i < _tracers.Length; i++)
        {
            _tracers[i].SetPosition(0, muzzle);
            _tracers[i].SetPosition(1, ends[i]);
            _tracers[i].enabled = true;
        }

        yield return new WaitForSeconds(0.1f);

        _muzzleFlash.enabled = false;
        foreach (LineRenderer lr in _tracers) lr.enabled = false;
    }
}
