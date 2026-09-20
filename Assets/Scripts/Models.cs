using System;
using System.Collections.Generic;
using UnityEngine;

public enum Difficulty { Easy, Normal, Hard }

[Serializable]
public class SongInfo
{
    public string title = "Песня";
    public AudioClip clip;            
    public float bpm = 120f;          
    public float firstBeatOffset = 0f;
}

public class NoteData
{
    public int lane;
    public float time;
    public float duration;
    public bool IsHold { get { return duration > 0f; } }
    public float EndTime { get { return time + duration; } }
}


public static class ChartGenerator
{
    public static List<NoteData> Generate(float bpm, float offset, float songLength, Difficulty diff, int seed)
    {
        float step, prob, holdChance, doubleChance;
        switch (diff)
        {
            case Difficulty.Easy:   step = 1f;    prob = 0.70f; holdChance = 0.10f; doubleChance = 0.00f; break;
            case Difficulty.Normal: step = 0.5f;  prob = 0.55f; holdChance = 0.12f; doubleChance = 0.10f; break;
            default:                step = 0.5f;  prob = 0.80f; holdChance = 0.15f; doubleChance = 0.25f; break;
        }

        float beat = 60f / bpm;
        var rng = new System.Random(seed);         
        var list = new List<NoteData>();
        var laneFree = new float[4];
        int prevLane = -1;

        float start = offset + 4f * beat;           
        float end = songLength - 3f * beat;

        for (int i = 0; ; i++)
        {
            float t = start + i * step * beat;
            if (t > end) break;
            if (rng.NextDouble() > prob) continue;

            int lane = PickLane(rng, laneFree, t, prevLane, -1);
            if (lane < 0) continue;

            float dur = 0f;
            if (rng.NextDouble() < holdChance)
            {
                dur = beat * (2 + rng.Next(0, 3));  
                if (t + dur > songLength - beat) dur = 0f;
            }

            list.Add(new NoteData { lane = lane, time = t, duration = dur });
            laneFree[lane] = t + dur + 0.05f;
            prevLane = lane;

            if (rng.NextDouble() < doubleChance)
            {
                int l2 = PickLane(rng, laneFree, t, -1, lane);
                if (l2 >= 0)
                {
                    list.Add(new NoteData { lane = l2, time = t, duration = 0f });
                    laneFree[l2] = t + 0.05f;
                }
            }
        }

        list.Sort((a, b) => a.time.CompareTo(b.time));
        return list;
    }

    static int PickLane(System.Random rng, float[] laneFree, float t, int avoid1, int avoid2)
    {
        var c = new List<int>();
        for (int l = 0; l < 4; l++)
        {
            if (l == avoid1 || l == avoid2) continue;
            if (laneFree[l] <= t) c.Add(l);
        }
        if (c.Count == 0)
        {
            if (avoid1 >= 0 && laneFree[avoid1] <= t) return avoid1;
            return -1;
        }
        return c[rng.Next(c.Count)];
    }
}


[Serializable]
public class ScoreEntry
{
    public string name;
    public int score;
    public int maxCombo;
    public float accuracy;
    public string song;
    public string difficulty;
}

[Serializable]
public class ScoreList
{
    public List<ScoreEntry> items = new List<ScoreEntry>();
}

public static class ScoreStorage
{
    const string Key = "rhythm_scores_v1";

    public static List<ScoreEntry> Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<ScoreEntry>();
        var l = JsonUtility.FromJson<ScoreList>(json);
        return (l != null && l.items != null) ? l.items : new List<ScoreEntry>();
    }

    public static void Add(ScoreEntry e)
    {
        var list = Load();
        list.Add(e);
        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > 50) list.RemoveRange(50, list.Count - 50);
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(new ScoreList { items = list }));
        PlayerPrefs.Save();
    }
}
