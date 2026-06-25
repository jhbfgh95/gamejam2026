using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaveData))]
public class WaveDataEditor : Editor
{
    private Texture2D _preview;
    private AnimationCurve _lastCurve;

    public override void OnInspectorGUI()
    {
        var data = (WaveData)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            data.Bake(64);
            RefreshSceneHands(data);
            _preview = null;
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Wave Preview", EditorStyles.boldLabel);

        if (_preview == null || !CurvesEqual(data.curve, _lastCurve))
        {
            _lastCurve = CopyOf(data.curve);
            if (data.samples == null) data.Bake(64);
            _preview = BuildPreview(data, 256, 80);
        }

        Rect previewRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(80));
        EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f));
        GUI.DrawTexture(previewRect, _preview, ScaleMode.StretchToFill);

        if (data.samples != null)
        {
            float integral = data.Integral();
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = Mathf.Abs(integral) < 0.05f ? Color.green : new Color(1f, 0.4f, 0.4f);
            EditorGUILayout.LabelField($"Integral: {integral:F4}   (≈0 = solved)", style);
        }
    }

    static void RefreshSceneHands(WaveData changed)
    {
        foreach (var hand in Object.FindObjectsByType<Hand>(FindObjectsSortMode.None))
        {
            if (!hand.waves.Contains(changed)) continue;
            hand.RecalculateWave();
            EditorUtility.SetDirty(hand.gameObject);
        }
    }

    static Texture2D BuildPreview(WaveData data, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        var bg = new Color(0.12f, 0.12f, 0.12f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

        for (int x = 0; x < w; x++)
            pixels[(h / 2) * w + x] = new Color(0.35f, 0.35f, 0.35f);

        var lineCol = new Color(1f, 0.35f, 0.35f);
        for (int x = 0; x < w; x++)
        {
            float t = (float)x / (w - 1);
            float val = data.curve.Evaluate(t);
            int y = Mathf.Clamp(Mathf.RoundToInt((val * 0.5f + 0.5f) * (h - 1)), 0, h - 1);
            for (int dy = -1; dy <= 1; dy++)
            {
                int py = Mathf.Clamp(y + dy, 0, h - 1);
                pixels[py * w + x] = Color.Lerp(pixels[py * w + x], lineCol, dy == 0 ? 1f : 0.5f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static bool CurvesEqual(AnimationCurve a, AnimationCurve b)
    {
        if (a == null || b == null) return a == b;
        if (a.keys.Length != b.keys.Length) return false;
        for (int i = 0; i < a.keys.Length; i++)
            if (a.keys[i].time != b.keys[i].time || a.keys[i].value != b.keys[i].value)
                return false;
        return true;
    }

    static AnimationCurve CopyOf(AnimationCurve c) => c == null ? null : new AnimationCurve(c.keys);
}
