using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public WeaponBase[]  weapons      { get; private set; }
    public int           currentIndex { get; private set; }
    public WeaponBase    currentWeapon => weapons[currentIndex];

    public event System.Action<int> OnWeaponChanged;

    private Camera _camera;

    // Viewmodel: local position and scale relative to the camera for each weapon slot.
    private static readonly (Vector3 pos, Vector3 scale)[] ViewmodelConfig =
    {
        (new Vector3( 0.25f, -0.22f, 0.50f), new Vector3(0.05f, 0.05f, 0.44f)),  // Rifle
        (new Vector3( 0.22f, -0.20f, 0.42f), new Vector3(0.09f, 0.07f, 0.30f)),  // Shotgun
        (new Vector3( 0.25f, -0.22f, 0.50f), new Vector3(0.06f, 0.06f, 0.50f)),  // Machine Gun
        (new Vector3( 0.22f, -0.19f, 0.55f), new Vector3(0.03f, 0.03f, 0.60f)),  // Railgun
    };

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
        weapons    = new WeaponBase[4];
        weapons[0] = BuildWeapon<Rifle>(0);
        weapons[1] = BuildWeapon<Shotgun>(1);
        weapons[2] = BuildWeapon<MachineGun>(2);
        weapons[3] = BuildWeapon<Railgun>(3);
    }

    T BuildWeapon<T>(int index) where T : WeaponBase
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = typeof(T).Name + "_Viewmodel";
        go.transform.SetParent(_camera.transform, false);
        go.transform.localPosition = ViewmodelConfig[index].pos;
        go.transform.localScale    = ViewmodelConfig[index].scale;

        Destroy(go.GetComponent<Collider>());

        // AddComponent triggers Awake, which sets hudColor on the weapon.
        T weapon = go.AddComponent<T>();

        Renderer rend   = go.GetComponent<Renderer>();
        Shader   shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat    = new Material(shader);
        mat.SetColor("_BaseColor", weapon.hudColor);
        mat.SetColor("_Color",     weapon.hudColor);
        rend.material = mat;

        go.SetActive(false);
        return weapon;
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
