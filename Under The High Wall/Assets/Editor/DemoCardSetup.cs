using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 一次性工具：把作品集里的道具卡面铺到手牌槽，并把地块台子抬高一档。
// 运行后所有改动都写进场景，不需要游戏运行时再拼界面。
public static class DemoCardSetup
{
    [MenuItem("高墙之下/生成卡面与台子")]
    public static void Build()
    {
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        if (demo == null)
        {
            Debug.LogError("场景里找不到 RescueDemo");
            return;
        }

        var so = new SerializedObject(demo);

        // 1. 每个牌下面的柱子加高到 0.9（顶面仍在 0.03），牌和角色都立在柱子上。
        var tiles = so.FindProperty("tiles");
        for (int i = 0; i < tiles.arraySize; i++)
        {
            if (!(tiles.GetArrayElementAtIndex(i).objectReferenceValue is Renderer r)) continue;
            var t = r.transform;
            Vector3 scale = t.localScale;
            scale.y = 0.9f;
            t.localScale = scale;
            Vector3 pos = t.localPosition;
            pos.y = 0.39f;  // 柱子高 0.9，底面仍贴着地，顶面抬到 0.84
            t.localPosition = pos;
        }

        // 2. 卡背原来贴着旧台面，台子抬高后要一起抬起来，否则会被埋进地块里。
        var cardBacks = so.FindProperty("cardBacks");
        for (int i = 0; i < cardBacks.arraySize; i++)
        {
            if (!(cardBacks.GetArrayElementAtIndex(i).objectReferenceValue is Renderer r)) continue;
            Vector3 pos = r.transform.localPosition;
            pos.y = 0.846f;
            r.transform.localPosition = pos;
        }

        // 3. 探索手牌槽加卡面图和底部文字条。
        var handButtons = so.FindProperty("handButtons");
        var handLabels = so.FindProperty("handLabels");
        var handArt = so.FindProperty("handArt");
        handArt.arraySize = handButtons.arraySize;
        for (int i = 0; i < handButtons.arraySize; i++)
        {
            var button = handButtons.GetArrayElementAtIndex(i).objectReferenceValue as Button;
            if (button == null) continue;
            var slot = (RectTransform)button.transform;

            Transform art = slot.Find("卡面");
            if (art == null)
            {
                var go = new GameObject("卡面", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(slot, false);
                art = go.transform;
            }
            var artRect = (RectTransform)art;
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            var raw = art.GetComponent<RawImage>();
            raw.raycastTarget = false;
            art.SetSiblingIndex(0);
            handArt.GetArrayElementAtIndex(i).objectReferenceValue = raw;

            Transform strip = slot.Find("文字底");
            if (strip == null)
            {
                var go = new GameObject("文字底", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(slot, false);
                strip = go.transform;
            }
            var stripRect = (RectTransform)strip;
            stripRect.anchorMin = new Vector2(0f, 0f);
            stripRect.anchorMax = new Vector2(1f, 0f);
            stripRect.pivot = new Vector2(.5f, 0f);
            stripRect.sizeDelta = new Vector2(-8f, 46f);
            stripRect.anchoredPosition = new Vector2(0f, 0f);
            var stripImage = strip.GetComponent<Image>();
            stripImage.color = new Color(0f, 0f, 0f, 0.74f);
            stripImage.raycastTarget = false;
            strip.SetSiblingIndex(1);

            var label = handLabels.GetArrayElementAtIndex(i).objectReferenceValue as Text;
            if (label != null)
            {
                var lr = label.rectTransform;
                lr.anchorMin = new Vector2(0f, 0f);
                lr.anchorMax = new Vector2(1f, 0f);
                lr.pivot = new Vector2(.5f, 0f);
                lr.sizeDelta = new Vector2(-12f, 44f);
                lr.anchoredPosition = new Vector2(0f, 2f);
                label.fontSize = 12;
                label.lineSpacing = 1f;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
                label.transform.SetSiblingIndex(2);
            }
        }

        // 3. 五张卡面对应匕首、斧头、破碎玩偶、圣水、圣十字。
        var propArt = so.FindProperty("propArtTextures");
        propArt.arraySize = 5;
        string[] paths =
        {
            "Assets/Art/card/Prop Card/Dagger.png",
            "Assets/Art/card/Prop Card/hatchet.png",
            "Assets/Art/card/Prop Card/broken doll.png",
            "Assets/Art/card/Prop Card/holy water.png",
            "Assets/Art/card/Prop Card/Holy cross.png"
        };
        for (int i = 0; i < paths.Length; i++)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[i]);
            if (tex == null) Debug.LogWarning("缺少卡面：" + paths[i]);
            propArt.GetArrayElementAtIndex(i).objectReferenceValue = tex;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("卡面与台子已更新");
    }
}
