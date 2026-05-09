using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private WeaponManager _wm;
    private Image[]       _slotBgs;
    private Text[]        _slotTexts;
    private Text          _weaponNameText;
    private Text          _ammoText;
    private Font          _font;
    private PlayerHealth  _ph;
    private Text          _healthText;
    private GameObject    _deathOverlay;
    private Image         _damageFlash;

    void Start()
    {
        _wm = FindFirstObjectByType<WeaponManager>();
        if (_wm == null) return;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
             ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject canvasGO = new GameObject("PlayerHUD");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        BuildDamageFlash(canvasGO);
        BuildCrosshair(canvasGO);
        BuildWeaponSlots(canvasGO);
        BuildWeaponInfo(canvasGO);
        BuildHealthBar(canvasGO);
        BuildDeathOverlay(canvasGO);

        PlayerHealth.OnPlayerDied    += ShowDeathOverlay;
        PlayerHealth.OnPlayerDamaged += FlashDamage;

        _wm.OnWeaponChanged += RefreshSlots;
        RefreshSlots(_wm.currentIndex);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _ph = playerObj.GetComponent<PlayerHealth>();
        if (_ph != null)
        {
            _ph.OnHealthChanged += RefreshHealth;
            RefreshHealth(_ph.CurrentHealth, _ph.maxHealth);
        }
    }

    void OnDestroy()
    {
        if (_wm != null) _wm.OnWeaponChanged -= RefreshSlots;
        if (_ph != null) _ph.OnHealthChanged -= RefreshHealth;
        PlayerHealth.OnPlayerDied    -= ShowDeathOverlay;
        PlayerHealth.OnPlayerDamaged -= FlashDamage;
    }

    void Update()
    {
        if (_wm == null || _ammoText == null) return;
        WeaponBase w = _wm.currentWeapon;
        _ammoText.text = w.IsReloading
            ? $"RELOADING  {Mathf.CeilToInt((1f - w.ReloadProgress) * w.reloadTime)}s"
            : $"{w.currentAmmo} / {w.maxAmmo}";
    }

    // -------------------------------------------------------------------------

    void BuildDamageFlash(GameObject root)
    {
        GameObject go = new GameObject("DamageFlash");
        go.transform.SetParent(root.transform, false);
        _damageFlash = go.AddComponent<Image>();
        _damageFlash.color = new Color(0.85f, 0f, 0f, 0f);
        _damageFlash.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void FlashDamage()
    {
        if (_damageFlash == null) return;
        StopCoroutine("DamageFlashRoutine");
        StartCoroutine(DamageFlashRoutine());
    }

    IEnumerator DamageFlashRoutine()
    {
        // Snap to peak alpha, then fade out
        _damageFlash.color = new Color(0.85f, 0f, 0f, 0.28f);
        float duration = 0.45f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0.28f, 0f, t / duration);
            _damageFlash.color = new Color(0.85f, 0f, 0f, a);
            yield return null;
        }
        _damageFlash.color = new Color(0.85f, 0f, 0f, 0f);
    }

    void BuildCrosshair(GameObject root)
    {
        (Vector2 pos, Vector2 size)[] arms =
        {
            (new Vector2(  0,  10), new Vector2(2,  8)),
            (new Vector2(  0, -10), new Vector2(2,  8)),
            (new Vector2(-10,   0), new Vector2(8,  2)),
            (new Vector2( 10,   0), new Vector2(8,  2)),
        };

        foreach (var (pos, size) in arms)
        {
            GameObject go = new GameObject("CH");
            go.transform.SetParent(root.transform, false);
            go.AddComponent<Image>().color = Color.white;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }
    }

    void BuildWeaponSlots(GameObject root)
    {
        int n = _wm.weapons.Length;
        _slotBgs   = new Image[n];
        _slotTexts = new Text[n];

        const float slotSize = 75f;
        const float gap      = 8f;
        float totalW = n * slotSize + (n - 1) * gap;

        for (int i = 0; i < n; i++)
        {
            GameObject slotGO = new GameObject($"Slot_{i + 1}");
            slotGO.transform.SetParent(root.transform, false);
            _slotBgs[i] = slotGO.AddComponent<Image>();

            RectTransform rt = slotGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            float x             = -totalW / 2f + i * (slotSize + gap) + slotSize / 2f;
            rt.anchoredPosition = new Vector2(x, 20f);
            rt.sizeDelta        = new Vector2(slotSize, slotSize);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(slotGO.transform, false);
            Text t = labelGO.AddComponent<Text>();
            t.font               = _font;
            t.alignment          = TextAnchor.MiddleCenter;
            t.fontSize           = 12;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            t.text               = $"[{i + 1}]\n{_wm.weapons[i].weaponName}";
            _slotTexts[i]        = t;

            RectTransform lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }
    }

    void BuildHealthBar(GameObject root)
    {
        _healthText = MakeText(root, "HealthText", TextAnchor.LowerLeft, 22, Color.white,
                               new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(260f, 28f));
    }

    void BuildDeathOverlay(GameObject root)
    {
        _deathOverlay = new GameObject("DeathOverlay");
        _deathOverlay.transform.SetParent(root.transform, false);

        Image tint = _deathOverlay.AddComponent<Image>();
        tint.color = new Color(0f, 0f, 0f, 0.65f);
        tint.raycastTarget = true;
        RectTransform rt = _deathOverlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        GameObject labelGO = new GameObject("YouDied");
        labelGO.transform.SetParent(_deathOverlay.transform, false);
        Text label = labelGO.AddComponent<Text>();
        label.font = _font;
        label.text = "YOU DIED";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 96;
        label.color = new Color(0.85f, 0.1f, 0.1f, 1f);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = new Vector2(0f, 150f);
        lrt.sizeDelta = new Vector2(800f, 200f);

        _deathOverlay.SetActive(false);
    }

    void ShowDeathOverlay()
    {
        if (_deathOverlay != null) _deathOverlay.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void RefreshHealth(float current, float max)
    {
        if (_healthText != null) _healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    void BuildWeaponInfo(GameObject root)
    {
        var botRight = new Vector2(1f, 0f);
        _weaponNameText = MakeText(root, "WeaponName", TextAnchor.LowerRight, 22, Color.white,
                                   botRight, new Vector2(-20f, 108f), new Vector2(180f, 30f));
        _ammoText       = MakeText(root, "Ammo", TextAnchor.LowerRight, 15,
                                   new Color(0.75f, 0.75f, 0.75f),
                                   botRight, new Vector2(-20f, 80f), new Vector2(120f, 24f));
    }

    Text MakeText(GameObject root, string name, TextAnchor anchor, int fontSize, Color color,
                  Vector2 anchorPivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        Text t = go.AddComponent<Text>();
        t.font               = _font;
        t.alignment          = anchor;
        t.fontSize           = fontSize;
        t.color              = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorPivot;
        rt.pivot            = anchorPivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return t;
    }

    void RefreshSlots(int activeIndex)
    {
        if (_slotBgs == null) return;
        for (int i = 0; i < _slotBgs.Length; i++)
        {
            bool active = i == activeIndex;
            _slotBgs[i].color   = active ? _wm.weapons[i].hudColor
                                         : new Color(0.12f, 0.12f, 0.12f, 0.75f);
            _slotTexts[i].color = active ? Color.black
                                         : new Color(0.65f, 0.65f, 0.65f, 1f);
        }
        if (_weaponNameText != null) _weaponNameText.text = _wm.weapons[activeIndex].weaponName;
        if (_ammoText != null)       _ammoText.text       = $"{_wm.currentWeapon.currentAmmo} / {_wm.currentWeapon.maxAmmo}";
    }
}
