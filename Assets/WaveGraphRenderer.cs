using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class WaveGraphRenderer : MonoBehaviour
{
    [SerializeField] private Transform soundBlockTransform;
    [SerializeField] private Vector2 soundBlockFillRatio = new Vector2(0.8f, 0.8f);
    [SerializeField] private Vector2 soundBlockMargin = Vector2.zero;

    [SerializeField] private Vector2 graphSize = new Vector2(0.8f, 0.3f);
    [SerializeField] private Vector3 graphOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private float comfortThreshold = 0.05f;
    [SerializeField] private Color comfortColor = Color.green;
    [SerializeField] private Color stressColor = Color.red;

    [SerializeField] private float scrollSpeed = 0.5f;

    [SerializeField, HideInInspector] private float _lastIntegral;
    private float[] _lastSamples;
    private float _scrollOffset;

    private LineRenderer _lr;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = false;
    }

    void GetGraphParams(out Vector2 size, out Vector3 offset)
    {
        size = graphSize;
        offset = graphOffset;
        if (soundBlockTransform == null) return;

        var sr = soundBlockTransform.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Vector3 center = transform.InverseTransformPoint(sr.bounds.center);
        offset = center + new Vector3(soundBlockMargin.x, soundBlockMargin.y, 0f);

        Vector3 localSize = transform.InverseTransformVector(sr.bounds.size);
        size = new Vector2(
            Mathf.Abs(localSize.x) * soundBlockFillRatio.x,
            Mathf.Abs(localSize.y) * 0.5f * soundBlockFillRatio.y);
    }

    private void Update()
    {
        if (!Application.isPlaying || _lastSamples == null || _lastSamples.Length < 2) return;

        _scrollOffset += scrollSpeed * Time.deltaTime;

        GetGraphParams(out Vector2 size, out Vector3 offset);
        int n = _lastSamples.Length;
        _lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / (n - 1);
            float samplePos = (_scrollOffset * n + i) % n;
            int idx0 = (int)samplePos % n;
            int idx1 = (idx0 + 1) % n;
            float val = Mathf.Lerp(_lastSamples[idx0], _lastSamples[idx1], samplePos - Mathf.Floor(samplePos));
            float x = (t - 0.5f) * size.x;
            float y = val * size.y + offset.y;
            _lr.SetPosition(i, new Vector3(x + offset.x, y, offset.z));
        }
    }

    private void OnValidate()
    {
        if (_lastSamples != null)
            DrawWave(_lastSamples, _lastIntegral);
    }

    public void DrawWave(float[] samples, float integralValue)
    {
        if (_lr == null) _lr = GetComponent<LineRenderer>();
        if (_lr == null || samples == null || samples.Length < 2) return;

        GetGraphParams(out Vector2 size, out Vector3 offset);
        _lr.positionCount = samples.Length;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / (samples.Length - 1);
            float x = (t - 0.5f) * size.x;
            float y = samples[i] * size.y + offset.y;
            _lr.SetPosition(i, new Vector3(x + offset.x, y, offset.z));
        }

        _lr.startWidth = lineWidth;
        _lr.endWidth = lineWidth;
        _lastSamples = samples;
        _lastIntegral = integralValue;
        Color c = Mathf.Abs(integralValue) < comfortThreshold ? comfortColor : stressColor;
        _lr.startColor = c;
        _lr.endColor = c;
        _lr.sharedMaterial.color = c;
    }
}
