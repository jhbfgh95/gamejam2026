using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private int waveResolution = 64;

    public List<WaveData> waves = new List<WaveData>();
    public List<Hand> connectedHands = new List<Hand>();
    public float[] combinedWave;
    public float integralValue;

    void Start()
    {
        foreach (var w in waves)
            w?.Bake(waveResolution);
        RecalculateWave();
    }

    void Update() { }

    void OnValidate()
    {
        foreach (var w in waves)
            w?.Bake(waveResolution);
        RecalculateWave();
    }

    public void RecalculateWave()
    {
        combinedWave = new float[waveResolution];

        // 자신의 고유 파동들 합산
        foreach (var w in waves)
        {
            if (w == null) continue;
            if (w.samples == null) w.Bake(waveResolution);
            for (int i = 0; i < waveResolution; i++)
                combinedWave[i] += w.samples[i];
        }

        // 연결된 노드들의 고유 파동들 합산 (combinedWave 아닌 waves 사용 — 이중 합산 방지)
        foreach (var hand in connectedHands)
        {
            if (hand == null) continue;
            foreach (var w in hand.waves)
            {
                if (w == null) continue;
                if (w.samples == null) w.Bake(waveResolution);
                for (int i = 0; i < waveResolution; i++)
                    combinedWave[i] += w.samples[i];
            }
        }

        // 사다리꼴 적분
        integralValue = 0f;
        float dx = 1f / (waveResolution - 1);
        for (int i = 0; i < waveResolution - 1; i++)
            integralValue += (combinedWave[i] + combinedWave[i + 1]) * 0.5f * dx;

        GetComponent<WaveGraphRenderer>()?.DrawWave(combinedWave, integralValue);
        if (Application.isPlaying) GameManager.Instance?.CheckWinCondition();
    }
}
