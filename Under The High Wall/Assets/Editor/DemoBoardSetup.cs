using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 把棋盘从 8×8 收成 8×7：删掉最下面一排的地块、文字和卡背，并把底板缩短。
static class DemoBoardSetup
{
    private const int Keep = 8 * 7; // 横向 8 格、纵向 7 格

    [MenuItem("高墙之下/去掉最后一排")]
    private static void Trim()
    {
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        if (demo == null)
        {
            Debug.LogError("场景里找不到 RescueDemo。");
            return;
        }

        var so = new SerializedObject(demo);
        TrimArray(so, "tiles");
        TrimArray(so, "tileLabels");
        TrimArray(so, "cardBacks");

        // 底板原来按 8 行做，缩短一行并往前挪，保持边缘对齐。
        var baseObject = GameObject.Find("Board Base");
        if (baseObject != null)
        {
            var scale = baseObject.transform.localScale;
            scale.z = 7.8f;
            baseObject.transform.localScale = scale;
            var position = baseObject.transform.position;
            position.z = -3f;
            baseObject.transform.position = position;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("棋盘已改为 8×7，保留 " + Keep + " 个格子。");
    }

    private static void TrimArray(SerializedObject so, string field)
    {
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("找不到字段 " + field);
            return;
        }

        for (int i = prop.arraySize - 1; i >= Keep; i--)
        {
            var obj = prop.GetArrayElementAtIndex(i).objectReferenceValue;
            if (obj == null) continue;
            var go = obj is Renderer r ? r.gameObject : obj is Text t ? t.gameObject : null;
            if (go != null) Object.DestroyImmediate(go);
        }
        prop.arraySize = Keep;
    }
}
