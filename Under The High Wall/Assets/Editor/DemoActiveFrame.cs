using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 给左上角的角色头像加一个橘黄色高亮框，标出当前行动的角色。
public static class DemoActiveFrame
{
    private static readonly Color Orange = new Color(1f, .55f, .12f, 1f);

    [MenuItem("高墙之下/生成行动高亮框")]
    public static void Run()
    {
        GameObject bar = Find("身份栏");
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        if (bar == null || demo == null)
        {
            Debug.LogWarning("找不到身份栏或 RescueDemo。");
            return;
        }

        var frames = new Image[2];
        for (int i = 0; i < 2; i++)
        {
            Transform avatar = bar.transform.Find("头像" + i);
            if (avatar == null)
            {
                Debug.LogWarning("找不到头像" + i);
                continue;
            }

            Transform existing = bar.transform.Find("行动框" + i);
            GameObject go = existing != null ? existing.gameObject : new GameObject("行动框" + i, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            var target = (RectTransform)avatar;

            go.transform.SetParent(bar.transform, false);
            rect.anchorMin = target.anchorMin;
            rect.anchorMax = target.anchorMax;
            rect.pivot = target.pivot;
            // 尺寸向外扩 4（每边 2 像素），再用 pivot 把外扩量均分到两侧，保证边框是均匀的细线。
            Vector2 grow = new Vector2(4f, 4f);
            rect.anchoredPosition = target.anchoredPosition + new Vector2((target.pivot.x - .5f) * grow.x, (target.pivot.y - .5f) * grow.y);
            rect.sizeDelta = target.sizeDelta + grow;

            var image = go.GetComponent<Image>();
            image.color = Orange;
            image.raycastTarget = false;
            go.transform.SetSiblingIndex(0);
            frames[i] = image;
        }

        var so = new SerializedObject(demo);
        SerializedProperty list = so.FindProperty("activeFrames");
        list.arraySize = 2;
        for (int i = 0; i < 2; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(bar.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("行动高亮框已生成。");
    }

    private static GameObject Find(string name)
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }
}
