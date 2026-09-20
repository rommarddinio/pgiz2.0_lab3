using UnityEngine;

public static class SynthMusic
{
    static readonly float[] Roots = { 55f, 43.65f, 49f, 41.2f };   
    static readonly float[] Ratios = { 1f, 1.25f, 1.5f, 2f };

    public static AudioClip Create(string name, float bpm, float minSeconds, int seed)
    {
        const int sr = 44100;
        float beat = 60f / bpm;
        int bars = Mathf.CeilToInt(minSeconds / (4f * beat));
        int n = (int)(bars * 4f * beat * sr);
        var data = new float[n];
        var rng = new System.Random(seed);

        for (int i = 0; i < n; i++)
        {
            double t = (double)i / sr;
            float bp = (float)(t / beat);
            int bi = (int)bp;
            float inBeat = (bp - bi) * beat;
            int bar = bi / 4;
            float root = Roots[bar % 4];
            float s = 0f;

            s += 0.7f * Mathf.Sin(2f * Mathf.PI * (50f * inBeat + 100f * (1f - Mathf.Exp(-30f * inBeat)) / 30f))
                      * Mathf.Exp(-7f * inBeat);

            float ht = inBeat - beat * 0.5f;
            if (ht >= 0f) s += ((float)rng.NextDouble() * 2f - 1f) * 0.12f * Mathf.Exp(-45f * ht);

            float eighth = beat * 0.5f;
            float ep = (float)(t / eighth);
            int ei = (int)ep;
            float inE = (ep - ei) * eighth;
            float atk = Mathf.Min(1f, inE * 300f);

            s += 0.22f * (float)System.Math.Sin(2.0 * System.Math.PI * root * t) * atk * Mathf.Exp(-2.5f * inE);
            float af = root * 4f * Ratios[ei % 4];
            s += 0.10f * (float)System.Math.Sin(2.0 * System.Math.PI * af * t) * atk * Mathf.Exp(-6f * inE);

            data[i] = Mathf.Clamp(s, -1f, 1f);
        }

        var clip = AudioClip.Create(name, n, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}
