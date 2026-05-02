using System.Collections;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public string weaponName  = "Weapon";
    public int    maxAmmo     = 30;
    public int    currentAmmo;
    public float  fireRate    = 0.2f;
    public float  reloadTime  = 1f;
    public Color  hudColor    = Color.gray;

    public bool  IsReloading    { get; protected set; }
    public float ReloadProgress { get; protected set; }

    protected float    _nextFireTime;
    private   Coroutine _reloadCoroutine;

    protected virtual void Awake() => currentAmmo = maxAmmo;

    public virtual void OnEquip() => gameObject.SetActive(true);

    public virtual void OnUnequip()
    {
        CancelReload();
        gameObject.SetActive(false);
    }

    public virtual void TryFire()
    {
        if (IsReloading || currentAmmo <= 0) return;
        if (Time.time < _nextFireTime) return;
        _nextFireTime = Time.time + fireRate;
        currentAmmo--;
        Fire();
        if (currentAmmo == 0) Reload();
    }

    protected virtual void Fire() { }

    public virtual void Reload()
    {
        if (IsReloading || currentAmmo == maxAmmo) return;
        if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading    = true;
        ReloadProgress = 0f;
        float elapsed  = 0f;

        while (elapsed < reloadTime)
        {
            elapsed       += Time.deltaTime;
            ReloadProgress = elapsed / reloadTime;
            yield return null;
        }

        currentAmmo    = maxAmmo;
        IsReloading    = false;
        ReloadProgress = 1f;
        _reloadCoroutine = null;
    }

    private void CancelReload()
    {
        if (_reloadCoroutine == null) return;
        StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = null;
        IsReloading      = false;
        ReloadProgress   = 0f;
    }
}
