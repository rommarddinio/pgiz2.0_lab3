using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIFactory
{
    static Font font;

    public static Font DefaultFont
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return font;
        }
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var rt = Rect(parent, name, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    public static RectTransform Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    public static Text Label(Transform parent, string text, int fontSize, Vector2 pos, Vector2 size,
                             TextAnchor anchor = TextAnchor.MiddleCenter, Color? color = null)
    {
        var rt = Rect(parent, "Label", pos, size);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = DefaultFont;
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = color ?? Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string text, Vector2 pos, Vector2 size,
                                    UnityAction onClick, int fontSize = 32)
    {
        var img = Box(parent, "Btn_" + text, pos, size, new Color(0.2f, 0.28f, 0.55f, 1f));
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        Label(img.transform, text, fontSize, Vector2.zero, size);
        return b;
    }

    public static InputField MakeInput(Transform parent, Vector2 pos, Vector2 size, string placeholder)
    {
        var bg = Box(parent, "NameInput", pos, size, new Color(1f, 1f, 1f, 0.15f));
        var input = bg.gameObject.AddComponent<InputField>();
        var inner = size - new Vector2(20f, 0f);
        var txt = Label(bg.transform, "", 32, Vector2.zero, inner);
        var ph = Label(bg.transform, placeholder, 32, Vector2.zero, inner, TextAnchor.MiddleCenter,
                       new Color(1f, 1f, 1f, 0.45f));
        ph.fontStyle = FontStyle.Italic;
        input.targetGraphic = bg;
        input.textComponent = txt;
        input.placeholder = ph;
        input.characterLimit = 16;
        return input;
    }
}
