using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 网页版（WebGL）里 URP Lit 材质会变成纯黑，这里把棋盘、棋子换成 URP/Unlit 材质，
// 并把环境光设成固定亮度，保证导出的网页版和编辑器里看起来一致。
public static class DemoWebMaterials
{
    private const string Folder = "Assets/Materials";

    [MenuItem("高墙之下/修复网页版材质")]
    public static void Run()
    {
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        if (demo == null) return;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null)
        {
            Debug.LogError("找不到 URP/Unlit 着色器");
            return;
        }

        var tileMat = GetOrCreate("WebGL_Tile", unlit, Color.white);
        var baseMat = GetOrCreate("WebGL_BoardBase", unlit, new Color(.42f, .45f, .5f, 1f));

        var so = new SerializedObject(demo);
        var tiles = so.FindProperty("tiles");
        for (int i = 0; i < tiles.arraySize; i++)
            if (tiles.GetArrayElementAtIndex(i).objectReferenceValue is Renderer r)
                r.sharedMaterial = tileMat;

        foreach (var field in new[] { "playerPawn", "secondPawn", "girlPawn" })
        {
            Transform t = (Transform)so.FindProperty(field).objectReferenceValue;
            if (t == null) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                Color c = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor")
                    ? r.sharedMaterial.GetColor("_BaseColor")
                    : Color.white;
                if (field == "girlPawn") c = new Color(.92f, .86f, .72f, 1f);
                var mat = GetOrCreate("WebGL_" + field, unlit, c);
                r.sharedMaterial = mat;
            }
        }

        GameObject boardBase = GameObject.Find("Board Base");
        if (boardBase != null)
        {
            var r = boardBase.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = baseMat;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.55f, .57f, .62f, 1f);

        EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("网页版材质已替换：棋盘、棋子、底座改用 Unlit。");
    }

    private static Material GetOrCreate(string name, Shader shader, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
