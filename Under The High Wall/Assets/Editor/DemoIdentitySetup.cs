using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 编辑器工具：把身份牌界面（头像、技能按钮、开局抽牌）摆进 SampleScene，
// 并把引用和按钮事件一起保存。界面在场景里可以直接看到，运行时不临时创建。
public static class DemoIdentitySetup
{
    private static readonly Color Panel = new(0.12f, 0.17f, 0.21f);
    private static readonly Color Amber = new(0.48f, 0.32f, 0.15f);
    private static readonly Color Deep = new(0.05f, 0.06f, 0.07f, 0.97f);
    private static Font uiFont;

    [MenuItem("高墙之下/生成身份牌界面")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        var layout = GameObject.Find("Rescue Layout");
        if (demo == null || layout == null)
        {
            Debug.LogError("找不到 Rescue Demo 或 Rescue Layout，先打开 SampleScene。");
            return;
        }
        var controls = layout.transform.Find("Controls");
        var battleOverlay = layout.transform.Find("Battle Overlay");
        var encounter = battleOverlay.Find("Encounter");
        uiFont = Child(controls, "Stats").GetComponent<Text>().font;

        LayoutControls(controls, demo);
        LayoutBattle(encounter, demo);
        BuildIdentityOverlay(layout.transform, demo);
        Assign(demo, controls, encounter, layout.transform);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("身份牌界面已生成并保存。");
    }

    // Transform.Find 会把“/”当成路径分隔符，这里按名字精确查找直接子物体。
    private static Transform Child(Transform parent, string name)
    {
        foreach (Transform t in parent) if (t.name == name) return t;
        return parent.Find(name);
    }

    // 探索面板：身份栏、状态、事件、手牌、技能、四个按钮。
    private static void LayoutControls(Transform controls, RescueDemo demo)
    {
        Purge(controls, "身份栏");
        Purge(controls, "技能");

        var party = MakePanel(controls, "身份栏", new Vector2(22, -6), new Vector2(346, 56));
        var art0 = MakeRaw(party.transform, "头像0", new Vector2(8, -6), new Vector2(44, 44));
        var text0 = MakeText(party.transform, "身份0", "室友\n可分享的护盾", 13, Color.white, TextAnchor.MiddleLeft, new Vector2(58, -4), new Vector2(110, 48));
        var art1 = MakeRaw(party.transform, "头像1", new Vector2(178, -6), new Vector2(44, 44));
        var text1 = MakeText(party.transform, "身份1", "老师\n开局的准备", 13, Color.white, TextAnchor.MiddleLeft, new Vector2(228, -4), new Vector2(110, 48));

        Place(Child(controls, "Stats"), new Vector2(22, -66), new Vector2(346, 84));
        Child(controls, "Stats").GetComponent<Text>().fontSize = 14;
        Place(Child(controls, "Event"), new Vector2(22, -154), new Vector2(346, 64));
        Child(controls, "Event").GetComponent<Text>().fontSize = 13;
        Place(Child(controls, "手牌标题"), new Vector2(22, -222), new Vector2(346, 16));
        for (int i = 0; i < 4; i++)
            Place(Child(controls, "手牌" + (i + 1)), new Vector2(22 + i * 86, -240), new Vector2(78, 44));
        Place(Child(controls, "投掷骰子"), new Vector2(22, -332), new Vector2(170, 36));
        Place(Child(controls, "使用回忆卡"), new Vector2(198, -332), new Vector2(170, 36));
        var endTurn = Child(controls, "结束回合 / 切换队友");
        if (endTurn == null) endTurn = Child(controls, "结束回合（切换队友）");
        Place(endTurn, new Vector2(22, -376), new Vector2(170, 36));
        endTurn.name = "结束回合（切换队友）";
        Place(Child(controls, "重新开始"), new Vector2(198, -376), new Vector2(170, 36));

        var skill = MakeButton(controls, "技能按钮", "技能", 13, demo.UseSkill);
        Place(skill.transform, new Vector2(22, -290), new Vector2(346, 36));

        SetParty(demo, art0, art1, text0, text1, skill.GetComponent<Button>());
    }

    // 战斗面板：加高到 520，塞进战斗手牌、技能按钮和四个战斗按钮。
    private static void LayoutBattle(Transform encounter, RescueDemo demo)
    {
        Purge(encounter, "战斗技能");
        Place(encounter, new Vector2(190, -60), new Vector2(620, 520));
        Place(Child(encounter, "Enemy"), new Vector2(28, -18), new Vector2(482, 40));
        Child(encounter, "Enemy").GetComponent<Text>().fontSize = 26;
        Place(Child(encounter, "怪物原稿"), new Vector2(28, -66), new Vector2(105, 140));
        Place(Child(encounter, "Battle Stats"), new Vector2(155, -66), new Vector2(430, 150));
        Child(encounter, "Battle Stats").GetComponent<Text>().fontSize = 18;
        Place(Child(encounter, "战斗手牌标题"), new Vector2(28, -218), new Vector2(300, 16));
        for (int i = 0; i < 4; i++)
            Place(Child(encounter, "战斗手牌" + (i + 1)), new Vector2(28 + i * 140, -236), new Vector2(136, 42));
        Place(Child(encounter, "攻击（掷骰对抗）"), new Vector2(28, -332), new Vector2(270, 44));
        Place(Child(encounter, "防守（掷骰格挡）"), new Vector2(322, -332), new Vector2(270, 44));
        Place(Child(encounter, "队友支援（两格内）"), new Vector2(28, -384), new Vector2(270, 44));
        Place(Child(encounter, "撤退（污染 +1）"), new Vector2(322, -384), new Vector2(270, 44));

        var battleSkill = MakeButton(encounter, "战斗技能按钮", "技能", 13, demo.UseSkill);
        Place(battleSkill.transform, new Vector2(28, -286), new Vector2(270, 38));
        SetBattleSkill(demo, battleSkill.GetComponent<Button>());
    }

    // 抽身份牌的面板：两张卡面 + 名字 + 技能说明 + 继续按钮。
    private static void BuildIdentityOverlay(Transform layout, RescueDemo demo)
    {
        var old = layout.Find("身份抽取面板");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var panel = MakePanel(layout, "身份抽取面板", Vector2.zero, Vector2.zero);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 1f);

        MakeText(panel.transform, "标题", "抽取身份牌", 30, Color.white, TextAnchor.MiddleCenter, new Vector2(300, -34), new Vector2(400, 52));
        MakeText(panel.transform, "说明", "本局两名角色随机抽取身份，技能用完即止；战斗类技能每场刷新。", 14,
            new Color(0.75f, 0.75f, 0.78f), TextAnchor.MiddleCenter, new Vector2(200, -90), new Vector2(600, 26));

        var art0 = MakeRaw(panel.transform, "卡面0", new Vector2(150, -120), new Vector2(180, 300));
        var name0 = MakeText(panel.transform, "身份名0", "抽取中…", 20, Color.white, TextAnchor.MiddleCenter, new Vector2(150, -470), new Vector2(220, 30));
        var skill0 = MakeText(panel.transform, "技能0", "", 13, new Color(0.8f, 0.8f, 0.82f), TextAnchor.UpperCenter, new Vector2(140, -500), new Vector2(240, 76));

        var art1 = MakeRaw(panel.transform, "卡面1", new Vector2(630, -120), new Vector2(180, 300));
        var name1 = MakeText(panel.transform, "身份名1", "抽取中…", 20, Color.white, TextAnchor.MiddleCenter, new Vector2(630, -470), new Vector2(220, 30));
        var skill1 = MakeText(panel.transform, "技能1", "", 13, new Color(0.8f, 0.8f, 0.82f), TextAnchor.UpperCenter, new Vector2(620, -500), new Vector2(240, 76));

        var go = MakeButton(panel.transform, "继续按钮", "继续", 18, demo.ContinueAfterIdentity);
        Place(go.transform, new Vector2(400, -580), new Vector2(200, 44));

        panel.SetActive(false);
        SetOverlay(demo, panel.gameObject, art0, art1, name0, name1, skill0, skill1, go.GetComponent<Button>());
    }

    private static void Assign(RescueDemo demo, Transform controls, Transform encounter, Transform layout)
    {
        var so = new SerializedObject(demo);
        var party = Child(controls, "身份栏");
        SetArray(so, "partyArt", new Object[] { party.Find("头像0").GetComponent<RawImage>(), party.Find("头像1").GetComponent<RawImage>() });
        SetArray(so, "partyNames", new Object[] { party.Find("身份0").GetComponent<Text>(), party.Find("身份1").GetComponent<Text>() });
        so.FindProperty("skill").objectReferenceValue = Child(controls, "技能按钮").GetComponent<Button>();
        so.FindProperty("skillLabel").objectReferenceValue = Child(controls, "技能按钮/Label").GetComponent<Text>();
        so.FindProperty("battleSkill").objectReferenceValue = Child(encounter, "战斗技能按钮").GetComponent<Button>();

        var names = new[] { "classmate", "teacher", "security", "mother", "doctor", "confidant" };
        var textures = new Object[names.Length];
        for (int i = 0; i < names.Length; i++)
            textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Characters/" + names[i] + "_card.png");
        SetArray(so, "identityArtTextures", textures);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetParty(RescueDemo demo, RawImage a0, RawImage a1, Text t0, Text t1, Button skill)
    {
        var so = new SerializedObject(demo);
        SetArray(so, "partyArt", new Object[] { a0, a1 });
        SetArray(so, "partyNames", new Object[] { t0, t1 });
        so.FindProperty("skill").objectReferenceValue = skill;
        so.FindProperty("skillLabel").objectReferenceValue = skill.transform.Find("Label").GetComponent<Text>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBattleSkill(RescueDemo demo, Button button)
    {
        var so = new SerializedObject(demo);
        so.FindProperty("battleSkill").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetOverlay(RescueDemo demo, GameObject overlay, RawImage a0, RawImage a1, Text n0, Text n1, Text s0, Text s1, Button go)
    {
        var so = new SerializedObject(demo);
        so.FindProperty("identityOverlay").objectReferenceValue = overlay;
        SetArray(so, "identityArt", new Object[] { a0, a1 });
        SetArray(so, "identityNames", new Object[] { n0, n1 });
        SetArray(so, "identitySkills", new Object[] { s0, s1 });
        so.FindProperty("identityContinue").objectReferenceValue = go;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedObject so, string field, Object[] values)
    {
        var p = so.FindProperty(field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void Purge(Transform parent, string prefix)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child.name.StartsWith(prefix)) Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void Place(Transform t, Vector2 pos, Vector2 size)
    {
        var rect = (RectTransform)t;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject MakePanel(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Panel;
        Place(go.transform, pos, size);
        return go;
    }

    private static RawImage MakeRaw(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<RawImage>();
        image.color = Color.white;
        Place(go.transform, pos, size);
        return image;
    }

    private static Text MakeText(Transform parent, string name, string content, int fontSize, Color color,
        TextAnchor anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = content;
        Place(go.transform, pos, size);
        return text;
    }

    private static GameObject MakeButton(Transform parent, string name, string label, int fontSize, UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Amber;
        var button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        UnityEventTools.AddPersistentListener(button.onClick, action);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
        Stretch(textGo.GetComponent<RectTransform>());
        return go;
    }
}
