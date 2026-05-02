using System.Collections;
using UnityEngine;

public class Rifle : WeaponBase
{
    public float damage = 25f;
    public float range  = 100f;

    private Light        _muzzleFlash;
    private LineRenderer _tracer;

    protected override void Awake()
    {
        weaponName  = "Rifle";
        maxAmmo     = 25;
        fireRate    = 0.12f;
        reloadTime  = 3f;
        hudColor    = new Color(0.55f, 0.55f, 0.55f);
        base.Awake();
    }

    void Start()
    {
        BuildVisuals();
    }

    void BuildVisuals()
    {
        // Point light at the muzzle tip — flashes briefly on each shot.
        GameObject flashGO = new GameObject("MuzzleFlash");
        flashGO.transform.SetParent(transform, false);
        flashGO.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        _muzzleFlash           = flashGO.AddComponent<Light>();
        _muzzleFlash.type      = LightType.Point;
        _muzzleFlash.color     = new Color(1f, 0.85f, 0.4f);
        _muzzleFlash.intensity = 4f;
        _muzzleFlash.range     = 5f;
        _muzzleFlash.enabled   = false;

        // Tracer line from muzzle to impact — visible for one frame.
        GameObject tracerGO = new GameObject("Tracer");
        tracerGO.transform.SetParent(transform, false);
        _tracer = tracerGO.AddComponent<LineRenderer>();
        _tracer.positionCount = 2;
        _tracer.startWidth    = 0.02f;
        _tracer.endWidth      = 0.003f;
        _tracer.useWorldSpace = true;
        _tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _tracer.receiveShadows    = false;
        Material tracerMat = new Material(Shader.Find("Sprites/Default"));
        _tracer.material    = tracerMat;
        _tracer.startColor  = new Color(1f, 0.95f, 0.6f, 1f);
        _tracer.endColor    = new Color(1f, 0.95f, 0.6f, 0f);
        _tracer.enabled     = false;
    }

    protected override void Fire()
    {
        Camera cam    = Camera.main;
        Vector3 origin = cam.transform.position;
        Vector3 dir    = cam.transform.forward;
        Vector3 end    = origin + dir * range;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
        {
            end = hit.point;
            hit.collider.GetComponentInParent<EnemyBase>()?.TakeDamage(damage);
        }

        StartCoroutine(ShowFeedback(end));
    }

    IEnumerator ShowFeedback(Vector3 end)
    {
        _muzzleFlash.enabled = true;
        _tracer.enabled      = true;
        _tracer.SetPosition(0, _muzzleFlash.transform.position);
        _tracer.SetPosition(1, end);

        yield return new WaitForSeconds(0.05f);

        _muzzleFlash.enabled = false;
        _tracer.enabled      = false;
    }
}
