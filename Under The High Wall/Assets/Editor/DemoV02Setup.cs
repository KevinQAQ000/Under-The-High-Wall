using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 编辑器工具：把 Demo v0.2 的手牌栏和骰子战斗界面摆进场景，并接到 RescueDemo 上。
// 只在编辑器里改场景对象，运行时不生成任何界面。
public static class DemoV02Setup
{
    private const int Slots = 4;
    private static readonly string[] ExploreSlots = { "手牌1", "手牌2", "手牌3", "手牌4" };
    private static readonly string[] BattleSlots = { "战斗手牌1", "战斗手牌2", "战斗手牌3", "战斗手牌4" };

    [MenuItem("高墙之下/生成 Demo v0.2 界面")]
    public static void Build()
    {
        RescueDemo demo = Object.FindFirstObjectByType<RescueDemo>(FindObjectsInactive.Include);
        RectTransform layout = GameObject.Find("Rescue Layout")?.transform as RectTransform;
        if (demo == null || layout == null)
        {
            Debug.LogError("找不到 RescueDemo 或 Rescue Layout，请先打开 SampleScene。");
            return;
        }

        RectTransform controls = layout.Find("Controls") as RectTransform;
        RectTransform overlay = layout.Find("Battle Overlay") as RectTransform;
        RectTransform encounter = overlay.Find("Encounter") as RectTransform;
        RectTransform stats = controls.Find("Stats") as RectTransform;
        RectTransform source = FindChild(controls, "投掷骰子");
        if (controls == null || overlay == null || encounter == null || stats == null || source == null)
        {
            Debug.LogError("界面层级不完整：Controls / Battle Overlay / Encounter / Stats / 投掷骰子 缺失。");
            return;
        }
        Font font = stats.GetComponent<Text>().font;

        // 允许重复执行：先清掉上一次生成的手牌槽和标题。
        Purge(controls, "手牌");
        Purge(encounter, "战斗手牌");

        // 右侧控制栏：状态、事件、手牌栏、2×2 按钮。
        Place(stats, new Vector2(22, -10), new Vector2(346, 118));
        var eventBox = controls.Find("Event") as RectTransform;
        Place(eventBox, new Vector2(22, -132), new Vector2(346, 86));
        var eventText = eventBox.GetComponent<Text>();
        eventText.fontSize = 14;
        eventText.horizontalOverflow = HorizontalWrapMode.Wrap;
        eventText.verticalOverflow = VerticalWrapMode.Truncate;
        MakeText(controls, "手牌标题", "手牌（点击按钮使用）", new Vector2(22, -222), new Vector2(346, 18), 12, font);

        var handButtons = new Button[Slots];
        var handLabels = new Text[Slots];
        for (int i = 0; i < Slots; i++)
        {
            RectTransform slot = CloneButton(source, controls, ExploreSlots[i],
                new Vector2(22 + i * 86, -244), new Vector2(78, 48), font, 12);
            handButtons[i] = slot.GetComponent<Button>();
            handLabels[i] = slot.GetComponentInChildren<Text>(true);
        }

        RectTransform roll = FindChild(controls, "投掷骰子");
        RectTransform memory = FindChild(controls, "使用回忆卡");
        RectTransform endTurn = FindChild(controls, "结束回合");
        RectTransform restart = FindChild(controls, "重新开始");
        Place(roll, new Vector2(22, -300), new Vector2(170, 44));
        Place(memory, new Vector2(198, -300), new Vector2(170, 44));
        Place(endTurn, new Vector2(22, -352), new Vector2(170, 44));
        Place(restart, new Vector2(198, -352), new Vector2(170, 44));
        foreach (RectTransform rt in new[] { roll, memory, endTurn, restart })
            FitLabel(rt, 14);

        Wire(roll.GetComponent<Button>(), demo.RollDice);
        Wire(memory.GetComponent<Button>(), demo.UseMemoryCard);
        Wire(endTurn.GetComponent<Button>(), demo.EndPlayerTurn);

        // 战斗面板：拉开高度放手牌，按钮下移。
        Place(encounter, new Vector2(190, -96), new Vector2(620, 440));
        Place(FindChild(encounter, "Enemy"), new Vector2(28, -18), new Vector2(482, 45));
        Place(FindChild(encounter, "Battle Stats"), new Vector2(155, -78), new Vector2(430, 150));
        Place(FindChild(encounter, "怪物"), new Vector2(28, -78), new Vector2(105, 153));
        MakeText(encounter, "战斗手牌标题", "手牌：打出后本回合生效", new Vector2(28, -238), new Vector2(300, 16), 11, font);

        var battleSlots = new Button[Slots];
        var battleLabels = new Text[Slots];
        for (int i = 0; i < Slots; i++)
        {
            RectTransform slot = CloneButton(source, encounter, BattleSlots[i],
                new Vector2(28 + i * 144, -256), new Vector2(136, 44), font, 12);
            battleSlots[i] = slot.GetComponent<Button>();
            battleLabels[i] = slot.GetComponentInChildren<Text>(true);
        }

        RectTransform attack = FindChild(encounter, "攻击");
        RectTransform guard = FindChild(encounter, "防守");
        RectTransform support = FindChild(encounter, "支援");
        RectTransform retreat = FindChild(encounter, "撤退");
        Place(attack, new Vector2(28, -308), new Vector2(270, 50));
        Place(guard, new Vector2(322, -308), new Vector2(270, 50));
        Place(support, new Vector2(28, -366), new Vector2(270, 50));
        Place(retreat, new Vector2(322, -366), new Vector2(270, 50));
        foreach (RectTransform rt in new[] { attack, guard, support, retreat })
            FitLabel(rt, 16);

        SetLabel(attack, "攻击（掷骰对抗）");
        SetLabel(guard, "防守（掷骰格挡）");
        SetLabel(support, "队友支援（两格内）");
        SetLabel(retreat, "撤退（污染 +1）");
        attack.name = "攻击（掷骰对抗）";
        guard.name = "防守（掷骰格挡）";
        support.name = "队友支援（两格内）";
        retreat.name = "撤退（污染 +1）";

        Wire(attack.GetComponent<Button>(), demo.AttackEnemy);
        Wire(guard.GetComponent<Button>(), demo.GuardEnemy);
        Wire(support.GetComponent<Button>(), demo.SupportAlly);
        Wire(retreat.GetComponent<Button>(), demo.RetreatBattle);

        UnityAction[] explore = { demo.UseHandCard0, demo.UseHandCard1, demo.UseHandCard2, demo.UseHandCard3 };
        UnityAction[] battle = { demo.UseBattleCard0, demo.UseBattleCard1, demo.UseBattleCard2, demo.UseBattleCard3 };
        for (int i = 0; i < Slots; i++)
        {
            Wire(handButtons[i], explore[i]);
            Wire(battleSlots[i], battle[i]);
        }

        var so = new SerializedObject(demo);
        SetArray(so, "handButtons", handButtons);
        SetArray(so, "handLabels", handLabels);
        SetArray(so, "battleHandButtons", battleSlots);
        SetArray(so, "battleHandLabels", battleLabels);
        so.ApplyModifiedPropertiesWithoutUndo();

        Scene scene = demo.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Demo v0.2 界面已生成并保存：" + scene.name);
    }

    private static RectTransform FindChild(Transform parent, string key)
    {
        if (parent == null) return null;
        foreach (RectTransform child in parent)
            if (child.name.Contains(key)) return child;
        return null;
    }

    // 清掉同一前缀的旧对象，保证重复执行不会叠出手牌槽。
    private static void Purge(Transform parent, string prefix)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith(prefix)) Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void Place(RectTransform rt, Vector2 position, Vector2 size)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4f, 2f);
        rt.offsetMax = new Vector2(-4f, -2f);
    }

    // 把按钮文字改成自适应字号，避免缩小按钮后中文溢出。
    private static void FitLabel(RectTransform button, int maxSize)
    {
        if (button == null) return;
        Text label = button.GetComponentInChildren<Text>(true);
        if (label == null) return;
        Stretch(label.rectTransform);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = maxSize;
    }

    private static void SetLabel(RectTransform button, string text)
    {
        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null) label.text = text;
    }

    private static Text MakeText(RectTransform parent, string name, string content, Vector2 position, Vector2 size, int fontSize, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.text = content;
        text.color = new Color(0.82f, 0.82f, 0.82f);
        text.alignment = TextAnchor.MiddleLeft;
        Place((RectTransform)go.transform, position, size);
        return text;
    }

    private static RectTransform CloneButton(RectTransform source, Transform parent, string name, Vector2 position, Vector2 size, Font font, int fontSize)
    {
        GameObject go = Object.Instantiate(source.gameObject, parent);
        go.name = name;
        RectTransform rt = (RectTransform)go.transform;
        Place(rt, position, size);
        ClearListeners(go.GetComponent<Button>());
        Text label = go.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.font = font;
            label.text = "空";
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = fontSize;
            Stretch(label.rectTransform);
        }
        return rt;
    }

    // 克隆会带走原按钮的点击事件，这里全部清掉再重新接线。
    private static void ClearListeners(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static void Wire(Button button, UnityAction action)
    {
        ClearListeners(button);
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static void SetArray(SerializedObject so, string field, Object[] values)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogError("RescueDemo 上找不到字段 " + field);
            return;
        }
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
