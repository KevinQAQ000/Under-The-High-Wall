using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 修正素材被导入成 512×1024 导致的形变，并给封面重新挂上图片。
public static class DemoArtFix
{
    [MenuItem("高墙之下/修复素材比例与封面")]
    public static void Run()
    {
        // 1. 卡面原图是 732×1028，Unity 默认会把非 2 次幂缩到最近的 2 次幂（512×1024），
        //    比例就从 1.40 变成 2.00，看起来像被拉长。这里统一关闭缩放。
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            if (importer.npotScale == TextureImporterNPOTScale.None && !importer.mipmapEnabled) continue;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        // 2. 封面右侧的 Logo 和卡面：之前纹理引用被删掉后变成白色方块。
        GameObject menu = FindMenu();
        if (menu == null)
        {
            Debug.LogWarning("场景里找不到 Rescue Menu。");
            return;
        }

        Assign(menu, "Emblem", "Assets/Art/Ui/emblem.png");
        Assign(menu, "CharacterCard", "Assets/Art/card/Character Card/Character Card-Teacher.png");
        Assign(menu, "MonsterCard", "Assets/Art/card/Monster Card/Two-Faced.png");
        Assign(menu, "MemoryCard", "Assets/Art/card/Recollection Playback/1.png");

        EditorSceneManager.MarkSceneDirty(menu.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("素材比例与封面已修复。");
    }

    private static GameObject FindMenu()
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Rescue Menu") return t.gameObject;
        }
        return null;
    }

    private static void Assign(GameObject menu, string childName, string path)
    {
        RawImage image = null;
        foreach (RawImage r in menu.GetComponentsInChildren<RawImage>(true))
            if (r.name == childName) image = r;
        if (image == null)
        {
            Debug.LogWarning("封面里找不到 " + childName);
            return;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogWarning("加载贴图失败：" + path);
            return;
        }

        RectTransform rect = image.rectTransform;
        Vector3 keep = rect.position;
        float maxWidth = rect.sizeDelta.x;
        float maxHeight = rect.sizeDelta.y;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);

        float scale = Mathf.Min(maxWidth / texture.width, maxHeight / texture.height);
        rect.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
        rect.position = keep;

        image.texture = texture;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        image.color = Color.white;
        EditorUtility.SetDirty(image);
    }
}
