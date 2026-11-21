using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 把界面改成派对游戏常见布局：左上角角色状态、底部手牌、右下角操作按钮。
// 3D 棋盘铺满全屏，HUD 作为半透明层浮在上面。
// 这个工具会重复执行，元素已经在根节点时会自动找回，不会因为找不到而中断。
public static class DemoHudSetup
{
    private static readonly Color Panel = new Color(0.08f, 0.09f, 0.11f, 0.82f);

    [MenuItem("高墙之下/生成战斗 HUD 布局")]
    public static void Build()
    {
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        var layout = GameObject.Find("Rescue Layout");
        if (demo == null || layout == null)
        {
            Debug.LogError("找不到 Rescue Demo 或 Rescue Layout，先打开 SampleScene。");
            return;
        }

        var root = layout.transform;
        var controls = Child(root, "Controls");

        // 先在原控制面板里找，找不到再去根节点找：支持反复执行。
        Transform Pick(string name)
        {
            if (controls != null)
            {
                var found = Child(controls, name);
                if (found != null) return found;
            }
            return Child(root, name);
        }

        Transform PickPrefix(string prefix)
        {
            if (controls != null)
            {
                var found = ChildByPrefix(controls, prefix);
                if (found != null) return found;
            }
            return ChildByPrefix(root, prefix);
        }

        // 棋盘铺满全屏，放在最底层。
        var viewport = Child(root, "3D Board Viewport");
        if (viewport != null)
        {
            Stretch(viewport);
            viewport.SetAsFirstSibling();
        }

        // 半透明底板：让手牌和右上角状态在棋盘上也能看清。
        EnsurePanel(root, "手牌底板", new Vector2(196, -398), new Vector2(620, 234));
        EnsurePanel(root, "状态底板", new Vector2(704, -2), new Vector2(294, 130));

        // 标题与任务提示放到顶部中间，字号收小，不挡棋盘。
        var title = Pick("Title");
        if (title != null)
        {
            Place(title, new Vector2(300, -6), new Vector2(400, 36));
            Label(title.gameObject, 24, false, TextAnchor.MiddleCenter);
        }
        var objective = Pick("Objective");
        if (objective != null)
        {
            Place(objective, new Vector2(300, -42), new Vector2(400, 24));
            Label(objective.gameObject, 14, false, TextAnchor.MiddleCenter);
        }

        // 左上角：两名角色的头像、身份、各自污染。
        var party = Pick("身份栏");
        if (party != null)
        {
            party.SetParent(root, false);
            Place(party, new Vector2(2, -6), new Vector2(250, 136));
            var partyBg = party.GetComponent<Image>();
            if (partyBg != null) partyBg.color = Panel;
            PlaceIf(party, "头像0", new Vector2(8, -8), new Vector2(48, 48), 0, false, TextAnchor.MiddleCenter);
            PlaceIf(party, "头像1", new Vector2(8, -66), new Vector2(48, 48), 0, false, TextAnchor.MiddleCenter);
            PlaceIf(party, "身份0", new Vector2(62, -2), new Vector2(178, 48), 11, false, TextAnchor.MiddleLeft);
            PlaceIf(party, "身份1", new Vector2(62, -60), new Vector2(178, 48), 11, false, TextAnchor.MiddleLeft);
        }

        // 右上角：女孩污染、回忆卡、手牌数等对局状态。
        var stats = Pick("Stats");
        if (stats != null)
        {
            stats.SetParent(root, false);
            Place(stats, new Vector2(714, -8), new Vector2(276, 118));
            Label(stats.gameObject, 15, false, TextAnchor.UpperRight);
        }

        // 底部中间：手牌，做成竖卡。
        var handTitle = Pick("手牌标题");
        if (handTitle != null)
        {
            handTitle.SetParent(root, false);
            Place(handTitle, new Vector2(208, -446), new Vector2(400, 18));
            Label(handTitle.gameObject, 12, false, TextAnchor.MiddleLeft);
        }
        for (int i = 0; i < 4; i++)
        {
            var card = Pick("手牌" + (i + 1));
            if (card == null) continue;
            card.SetParent(root, false);
            Place(card, new Vector2(208 + i * 148, -470), new Vector2(140, 150));
            Label(card.gameObject, 14, true, TextAnchor.MiddleCenter);
            var text = card.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        // 右下角：掷骰、技能、回忆卡、结束回合、重新开始，竖着排。
        var roll = Pick("投掷骰子");
        if (roll != null)
        {
            roll.SetParent(root, false);
            Place(roll, new Vector2(830, -374), new Vector2(158, 56));
            Label(roll.gameObject, 17, false, TextAnchor.MiddleCenter);
        }
        var skill = Pick("技能按钮");
        if (skill != null)
        {
            skill.SetParent(root, false);
            Place(skill, new Vector2(830, -438), new Vector2(158, 40));
            Label(skill.gameObject, 12, true, TextAnchor.MiddleCenter);
        }
        var memory = Pick("使用回忆卡");
        if (memory != null)
        {
            memory.SetParent(root, false);
            Place(memory, new Vector2(830, -484), new Vector2(158, 40));
            Label(memory.gameObject, 13, false, TextAnchor.MiddleCenter);
        }
        var endTurn = PickPrefix("结束回合");
        if (endTurn != null)
        {
            endTurn.SetParent(root, false);
            Place(endTurn, new Vector2(830, -530), new Vector2(158, 40));
            Label(endTurn.gameObject, 13, false, TextAnchor.MiddleCenter);
        }
        var restart = Pick("重新开始");
        if (restart != null)
        {
            restart.SetParent(root, false);
            Place(restart, new Vector2(830, -576), new Vector2(158, 40));
            Label(restart.gameObject, 13, false, TextAnchor.MiddleCenter);
        }

        // 消息条放在手牌上方，避免和卡牌重叠。
        var message = Pick("Event");
        if (message != null)
        {
            message.SetParent(root, false);
            Place(message, new Vector2(208, -404), new Vector2(600, 36));
            Label(message.gameObject, 14, false, TextAnchor.MiddleLeft);
        }

        // 原来的右侧控制面板搬空后删掉，避免挡棋盘。
        if (controls != null)
        {
            bool empty = true;
            foreach (Transform child in controls) empty = false;
            if (empty) Object.DestroyImmediate(controls.gameObject);
        }

        // 战斗面板、抽身份面板、主菜单要盖在 HUD 上面，放到最后绘制。
        foreach (var overlayName in new[] { "Battle Overlay", "身份抽取面板", "Rescue Menu" })
        {
            var overlay = Child(root, overlayName);
            if (overlay == null) continue;
            overlay.SetAsLastSibling();
            var overlayBg = overlay.GetComponent<Image>();
            if (overlayBg != null)
            {
                var c = overlayBg.color;
                overlayBg.color = new Color(0.02f, 0.02f, 0.03f, Mathf.Max(c.a, 0.9f));
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("战斗 HUD 布局已生成并保存。");
    }

    // 半透明底板只负责衬托，不接收点击，棋盘仍然可以点。
    private static Transform EnsurePanel(Transform root, string name, Vector2 pos, Vector2 size)
    {
        var existing = Child(root, name);
        if (existing != null)
        {
            Place(existing, pos, size);
            return existing;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        Place(go.transform, pos, size);
        var image = go.GetComponent<Image>();
        image.color = new Color(0.04f, 0.05f, 0.06f, 0.55f);
        image.raycastTarget = false;
        go.transform.SetSiblingIndex(1);
        return go.transform;
    }

    private static void PlaceIf(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, bool fit, TextAnchor anchor)
    {
        var child = Child(parent, name);
        if (child == null) return;
        Place(child, pos, size);
        if (fontSize > 0) Label(child.gameObject, fontSize, fit, anchor);
    }

    private static void Stretch(Transform t)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        // 轴心放在左上角：RescueDemo 用 rect 的左上/右下角换算相机视口。
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static void Place(Transform t, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Label(GameObject go, int size, bool fit, TextAnchor anchor)
    {
        var text = go.GetComponentInChildren<Text>();
        if (text == null) return;
        text.fontSize = size;
        text.alignment = anchor;
        text.resizeTextForBestFit = fit;
        if (fit)
        {
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = size;
        }
    }

    // Transform.Find 会把“/”当成路径分隔符，按钮名里带斜杠时必须按名字逐个找。
    private static Transform Child(Transform parent, string name)
    {
        foreach (Transform t in parent) if (t.name == name) return t;
        return parent.Find(name);
    }

    private static Transform ChildByPrefix(Transform parent, string prefix)
    {
        foreach (Transform t in parent) if (t.name.StartsWith(prefix)) return t;
        return null;
    }
}
