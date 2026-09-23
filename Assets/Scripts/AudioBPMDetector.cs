using UnityEngine;

public static class AudioBPMDetector
{
    public static float AnalyzeBPM(AudioClip clip)
    {
        if (clip == null) return 120f;

        int channels = clip.channels;
        int sampleRate = clip.frequency;
        
        float[] rawSamples = new float[clip.samples * channels];
        clip.GetData(rawSamples, 0);

        float[] monoSamples = new float[clip.samples];
        for (int i = 0; i < monoSamples.Length; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++)
            {
                sum += rawSamples[i * channels + c];
            }
            monoSamples[i] = sum / channels;
        }

        int windowSize = 2048; 
        int totalWindows = monoSamples.Length / windowSize;
        float[] energies = new float[totalWindows];

        for (int w = 0; w < totalWindows; w++)
        {
            float energy = 0f;
            for (int s = 0; s < windowSize; s++)
            {
                float val = monoSamples[w * windowSize + s];
                energy += val * val;
            }
            energies[w] = energy;
        }

        System.Collections.Generic.List<float> beatTimes = new System.Collections.Generic.List<float>();
        
        int historySize = Mathf.Max(10, sampleRate / windowSize); 

        for (int w = historySize; w < totalWindows; w++)
        {
            float localAverageEnergy = 0f;
            for (int h = 1; h <= historySize; h++)
            {
                localAverageEnergy += energies[w - h];
            }
            localAverageEnergy /= historySize;

            float sensitivity = 1.4f; 

            if (energies[w] > localAverageEnergy * sensitivity)
            {
                float timeInSeconds = (float)(w * windowSize) / sampleRate;

                if (beatTimes.Count == 0 || timeInSeconds - beatTimes[beatTimes.Count - 1] > 0.28f)
                {
                    beatTimes.Add(timeInSeconds);
                }
            }
        }

        if (beatTimes.Count < 2) return 120f;

        System.Collections.Generic.List<float> intervals = new System.Collections.Generic.List<float>();
        for (int i = 1; i < beatTimes.Count; i++)
        {
            intervals.Add(beatTimes[i] - beatTimes[i - 1]);
        }

        intervals.Sort();
        float medianInterval = intervals[intervals.Count / 2];

        if (medianInterval <= 0f) return 120f;
        float detectedBPM = 60f / medianInterval;

        while (detectedBPM < 80f) detectedBPM *= 2f;
        while (detectedBPM > 160f) detectedBPM /= 2f;

        return Mathf.Round(detectedBPM);
    }
}
