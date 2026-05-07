using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponBase[] weaponPrefabs;

    public WeaponBase[]  weapons      { get; private set; }
    public int           currentIndex { get; private set; }
    public WeaponBase    currentWeapon => weapons[currentIndex];

    public event System.Action<int> OnWeaponChanged;

    private Camera _camera;

    // Build weapons in Awake so PlayerHUD can safely read them in Start.
    void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
        if (_camera == null) _camera = Camera.main;
        BuildWeapons();
    }

    void Start()
    {
        EquipWeapon(0);
    }

    void Update()
    {
        HandleSwitchInput();
        if (Input.GetMouseButton(0))        currentWeapon.TryFire();
        if (Input.GetKeyDown(KeyCode.R))    currentWeapon.Reload();
    }

    void HandleSwitchInput()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                EquipWeapon(i);
                return;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)      EquipWeapon((currentIndex - 1 + weapons.Length) % weapons.Length);
        else if (scroll < 0f) EquipWeapon((currentIndex + 1) % weapons.Length);
    }

    void BuildWeapons()
    {
        weapons = new WeaponBase[weaponPrefabs.Length];
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            weapons[i] = Instantiate(weaponPrefabs[i], _camera.transform, false);
            weapons[i].gameObject.SetActive(false);
        }
    }

    public void EquipWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;
        weapons[currentIndex]?.OnUnequip();
        currentIndex = index;
        weapons[currentIndex].OnEquip();
        OnWeaponChanged?.Invoke(currentIndex);
    }
}
