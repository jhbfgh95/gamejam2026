using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "GameJam/WaveData")]
public class WaveData : ScriptableObject
{
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

    public float[] samples { get; private set; }

    public void Bake(int resolution)
    {
        samples = new float[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            samples[i] = curve.Evaluate(t);
        }
    }

    // 사다리꼴 적분: 구간 [0,1]을 (resolution-1)개 구간으로 나눠 합산
    public float Integral()
    {
        if (samples == null || samples.Length < 2) return 0f;

        float sum = 0f;
        float dx = 1f / (samples.Length - 1);
        for (int i = 0; i < samples.Length - 1; i++)
            sum += (samples[i] + samples[i + 1]) * 0.5f * dx;

        return sum;
    }
}
