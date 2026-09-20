using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public SongInfo[] songs;

    RectTransform menuP, selectP, recordsP, gameP, resultsP;
    RectTransform[] allPanels;

    int songIdx = 0;
    Difficulty diff = Difficulty.Normal;

    Text menuInfo, selectInfo, recordsText;
    Text resScore, resCombo, resAcc, resDetail, saveInfo;
    Button[] songBtns, diffBtns;
    Button saveBtn;
    InputField nameInput;

    RhythmGame game;
    ResultData lastResult;

    public static string DiffName(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return "Лёгкая";
            case Difficulty.Normal: return "Средняя";
            default: return "Сложная";
        }
    }

    void Awake()
    {
        EnsureSongs();
        BuildCanvas();
        BuildMenu();
        BuildSelect();
        BuildRecords();
        BuildGame();
        BuildResults();
        allPanels = new[] { menuP, selectP, recordsP, gameP, resultsP };
        Show(menuP);
    }

    void EnsureSongs()
    {
        if (songs == null || songs.Length == 0)
        {
            songs = new[]
            {
                new SongInfo { title = "Neon Pulse", bpm = 100f },
                new SongInfo { title = "Cyber Drive", bpm = 128f },
                new SongInfo { title = "Rush Hour", bpm = 150f }
            };
        }
    }

    void BuildCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
        canvasTransform = go.transform;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
    Transform canvasTransform;

    void Show(RectTransform p)
    {
        foreach (var x in allPanels) x.gameObject.SetActive(x == p);
        if (p == menuP) menuInfo.text = "Выбрано: " + songs[songIdx].title + "  -  " + DiffName(diff);
        if (p == recordsP) RefreshRecords();
    }

    static readonly Color BgColor = new Color(0.05f, 0.05f, 0.12f, 1f);
    static readonly Vector2 BtnSize = new Vector2(620f, 90f);

    void BuildMenu()
    {
        menuP = UIFactory.Panel(canvasTransform, "Menu", BgColor);
        UIFactory.Label(menuP, "РИТМ-ИГРА", 100, new Vector2(0, 340), new Vector2(1200, 150));
        menuInfo = UIFactory.Label(menuP, "", 34, new Vector2(0, 240), new Vector2(1200, 60),
                                   TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));

        UIFactory.MakeButton(menuP, "Начать игру", new Vector2(0, 120), BtnSize, StartGame);
        UIFactory.MakeButton(menuP, "Выбор песни и сложности", new Vector2(0, 0), BtnSize, () => { RefreshSelect(); Show(selectP); });
        UIFactory.MakeButton(menuP, "Таблица рекордов", new Vector2(0, -120), BtnSize, () => Show(recordsP));
        UIFactory.MakeButton(menuP, "Выход", new Vector2(0, -240), BtnSize, QuitGame);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void BuildSelect()
    {
        selectP = UIFactory.Panel(canvasTransform, "Select", BgColor);
        UIFactory.Label(selectP, "Выбор песни и сложности", 70, new Vector2(0, 430), new Vector2(1400, 100));
        UIFactory.Label(selectP, "Композиция", 36, new Vector2(0, 340), new Vector2(600, 50));

        songBtns = new Button[songs.Length];
        for (int i = 0; i < songs.Length; i++)
        {
            int idx = i;
            songBtns[i] = UIFactory.MakeButton(selectP,
                songs[i].title + "   (" + songs[i].bpm.ToString("0") + " BPM)",
                new Vector2(0, 270 - i * 95), new Vector2(760, 80),
                () => { songIdx = idx; RefreshSelect(); });
        }

        UIFactory.Label(selectP, "Сложность", 36, new Vector2(0, -100), new Vector2(600, 50));
        diffBtns = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var d = (Difficulty)i;
            diffBtns[i] = UIFactory.MakeButton(selectP, DiffName(d), new Vector2(-300 + i * 300, -180),
                                               new Vector2(260, 80), () => { diff = d; RefreshSelect(); });
        }

        selectInfo = UIFactory.Label(selectP, "", 34, new Vector2(0, -280), new Vector2(1200, 60),
                                     TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));

        UIFactory.MakeButton(selectP, "Играть", new Vector2(-220, -400), new Vector2(380, 80), StartGame);
        UIFactory.MakeButton(selectP, "Назад", new Vector2(220, -400), new Vector2(380, 80), () => Show(menuP));
    }

    void RefreshSelect()
    {
        var on = new Color(0.2f, 0.7f, 0.35f, 1f);
        var off = new Color(0.2f, 0.28f, 0.55f, 1f);
        for (int i = 0; i < songBtns.Length; i++) songBtns[i].image.color = (i == songIdx) ? on : off;
        for (int i = 0; i < diffBtns.Length; i++) diffBtns[i].image.color = (i == (int)diff) ? on : off;
        selectInfo.text = "Выбрано: " + songs[songIdx].title + "  -  " + DiffName(diff);
    }

    void BuildRecords()
    {
        recordsP = UIFactory.Panel(canvasTransform, "Records", BgColor);
        UIFactory.Label(recordsP, "Таблица рекордов", 70, new Vector2(0, 430), new Vector2(1400, 100));
        recordsText = UIFactory.Label(recordsP, "", 34, new Vector2(0, 30), new Vector2(1600, 700),
                                      TextAnchor.UpperLeft);
        UIFactory.MakeButton(recordsP, "Назад", new Vector2(0, -430), new Vector2(380, 80), () => Show(menuP));
    }

    void RefreshRecords()
    {
        var list = ScoreStorage.Load();
        if (list.Count == 0)
        {
            recordsText.text = "Пока нет записей. Сыграйте и сохраните результат!";
            return;
        }
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Mathf.Min(10, list.Count); i++)
        {
            var e = list[i];
            sb.AppendFormat("{0}. {1}  -  {2} очк.  |  комбо {3}  |  {4:0.0}%  |  {5} ({6})\n",
                            i + 1, e.name, e.score, e.maxCombo, e.accuracy, e.song, e.difficulty);
        }
        recordsText.text = sb.ToString();
    }

    void BuildGame()
    {
        gameP = UIFactory.Panel(canvasTransform, "Game", new Color(0.03f, 0.03f, 0.08f, 1f));
        game = gameObject.AddComponent<RhythmGame>();
        game.Build(gameP);
    }

    void StartGame()
    {
        Show(gameP);
        game.Begin(songs[songIdx], diff, songIdx, OnFinished, () => Show(menuP));
    }

    void OnFinished(ResultData r)
    {
        lastResult = r;
        resScore.text = "Итоговый счёт: " + r.score;
        resCombo.text = "Максимальное комбо: " + r.maxCombo;
        resAcc.text = "Точность: " + r.accuracy.ToString("0.0") + "%";
        resDetail.text = "Perfect: " + r.perfect + "     Good: " + r.good + "     Miss: " + r.miss;
        nameInput.text = "";
        saveBtn.interactable = true;
        saveInfo.text = "";
        Show(resultsP);
    }

    void BuildResults()
    {
        resultsP = UIFactory.Panel(canvasTransform, "Results", BgColor);
        UIFactory.Label(resultsP, "Результаты", 80, new Vector2(0, 420), new Vector2(1200, 120));
        resScore = UIFactory.Label(resultsP, "", 56, new Vector2(0, 300), new Vector2(1200, 80));
        resCombo = UIFactory.Label(resultsP, "", 44, new Vector2(0, 220), new Vector2(1200, 70));
        resAcc = UIFactory.Label(resultsP, "", 44, new Vector2(0, 150), new Vector2(1200, 70));
        resDetail = UIFactory.Label(resultsP, "", 36, new Vector2(0, 80), new Vector2(1200, 60),
                                    TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.75f));

        nameInput = UIFactory.MakeInput(resultsP, new Vector2(0, -20), new Vector2(560, 80), "Введите имя");
        saveBtn = UIFactory.MakeButton(resultsP, "Сохранить результат", new Vector2(0, -130), new Vector2(560, 80), SaveResult);
        saveInfo = UIFactory.Label(resultsP, "", 32, new Vector2(0, -205), new Vector2(800, 50),
                                   TextAnchor.MiddleCenter, new Color(0.5f, 1f, 0.5f));

        UIFactory.MakeButton(resultsP, "Играть снова", new Vector2(-300, -330), new Vector2(520, 80), StartGame);
        UIFactory.MakeButton(resultsP, "В меню", new Vector2(300, -330), new Vector2(520, 80), () => Show(menuP));
    }

    void SaveResult()
    {
        if (lastResult == null) return;
        string n = string.IsNullOrWhiteSpace(nameInput.text) ? "Игрок" : nameInput.text.Trim();
        ScoreStorage.Add(new ScoreEntry
        {
            name = n,
            score = lastResult.score,
            maxCombo = lastResult.maxCombo,
            accuracy = lastResult.accuracy,
            song = lastResult.songTitle,
            difficulty = DiffName(lastResult.difficulty)
        });
        saveBtn.interactable = false;
        saveInfo.text = "Результат сохранён!";
    }
}
