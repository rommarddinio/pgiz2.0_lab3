using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultData
{
    public int score, maxCombo, perfect, good, miss, total;
    public float accuracy;
    public string songTitle;
    public Difficulty difficulty;
}


public class RhythmGame : MonoBehaviour
{
    const int LaneCount = 4;
    const float LaneW = 160f;         
    const float HitY = -380f;       
    const float SpawnY = 580f;      
    const float ApproachTime = 1.5f;   
    const float PerfectWin = 0.05f;    
    const float GoodWin = 0.12f;      
    const float LeadIn = 2f;    
    const float NoteH = 40f;         

    static readonly KeyCode[] Keys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };
    static readonly Color[] LaneColors =
    {
        new Color(0.30f, 0.85f, 1.00f), new Color(1.00f, 0.40f, 0.80f),
        new Color(1.00f, 0.85f, 0.30f), new Color(0.50f, 1.00f, 0.50f)
    };

    float Speed { get { return (SpawnY - HitY) / ApproachTime; } }
    static float LaneX(int i) { return (i - (LaneCount - 1) / 2f) * LaneW; }

    class Note
    {
        public NoteData d;
        public RectTransform rt;
        public Image img;
        public bool judged, missed, holding;
    }

    RectTransform field;
    Image[] hitImgs = new Image[LaneCount];
    Text scoreText, comboText, judgeText, infoText;
    RectTransform progressFill;

    AudioSource src;
    readonly List<Note> notes = new List<Note>();
    readonly List<Note> active = new List<Note>();
    int spawnIdx;
    bool running;
    double startDsp;
    float songTime, endTime;
    float scoreF;
    int combo, maxCombo, perfect, good, miss;
    float judgeTimer;
    SongInfo song;
    Difficulty diff;
    Action<ResultData> onFinish;
    Action onAbort;

    public void Build(Transform parent)
    {
        field = UIFactory.Rect(parent, "Field", Vector2.zero, new Vector2(LaneCount * LaneW, 1080f));

        for (int i = 0; i < LaneCount; i++)
            UIFactory.Box(field, "Lane" + i, new Vector2(LaneX(i), 0f), new Vector2(LaneW - 4f, 1080f),
                          new Color(1f, 1f, 1f, i % 2 == 0 ? 0.07f : 0.04f));

        UIFactory.Box(field, "HitLine", new Vector2(0f, HitY + 30f), new Vector2(LaneCount * LaneW, 4f), Color.white);

        for (int i = 0; i < LaneCount; i++)
        {
            hitImgs[i] = UIFactory.Box(field, "Hit" + i, new Vector2(LaneX(i), HitY),
                                       new Vector2(LaneW - 8f, 60f), new Color(1f, 1f, 1f, 0.25f));
            UIFactory.Label(hitImgs[i].transform, Keys[i].ToString(), 36, Vector2.zero, new Vector2(LaneW, 60f));
        }

        scoreText = UIFactory.Label(parent, "Очки: 0", 56, new Vector2(-540f, 440f), new Vector2(800f, 100f),
                                    TextAnchor.MiddleLeft);
        infoText = UIFactory.Label(parent, "", 34, new Vector2(540f, 440f), new Vector2(800f, 100f),
                                   TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.7f));
        comboText = UIFactory.Label(parent, "", 80, new Vector2(0f, 230f), new Vector2(600f, 240f));
        judgeText = UIFactory.Label(parent, "", 64, new Vector2(0f, 40f), new Vector2(600f, 100f));
        UIFactory.Label(parent, "Клавиши: D F J K   |   Esc - выход в меню", 26, new Vector2(-540f, -500f),
                        new Vector2(800f, 40f), TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.5f));

        UIFactory.Box(parent, "ProgBg", new Vector2(0f, -535f), new Vector2(1920f, 10f), new Color(1f, 1f, 1f, 0.1f));
        var fill = UIFactory.Box(parent, "ProgFill", new Vector2(-960f, -535f), new Vector2(0f, 10f),
                                 new Color(0.3f, 0.85f, 1f, 1f));
        progressFill = fill.rectTransform;
        progressFill.pivot = new Vector2(0f, 0.5f);
        progressFill.anchoredPosition = new Vector2(-960f, -535f);
    }

    public void Begin(SongInfo s, Difficulty d, int songIndex, Action<ResultData> finishCb, Action abortCb)
    {
        Cleanup();
        song = s; diff = d; onFinish = finishCb; onAbort = abortCb;

        AudioClip clip = s.clip;
        s.bpm = AudioBPMDetector.AnalyzeBPM(clip); 
        float offset = s.firstBeatOffset;

        var data = ChartGenerator.Generate(s.bpm, offset, clip.length, d, songIndex * 10 + (int)d + 1);
        notes.Clear(); active.Clear();
        foreach (var nd in data) notes.Add(new Note { d = nd });
        spawnIdx = 0;

        float last = 0f;
        foreach (var n in notes) last = Mathf.Max(last, n.d.EndTime);
        endTime = notes.Count > 0 ? last + 1.5f : clip.length;

        scoreF = 0f; combo = maxCombo = perfect = good = miss = 0; judgeTimer = 0f;
        infoText.text = s.title + "  -  " + GameController.DiffName(d);
        judgeText.text = ""; comboText.text = "";

        if (src == null) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.clip = clip;
        startDsp = AudioSettings.dspTime + LeadIn;
        src.PlayScheduled(startDsp);  
        running = true;
    }

    void Cleanup()
    {
        running = false;
        if (src != null) src.Stop();
        foreach (var n in active) if (n.rt != null) Destroy(n.rt.gameObject);
        active.Clear();
    }

    void Finish()
    {
        running = false;
        int total = notes.Count;
        var r = new ResultData
        {
            score = (int)scoreF, maxCombo = maxCombo, perfect = perfect, good = good, miss = miss, total = total,
            accuracy = total > 0 ? (perfect + 0.5f * good) / total * 100f : 0f,
            songTitle = song.title, difficulty = diff
        };
        Cleanup();
        if (onFinish != null) onFinish(r);
    }

    void Update()
    {
        if (!running) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cleanup();
            if (onAbort != null) onAbort();
            return;
        }

        songTime = (float)(AudioSettings.dspTime - startDsp);   

        SpawnNotes();
        HandleInput();
        UpdateNotes();
        UpdateHud();

        if (songTime > endTime) Finish();
    }

    void SpawnNotes()
    {
        while (spawnIdx < notes.Count && notes[spawnIdx].d.time - songTime <= ApproachTime + 0.1f)
        {
            var n = notes[spawnIdx++];
            var c = LaneColors[n.d.lane];
            if (n.d.IsHold) c *= 0.85f;
            c.a = 1f;
            n.img = UIFactory.Box(field, "Note", new Vector2(LaneX(n.d.lane), SpawnY),
                                  new Vector2(LaneW - 16f, NoteH), c);
            n.rt = n.img.rectTransform;
            n.rt.pivot = new Vector2(0.5f, 0f);   
            active.Add(n);
        }
    }

    void HandleInput()
    {
        for (int l = 0; l < LaneCount; l++)
        {
            bool held = Input.GetKey(Keys[l]);
            var c = LaneColors[l];
            hitImgs[l].color = held ? new Color(c.r, c.g, c.b, 0.9f) : new Color(1f, 1f, 1f, 0.25f);
            if (Input.GetKeyDown(Keys[l])) TryHit(l);
        }
    }

    void TryHit(int lane)
    {
        Note target = null;
        foreach (var n in active)
        {
            if (n.d.lane != lane || n.judged || n.missed) continue;
            target = n;    
            break;
        }
        if (target == null) return;

        float ad = Mathf.Abs(songTime - target.d.time);
        if (ad > GoodWin) return;   

        bool isPerfect = ad <= PerfectWin;
        scoreF += (isPerfect ? 300f : 100f) * (1f + Mathf.Min(combo, 100) * 0.01f);
        combo++;
        if (combo > maxCombo) maxCombo = combo;
        if (isPerfect) perfect++; else good++;
        ShowJudge(isPerfect ? "PERFECT" : "GOOD", isPerfect ? new Color(1f, 0.9f, 0.3f) : new Color(0.5f, 1f, 0.5f));

        target.judged = true;
        if (target.d.IsHold)
        {
            target.holding = true;
        }
        else
        {
            Destroy(target.rt.gameObject);
            active.Remove(target);
        }
    }

    void UpdateNotes()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var n = active[i];

            if (n.holding)
            {
                scoreF += 100f * Time.deltaTime;
                bool held = Input.GetKey(Keys[n.d.lane]);
                bool complete = songTime >= n.d.EndTime;
                bool released = !held && !complete;

                if (released && songTime >= n.d.EndTime - 0.15f) { complete = true; released = false; }

                if (complete)
                {
                    scoreF += 200f;
                    ShowJudge("HOLD!", new Color(0.4f, 0.9f, 1f));
                    Destroy(n.rt.gameObject); active.RemoveAt(i); continue;
                }
                if (released)
                {
                    combo = 0;
                    ShowJudge("BREAK", new Color(1f, 0.4f, 0.4f));
                    Destroy(n.rt.gameObject); active.RemoveAt(i); continue;
                }
            }

            if (!n.judged && !n.missed && songTime - n.d.time > GoodWin)
            {
                n.missed = true;
                miss++;
                combo = 0;
                ShowJudge("MISS", new Color(1f, 0.35f, 0.35f));
                n.img.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            }

            float headY = HitY + (n.d.time - songTime) * Speed;
            float tailY = HitY + (n.d.EndTime - songTime) * Speed;
            if (n.holding) headY = HitY;

            n.rt.anchoredPosition = new Vector2(LaneX(n.d.lane), headY - NoteH * 0.5f);
            n.rt.sizeDelta = new Vector2(LaneW - 16f, NoteH + Mathf.Max(0f, tailY - headY));

            if (tailY < -620f) { Destroy(n.rt.gameObject); active.RemoveAt(i); }
        }
    }

    void ShowJudge(string text, Color c)
    {
        judgeText.text = text;
        judgeText.color = c;
        judgeTimer = 0.5f;
    }

    void UpdateHud()
    {
        scoreText.text = "Очки: " + (int)scoreF;
        comboText.text = combo >= 2 ? combo + "\nкомбо" : "";

        if (judgeTimer > 0f) judgeTimer -= Time.deltaTime;
        var jc = judgeText.color;
        jc.a = Mathf.Clamp01(judgeTimer / 0.5f);
        judgeText.color = jc;

        float p = endTime > 0f ? Mathf.Clamp01(songTime / endTime) : 0f;
        progressFill.sizeDelta = new Vector2(1920f * p, 10f);
    }
}
