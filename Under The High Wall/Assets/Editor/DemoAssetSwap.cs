using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 把 Assets/Art/card 里的正式素材接回场景：身份卡、怪物立绘、道具卡面、卡背。
// 之前的临时裁图被替换掉后，场景里的贴图引用全部丢了，这里一次性重新指定。
static class DemoAssetSwap
{
    private const string Root = "Assets/Art/card/";

    [MenuItem("高墙之下/替换正式素材")]
    private static void Swap()
    {
        var demo = Object.FindFirstObjectByType<RescueDemo>();
        if (demo == null)
        {
            Debug.LogError("场景里找不到 RescueDemo。");
            return;
        }

        var so = new SerializedObject(demo);

        // 身份卡顺序要和 IdentityInfo.ArtIndex 一致：室友、老师、保安、妈妈、医生、挚友。
        SetTextures(so, "identityArtTextures", new[]
        {
            "Character Card/Character Card-Classmate.png",
            "Character Card/Character Card-teacher.png",
            "Character Card/Character Card-security guard.png",
            "Character Card/Character Card-mother.png",
            "Character Card/Character Card-doctor.png",
            "Character Card/Character Card-confidant.png",
        });

        // 怪物 1-4：蜘蛛、稻草人、双面人、艺术家。双面人用 Two-Faced 原画。
        SetTextures(so, "monsterArt", new[]
        {
            "Monster Card/Spider.png",
            "Monster Card/Scarecrow.png",
            "Monster Card/Two-Faced.png",
            "Monster Card/Artist.png",
        });

        // 道具卡顺序和 HandArt 的 ArtIndex 一致：匕首、斧头、破碎玩偶、圣水、圣十字。
        SetTextures(so, "propArtTextures", new[]
        {
            "Prop Card/Dagger.png",
            "Prop Card/hatchet.png",
            "Prop Card/broken doll.png",
            "Prop Card/holy water.png",
            "Prop Card/Holy cross.png",
        });

        // 卡背：场景里的卡背共用 VenueBack 材质，换掉它的贴图即可。
        var back = Load("Venue Card/0 Venue card back.png");
        var backs = so.FindProperty("cardBacks");
        for (int i = 0; i < backs.arraySize; i++)
        {
            if (!(backs.GetArrayElementAtIndex(i).objectReferenceValue is Renderer r)) continue;
            if (back != null)
            {
                r.sharedMaterial.mainTexture = back;
                EditorUtility.SetDirty(r.sharedMaterial);
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("正式素材已接回：身份卡 6、怪物 4、道具 5、卡背 64。");
    }

    private static void SetTextures(SerializedObject so, string field, string[] paths)
    {
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("找不到字段 " + field);
            return;
        }
        prop.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = Load(paths[i]);
    }

    private static Texture2D Load(string relative)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + relative);
        if (tex == null) Debug.LogError("贴图没找到：" + Root + relative);
        return tex;
    }
}
