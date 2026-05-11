using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SporeNetworkEffect : MonoBehaviour
{
    private struct NodeData
    {
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float pulsePhase;
    }

    private struct LineData
    {
        public RectTransform rt;
        public Image img;
        public int a, b;
        public float signalPhase;
        public float signalSpeed;
    }

    private readonly List<NodeData> _nodes = new List<NodeData>();
    private readonly List<LineData> _lines = new List<LineData>();

    private const int   NodeCount = 22;
    private const float MaxDist   = 300f;
    private const float DriftMax  = 7f;

    // #A6FF00 primary, #6BFF1A mid, #C8FF2E glow
    private static readonly Color NodeBase  = new Color(0.651f, 1.000f, 0.000f, 0f);
    private static readonly Color LineBase  = new Color(0.420f, 1.000f, 0.102f, 0f);
    private static readonly Color PulseGlow = new Color(0.784f, 1.000f, 0.180f, 0f);

    private float _canvasW;
    private float _canvasH;

    void Start()
    {
        var canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        _canvasW = canvasRT.rect.width  > 0 ? canvasRT.rect.width  : 1920f;
        _canvasH = canvasRT.rect.height > 0 ? canvasRT.rect.height : 1080f;

        var parent = GetComponent<RectTransform>();

        for (int i = 0; i < NodeCount; i++)
        {
            var go  = new GameObject("Node");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = NodeBase;

            var nrt = go.GetComponent<RectTransform>();
            nrt.sizeDelta        = new Vector2(5f, 5f);
            nrt.anchorMin        = new Vector2(0.5f, 0.5f);
            nrt.anchorMax        = new Vector2(0.5f, 0.5f);
            nrt.anchoredPosition = new Vector2(
                Random.Range(-_canvasW * 0.5f, _canvasW * 0.5f),
                Random.Range(-_canvasH * 0.5f, _canvasH * 0.5f));

            float angle = Random.Range(0f, Mathf.PI * 2f);
            _nodes.Add(new NodeData
            {
                rt         = nrt,
                img        = img,
                velocity   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(2f, DriftMax),
                pulsePhase = Random.Range(0f, Mathf.PI * 2f)
            });
        }

        for (int i = 0; i < NodeCount; i++)
        {
            for (int j = i + 1; j < NodeCount; j++)
            {
                float d = Vector2.Distance(_nodes[i].rt.anchoredPosition, _nodes[j].rt.anchoredPosition);
                if (d > MaxDist) continue;

                var go  = new GameObject("Line");
                go.transform.SetParent(parent, false);
                go.transform.SetAsFirstSibling();
                var img = go.AddComponent<Image>();
                img.color = LineBase;

                var lrt = go.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.5f, 0.5f);
                lrt.anchorMax = new Vector2(0.5f, 0.5f);
                lrt.sizeDelta = new Vector2(d, 1.5f);

                _lines.Add(new LineData
                {
                    rt          = lrt,
                    img         = img,
                    a           = i,
                    b           = j,
                    signalPhase = Random.Range(0f, Mathf.PI * 2f),
                    signalSpeed = Random.Range(0.4f, 1.1f)
                });
            }
        }
    }

    void Update()
    {
        float t  = Time.time;
        float hw = _canvasW * 0.5f;
        float hh = _canvasH * 0.5f;

        for (int i = 0; i < _nodes.Count; i++)
        {
            NodeData n   = _nodes[i];
            Vector2  pos = n.rt.anchoredPosition + n.velocity * Time.deltaTime;

            if (pos.x < -hw || pos.x > hw) { n.velocity.x = -n.velocity.x; pos.x = Mathf.Clamp(pos.x, -hw, hw); }
            if (pos.y < -hh || pos.y > hh) { n.velocity.y = -n.velocity.y; pos.y = Mathf.Clamp(pos.y, -hh, hh); }
            n.rt.anchoredPosition = pos;

            float alpha = Mathf.Sin(t * 0.7f + n.pulsePhase) * 0.25f + 0.35f;
            n.img.color = new Color(NodeBase.r, NodeBase.g, NodeBase.b, alpha);
            _nodes[i]   = n;
        }

        for (int i = 0; i < _lines.Count; i++)
        {
            LineData l    = _lines[i];
            Vector2  posA = _nodes[l.a].rt.anchoredPosition;
            Vector2  posB = _nodes[l.b].rt.anchoredPosition;
            float    d    = Vector2.Distance(posA, posB);

            if (d > MaxDist * 1.2f) { l.img.color = Color.clear; continue; }

            l.rt.anchoredPosition = (posA + posB) * 0.5f;
            l.rt.sizeDelta        = new Vector2(d, 1.5f);
            l.rt.localRotation    = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(posB.y - posA.y, posB.x - posA.x) * Mathf.Rad2Deg);

            float proximity = 1f - Mathf.Clamp01(d / MaxDist);
            float signal    = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * l.signalSpeed * 2f + l.signalPhase)), 8f);
            Color c         = Color.Lerp(LineBase, PulseGlow, signal * proximity);
            l.img.color     = new Color(c.r, c.g, c.b, proximity * 0.10f + signal * proximity * 0.50f);
        }
    }
}
