using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    [Header("Enemy Prefabs")]
    public GameObject meleeEnemyPrefab;
    public GameObject rangedEnemyPrefab;

    // -------------------------------------------------------------------------
    [Header("Wave Composition")]
    [Tooltip("Melee enemies spawned in round 1.")]
    public int baseMeleeCount  = 1;
    [Tooltip("Ranged enemies spawned in round 1.")]
    public int baseRangedCount = 1;
    [Tooltip("Extra melee added each round.")]
    public int meleePerRound   = 1;
    [Tooltip("Extra ranged added each round.")]
    public int rangedPerRound  = 1;
    [Tooltip("Hard cap on melee enemies per wave.")]
    public int maxMeleeCount   = 10;
    [Tooltip("Hard cap on ranged enemies per wave.")]
    public int maxRangedCount  = 10;

    // -------------------------------------------------------------------------
    [Header("Speed Scaling")]
    [Tooltip("Multiplier applied to base move speed on round 1.")]
    public float baseSpeedMultiplier = 1f;
    [Tooltip("Extra speed multiplier added per round. e.g. 0.08 = +8% per round.")]
    public float speedScaleFactor    = 0.08f;
    [Tooltip("Max speed multiplier cap.")]
    public float maxSpeedMultiplier  = 2.5f;

    // -------------------------------------------------------------------------
    [Header("Damage Scaling")]
    [Tooltip("Multiplier applied to all damage values on round 1.")]
    public float baseDamageMultiplier = 1f;
    [Tooltip("Extra damage multiplier added per round.")]
    public float damageScaleFactor    = 0.10f;
    [Tooltip("Max damage multiplier cap.")]
    public float maxDamageMultiplier  = 3f;

    // -------------------------------------------------------------------------
    [Header("Ranged Aggression Scaling")]
    [Tooltip("Burst cooldown is divided by this per round (enemies fire more frequently).")]
    public float burstCooldownScaleFactor = 0.07f;
    [Tooltip("Minimum burst cooldown in seconds regardless of round.")]
    public float minBurstCooldown         = 0.6f;

    // -------------------------------------------------------------------------
    [Header("Spawn Settings")]
    [Tooltip("Seconds between rounds.")]
    public float betweenRoundsDelay = 4f;
    [Tooltip("Random horizontal offset radius applied to each spawn position.")]
    public float spawnPositionOffset = 2.5f;
    [Tooltip("0 = always spawn at furthest node. 1 = fully random. Blends distance weighting with randomness.")]
    [Range(0f, 1f)]
    public float spawnRandomness = 0.35f;

    // -------------------------------------------------------------------------
    private int  _round = 0;
    private readonly List<GameObject> _alive = new List<GameObject>();
    private bool         _roundActive = false;
    private SpawnNode[]  _nodes;
    private Transform    _player;
    private Text         _roundText;
    private Text         _statusText;

    // -------------------------------------------------------------------------
    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _nodes  = FindObjectsOfType<SpawnNode>();

        if (_player == null)        Debug.LogWarning("RoundManager: No Player tag found.");
        if (_nodes.Length == 0)     Debug.LogWarning("RoundManager: No SpawnNodes in scene.");

        BuildUI();
        StartCoroutine(BeginRound());
    }

    void Update()
    {
        if (!_roundActive) return;
        _alive.RemoveAll(e => e == null);
        if (_alive.Count == 0)
        {
            _roundActive = false;
            StartCoroutine(RoundCompleteSequence());
        }
    }

    // -------------------------------------------------------------------------
    IEnumerator RoundCompleteSequence()
    {
        SetStatus("ROUND COMPLETE");
        yield return new WaitForSeconds(betweenRoundsDelay);
        StartCoroutine(BeginRound());
    }

    IEnumerator BeginRound()
    {
        _round++;
        SetRoundText($"ROUND {_round}");
        SetStatus("");
        SpawnWave();
        _roundActive = true;
        yield return null;
    }

    // -------------------------------------------------------------------------
    void SpawnWave()
    {
        int meleeCount  = Mathf.Min(baseMeleeCount  + meleePerRound  * (_round - 1), maxMeleeCount);
        int rangedCount = Mathf.Min(baseRangedCount + rangedPerRound * (_round - 1), maxRangedCount);

        float speedMult  = Mathf.Min(baseSpeedMultiplier  + speedScaleFactor  * (_round - 1), maxSpeedMultiplier);
        float damageMult = Mathf.Min(baseDamageMultiplier + damageScaleFactor * (_round - 1), maxDamageMultiplier);

        // Build a priority-ordered node list (furthest first, blended with randomness)
        var orderedNodes = GetWeightedNodes();

        int nodeIndex = 0;
        for (int i = 0; i < meleeCount; i++)
            SpawnAt(meleeEnemyPrefab,  orderedNodes, ref nodeIndex, speedMult, damageMult);
        for (int i = 0; i < rangedCount; i++)
            SpawnAt(rangedEnemyPrefab, orderedNodes, ref nodeIndex, speedMult, damageMult);
    }

    // Returns nodes sorted by a score blending distance (furthest = best) + randomness
    List<SpawnNode> GetWeightedNodes()
    {
        return _nodes
            .Where(n => n != null)
            .OrderByDescending(n => {
                float dist        = Vector3.Distance(_player.position, n.transform.position);
                float randOffset  = Random.Range(-1f, 1f) * spawnRandomness;
                return dist + randOffset * dist; // scale randomness relative to distance
            })
            .ToList();
    }

    void SpawnAt(GameObject prefab, List<SpawnNode> nodes, ref int nodeIndex, float speedMult, float damageMult)
    {
        if (prefab == null) return;

        // Cycle through nodes so enemies spread across multiple nodes
        SpawnNode node = nodes.Count > 0 ? nodes[nodeIndex % nodes.Count] : null;
        nodeIndex++;

        Vector3 pos = node != null ? node.transform.position : Vector3.zero;

        // Random horizontal offset so enemies don't stack exactly
        Vector2 offset = Random.insideUnitCircle * spawnPositionOffset;
        pos += new Vector3(offset.x, 0f, offset.y);

        var go = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        ApplyScaling(go, speedMult, damageMult);
        _alive.Add(go);
    }

    void ApplyScaling(GameObject go, float speedMult, float damageMult)
    {
        var melee = go.GetComponent<MeleeEnemy>();
        if (melee != null)
        {
            melee.moveSpeed   *= speedMult;
            melee.meleeDamage *= damageMult;
            return;
        }

        var ranged = go.GetComponent<RangedEnemy>();
        if (ranged != null)
        {
            ranged.moveSpeed    *= speedMult;
            ranged.bulletDamage *= damageMult;
            ranged.bulletSpeed  *= Mathf.Lerp(1f, speedMult, 0.5f); // partial bullet speed scaling
            ranged.burstCooldown = Mathf.Max(
                minBurstCooldown,
                ranged.burstCooldown / (1f + burstCooldownScaleFactor * (_round - 1))
            );
        }
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------
    void BuildUI()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        var canvasGO = new GameObject("RoundHUD");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _roundText  = MakeText(canvasGO, "RoundText",  36, new Color(0.5f, 1f, 0.35f),           new Vector2(0f, -20f));
        _statusText = MakeText(canvasGO, "StatusText", 22, new Color(0.75f, 0.75f, 0.75f, 0.85f), new Vector2(0f, -65f));
    }

    Text MakeText(GameObject root, string objName, int fontSize, Color color, Vector2 anchoredPos)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(root.transform, false);
        var t = go.AddComponent<Text>();
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.alignment          = TextAnchor.UpperCenter;
        t.fontSize           = fontSize;
        t.color              = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        t.text               = "";
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(500f, 55f);
        return t;
    }

    void SetRoundText(string s) { if (_roundText  != null) _roundText.text  = s; }
    void SetStatus(string s)    { if (_statusText != null) _statusText.text = s; }
}
