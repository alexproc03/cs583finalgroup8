using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Railgun : WeaponBase
{
    public float damage = 150f;
    public float range  = 150f;

    private float        _rechargeStartTime;
    private Light        _muzzleFlash;
    private LineRenderer _tracer;

    protected override void Awake()
    {
        weaponName = "Railgun";
        maxAmmo    = 1;
        fireRate   = 0f;
        reloadTime = 10f;
        hudColor   = new Color(0.15f, 0.75f, 1f);
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
        _muzzleFlash.color     = new Color(0.4f, 0.9f, 1f);
        _muzzleFlash.intensity = 10f;
        _muzzleFlash.range     = 8f;
        _muzzleFlash.enabled   = false;

        GameObject tracerGO = new GameObject("Tracer");
        tracerGO.transform.SetParent(transform, false);
        _tracer = tracerGO.AddComponent<LineRenderer>();
        _tracer.positionCount     = 2;
        _tracer.startWidth        = 0.04f;
        _tracer.endWidth          = 0.01f;
        _tracer.useWorldSpace     = true;
        _tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _tracer.receiveShadows    = false;
        Material mat = new Material(Shader.Find("Sprites/Default"));
        _tracer.material   = mat;
        _tracer.startColor = new Color(0.5f, 1f, 1f, 1f);
        _tracer.endColor   = new Color(0.5f, 1f, 1f, 0f);
        _tracer.enabled    = false;
    }

    // Tick the recharge progress while the weapon is equipped.
    void Update()
    {
        if (!IsReloading) return;

        float elapsed  = Time.time - _rechargeStartTime;
        ReloadProgress = Mathf.Clamp01(elapsed / reloadTime);

        if (elapsed >= reloadTime)
        {
            currentAmmo    = maxAmmo;
            IsReloading    = false;
            ReloadProgress = 1f;
        }
    }

    public override void OnEquip()
    {
        // Catch up on any recharge time that elapsed while unequipped.
        if (IsReloading)
        {
            float elapsed = Time.time - _rechargeStartTime;
            if (elapsed >= reloadTime)
            {
                currentAmmo    = maxAmmo;
                IsReloading    = false;
                ReloadProgress = 1f;
            }
            else
            {
                ReloadProgress = Mathf.Clamp01(elapsed / reloadTime);
            }
        }
        gameObject.SetActive(true);
    }

    public override void OnUnequip()
    {
        // Intentionally skip CancelReload — recharge continues passively via Time.time.
        gameObject.SetActive(false);
    }

    // Railgun recharges automatically; R key does nothing.
    public override void Reload() { }

    protected override void Fire()
    {
        Camera  cam    = Camera.main;
        Vector3 origin = cam.transform.position;
        Vector3 dir    = cam.transform.forward;

        // Piercing: damage every enemy along the beam, each only once.
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, range);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        var damaged = new HashSet<EnemyBase>();
        foreach (RaycastHit hit in hits)
        {
            EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
            if (enemy != null && damaged.Add(enemy))
                enemy.TakeDamage(damage);
        }

        // Begin passive recharge.
        IsReloading        = true;
        ReloadProgress     = 0f;
        _rechargeStartTime = Time.time;

        StartCoroutine(ShowFeedback(origin + dir * range));
    }

    IEnumerator ShowFeedback(Vector3 end)
    {
        _muzzleFlash.enabled = true;
        _tracer.enabled      = true;
        _tracer.SetPosition(0, _muzzleFlash.transform.position);
        _tracer.SetPosition(1, end);

        yield return new WaitForSeconds(0.15f);

        _muzzleFlash.enabled = false;
        _tracer.enabled      = false;
    }
}
