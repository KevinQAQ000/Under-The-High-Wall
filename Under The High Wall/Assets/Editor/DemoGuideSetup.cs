using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 把提示文字统一放到左边头像下方，并搭出新手引导栏。
public static class DemoGuideSetup
{
    [MenuItem("高墙之下/生成新手引导")]
    public static void Run()
    {
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        GameObject layout = Find("Rescue Layout");
        GameObject bar = Find("身份栏");
        if (demo == null || layout == null || bar == null)
        {
            Debug.LogWarning("找不到 RescueDemo / Rescue Layout / 身份栏。");
            return;
        }

        var old = layout.transform.Find("引导面板");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        Font font = FindCaption(layout);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 面板紧贴在身份栏（y=-8，高 148）下面。
        var panel = new GameObject("引导面板", typeof(RectTransform), typeof(Image));
        var panelRect = (RectTransform)panel.transform;
        panelRect.SetParent(layout.transform, false);
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(2f, -146f);
        panelRect.sizeDelta = new Vector2(210f, 168f);
        var bg = panel.GetComponent<Image>();
        bg.color = new Color(.03f, .04f, .05f, .62f);
        bg.raycastTarget = false;

        Text goal = MakeText(panel.transform, "目标", font, new Vector2(6f, -4f), new Vector2(198f, 24f), 11);
        goal.text = "目标：找到女孩，带她回到入口。";

        Text msg = MakeText(panel.transform, "引导消息", font, new Vector2(6f, -26f), new Vector2(198f, 62f), 11);
        msg.alignment = TextAnchor.UpperLeft;
        msg.text = "点「开始救援」，先看看本局抽到的身份牌。";

        Text title = MakeText(panel.transform, "引导标题", font, new Vector2(6f, -88f), new Vector2(198f, 18f), 12);
        title.fontStyle = FontStyle.Bold;
        title.text = "新手引导 1/4";

        Text hint = MakeText(panel.transform, "引导内容", font, new Vector2(6f, -108f), new Vector2(198f, 46f), 11);
        hint.alignment = TextAnchor.UpperLeft;
        hint.text = "点右下角的「投掷骰子」开始这一回合。";

        // 原来的消息条被上一版脚本搬走了，这里把脚本引用改到新文字上。
        var so = new SerializedObject(demo);
        so.FindProperty("guidePanel").objectReferenceValue = panel;
        so.FindProperty("message").objectReferenceValue = msg;
        so.FindProperty("guideTitle").objectReferenceValue = title;
        so.FindProperty("guideHint").objectReferenceValue = hint;

        // 引导面板是新加的，会盖在菜单和战斗面板上面；把三个全屏面板重新提到最前面。
        foreach (var overlayName in new[] { "Battle Overlay", "身份抽取面板", "Rescue Menu" })
        {
            Transform overlay = layout.transform.Find(overlayName);
            if (overlay != null) overlay.SetAsLastSibling();
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // 手牌标题不再写操作说明，说明统一放到左边引导栏。
        foreach (Text t in layout.GetComponentsInChildren<Text>(true))
            if (t.text.StartsWith("手牌")) t.text = "手牌";

        EditorSceneManager.MarkSceneDirty(layout.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("新手引导栏已生成。");
    }

    private static Font FindCaption(GameObject layout)
    {
        foreach (Text t in layout.GetComponentsInChildren<Text>(true))
            if (t.text.StartsWith("手牌")) return t.font;
        foreach (Text t in layout.GetComponentsInChildren<Text>(true)) return t.font;
        return null;
    }

    private static Text MakeText(Transform parent, string name, Font font, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = new Color(.92f, .92f, .94f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, .9f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private static GameObject Find(string name)
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }
}
