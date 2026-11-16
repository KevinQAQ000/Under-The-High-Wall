using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UI = UnityEngine.UI;

public sealed class RescueDemo : MonoBehaviour
{
    private static readonly Color Background = new(0.06f, 0.09f, 0.12f);
    private static readonly Color Panel = new(0.12f, 0.17f, 0.21f);
    private static readonly Color Teal = new(0.19f, 0.42f, 0.42f);
    private static readonly Color Amber = new(0.48f, 0.32f, 0.15f);
    private static readonly Color Red = new(0.43f, 0.19f, 0.23f);

    private readonly Renderer[,] tiles = new Renderer[RescueRun.Size, RescueRun.Size];
    private readonly UI.Text[,] tileLabels = new UI.Text[RescueRun.Size, RescueRun.Size];
    private MaterialPropertyBlock tileColor;
    private RectTransform map;
    private Camera boardCamera;
    private Transform playerPawn;
    private Transform girlPawn;
    private RescueRun run = new();
    private Font font;
    private UI.Text objective;
    private UI.Text status;
    private UI.Text message;
    private UI.Text battleStatus;
    private UI.Button roll;
    private UI.Button memory;
    private GameObject battle;
    private GameObject menu;
    private UI.Text menuTitle;
    private UI.Text menuBody;
    private UI.Text startLabel;
    private bool started;

    private void Awake()
    {
        tileColor = new MaterialPropertyBlock();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC" }, 24);
        if (EventSystem.current == null)
        {
            var events = new GameObject("Rescue Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform);
        }

        var screen = new GameObject("Rescue HUD", typeof(Canvas), typeof(UI.CanvasScaler), typeof(UI.GraphicRaycaster));
        screen.transform.SetParent(transform);
        screen.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = screen.GetComponent<UI.CanvasScaler>();
        scaler.uiScaleMode = UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000, 640);
        scaler.screenMatchMode = UI.CanvasScaler.ScreenMatchMode.Expand;

        var stage = NewRect(screen.transform, "Rescue Layout");
        stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = new Vector2(1000, 640);

        Label(stage, "Title", "高墙之下", 34, 45, 23, 910, 46);
        objective = Label(stage, "Objective", "", 22, 45, 74, 910, 35);
        map = Place(NewRect(stage, "3D Board Viewport"), 45, 124, 500, 468);
        BuildBoard();

        var controls = Place(Block(stage, "Controls", Panel).rectTransform, 565, 124, 390, 468);
        status = Label(controls, "Stats", "", 22, 22, 18, 346, 132);
        message = Label(controls, "Event", "", 22, 22, 150, 346, 100);
        (roll, _) = Button(controls, "投掷骰子", 22, 260, 346, 52, () => run.Roll(Random.Range(1, 7)));
        (memory, _) = Button(controls, "使用回忆卡", 22, 323, 346, 52, () => run.UseMemory());
        Button(controls, "重新开始", 22, 386, 346, 52, () => run = new RescueRun());

        var shade = Place(Block(stage, "Battle Overlay", new Color(0.025f, 0.04f, 0.06f, 0.91f), true)
            .rectTransform, 0, 0, 1000, 640);
        battle = shade.gameObject;
        var encounter = Place(Block(shade, "Encounter", Panel).rectTransform, 231, 155, 538, 330);
        Label(encounter, "Enemy", "遭遇暗影", 30, 28, 25, 482, 45);
        battleStatus = Label(encounter, "Battle Stats", "", 22, 28, 82, 482, 137);
        Button(encounter, "攻击", 28, 238, 482, 62, () => run.攻击());
        var menuPanel = Place(Block(stage, "Rescue Menu", Background, true).rectTransform, 0, 0, 1000, 640);
        menu = menuPanel.gameObject;
        menuTitle = Label(menuPanel, "Heading", "", 38, 140, 100, 720, 65);
        menuBody = Label(menuPanel, "Instructions", "", 24, 140, 190, 720, 260);
        (_, startLabel) = Button(menuPanel, "开始救援", 140, 490, 720, 65, () =>
        {
            run = new RescueRun();
            started = true;
        });
        Refresh();
    }

    private void BuildBoard()
    {
        var backgroundCamera = new GameObject("Background Camera", typeof(Camera)).GetComponent<Camera>();
        backgroundCamera.transform.SetParent(transform);
        backgroundCamera.depth = -100;
        backgroundCamera.cullingMask = 0;
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Background;
        boardCamera = Camera.main;
        boardCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
        boardCamera.transform.position = new Vector3(1.5f, 0, -1.5f) - boardCamera.transform.forward * 10;
        boardCamera.orthographic = true;
        boardCamera.orthographicSize = 2.75f;
        boardCamera.clearFlags = CameraClearFlags.SolidColor;
        boardCamera.backgroundColor = Background;

        CreatePiece("Board Base", PrimitiveType.Cube, new Vector3(1.5f, -0.4f, -1.5f),
            new Vector3(4.25f, 0.45f, 4.25f), Panel);
        for (int y = 0; y < RescueRun.Size; y++)
        for (int x = 0; x < RescueRun.Size; x++)
        {
            bool wall = run.Tile(x, y) == '#';
            float height = wall ? 0.8f : 0.24f;
            var piece = CreatePiece(wall ? "Wall" : "Map Card", PrimitiveType.Cube,
                new Vector3(x, height / 2 - 0.24f, -y), new Vector3(0.94f, height, 0.94f), Panel);
            tiles[x, y] = piece.GetComponent<Renderer>();
            tileLabels[x, y] = Label(map, "Card Label", "", 18, 0, 0, 104, 30);
            tileLabels[x, y].alignment = TextAnchor.MiddleCenter;
        }

        playerPawn = CreatePiece("Player", PrimitiveType.Capsule, Vector3.zero,
            new Vector3(0.29f, 0.34f, 0.29f), new Color(0.27f, 0.9f, 0.84f));
        girlPawn = CreatePiece("Girl", PrimitiveType.Capsule, Vector3.zero,
            new Vector3(0.21f, 0.24f, 0.21f), new Color(1f, 0.7f, 0.34f));
    }

    private Transform CreatePiece(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color)
    {
        var piece = GameObject.CreatePrimitive(shape);
        piece.name = name;
        piece.transform.SetParent(transform);
        piece.transform.SetPositionAndRotation(position, Quaternion.identity);
        piece.transform.localScale = scale;
        Destroy(piece.GetComponent<Collider>());
        Paint(piece.GetComponent<Renderer>(), color);
        return piece.transform;
    }

    private void Paint(Renderer piece, Color color)
    {
        tileColor.SetColor("_BaseColor", color);
        tileColor.SetColor("_Color", color);
        piece.SetPropertyBlock(tileColor);
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || run.InBattle || menu.activeSelf)
            return;

        Vector2 cursor = mouse.position.ReadValue();
        if (!boardCamera.pixelRect.Contains(cursor))
            return;

        var surface = new Plane(Vector3.up, Vector3.zero);
        Ray ray = boardCamera.ScreenPointToRay(cursor);
        if (!surface.Raycast(ray, out float distance))
            return;

        Vector3 point = ray.GetPoint(distance);
        int x = Mathf.RoundToInt(point.x);
        int y = Mathf.RoundToInt(-point.z);
        if (Mathf.Abs(point.x - x) > 0.47f || Mathf.Abs(point.z + y) > 0.47f || !run.CanMove(x, y))
            return;

        run.Move(x, y);
        Refresh();
    }

    private void LateUpdate()
    {
        Vector2 lower = RectTransformUtility.WorldToScreenPoint(null, map.TransformPoint(0, -map.rect.height, 0));
        Vector2 upper = RectTransformUtility.WorldToScreenPoint(null, map.TransformPoint(map.rect.width, 0, 0));
        boardCamera.pixelRect = Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
        if (upper.y > lower.y)
            boardCamera.aspect = (upper.x - lower.x) / (upper.y - lower.y);
        for (int y = 0; y < RescueRun.Size; y++)
        for (int x = 0; x < RescueRun.Size; x++)
        {
            Vector3 point = boardCamera.WorldToScreenPoint(new Vector3(x, 0.035f, -y - 0.18f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(map, point, null, out Vector2 local);
            tileLabels[x, y].rectTransform.anchoredPosition = local + new Vector2(-52, 15);
        }
    }

    private void Refresh()
    {
        menu.SetActive(!started || run.Won || run.Lost);
        menuTitle.text = !started ? "高墙之下" : run.Won ? "她安全回来了" : "迷失高墙之下";
        menuBody.text = !started
            ? "找到失踪的女孩，带她安全回家。\n\n投掷骰子，点击相邻的高亮地块移动。\n击败暗影，获得回忆卡。\n留意啜泣提示，寻找女孩。\n遭遇袭击后使用回忆卡保护她。"
            : run.Won
                ? "你穿过了通道，将女孩安全带回。\n\n回忆守护着她，走过黑暗。\n再次开始，踏上新的救援旅程。"
                : run.Message + "\n\n污染使本次救援失败。\n第一次遇袭后，记得使用回忆卡。\n小心岔路上的陷阱。";
        startLabel.text = started ? "再试一次" : "开始救援";
        objective.text = run.Won ? "救援成功"
            : run.Lost ? "救援失败"
            : run.Escorting ? "护送 · 带女孩返回入口"
            : "探索 · 寻找失踪的女孩";
        status.text = $"剩余步数   {run.Steps}\n你的污染   {run.PlayerPollution} / {RescueRun.PlayerLimit}\n" +
                      $"女孩污染   {(run.Escorting ? $"{run.GirlPollution} / {RescueRun.GirlLimit}" : "—")}\n" +
                      $"回忆卡   {run.Memories}";
        message.text = run.Message;
        roll.interactable = run.CanRoll;
        memory.interactable = run.CanUseMemory;

        playerPawn.position = new Vector3(run.X - 0.14f, 0.34f, -run.Y + 0.14f);
        girlPawn.position = new Vector3(run.X + 0.19f, 0.24f, -run.Y + 0.14f);
        girlPawn.gameObject.SetActive(run.Escorting);
        for (int y = 0; y < RescueRun.Size; y++)
        for (int x = 0; x < RescueRun.Size; x++)
        {
            char tile = run.Tile(x, y);
            bool here = run.X == x && run.Y == y;
            Paint(tiles[x, y], here ? Teal : run.CanMove(x, y) ? Amber : tile switch
            {
                '#' => Panel,
                '?' => new Color(0.19f, 0.23f, 0.31f),
                'E' => Teal,
                'T' or 'M' => Red,
                _ => new Color(0.26f, 0.32f, 0.36f)
            });
            tileLabels[x, y].text = tile switch
            {
                '#' => "",
                'E' => "入口",
                'T' => "陷阱",
                'M' => "暗影",
                '?' => "?",
                _ => ""
            };
        }

        battle.SetActive(run.InBattle);
        if (run.InBattle)
            battleStatus.text = $"暗影   {run.MonsterHealth} / 3\n你的污染   {run.PlayerPollution} / {RescueRun.PlayerLimit}\n{run.Message}";
    }

    private (UI.Button, UI.Text) Button(Transform parent, string caption, int x, int y, int w, int h,
        System.Action action)
    {
        var image = Block(parent, caption.Length == 0 ? "Map Tile" : caption, Amber, true);
        Place(image.rectTransform, x, y, w, h);
        var button = image.gameObject.AddComponent<UI.Button>();
        button.targetGraphic = image;
        button.navigation = new UI.Navigation { mode = UI.Navigation.Mode.None };
        var colors = button.colors;
        colors.disabledColor = new Color(0.66f, 0.66f, 0.66f);
        button.colors = colors;
        var label = Label(image.transform, "Label", caption, 20, 0, 0, w, h);
        label.alignment = TextAnchor.MiddleCenter;
        button.onClick.AddListener(() =>
        {
            action();
            Refresh();
        });
        return (button, label);
    }

    private UI.Text Label(Transform parent, string name, string caption, int size, int x, int y, int w, int h)
    {
        var rect = Place(NewRect(parent, name), x, y, w, h);
        var label = rect.gameObject.AddComponent<UI.Text>();
        label.font = font;
        label.text = caption;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = new Color(0.93f, 0.95f, 0.96f);
        label.raycastTarget = false;
        return label;
    }

    private static UI.Image Block(Transform parent, string name, Color color, bool interactive = false)
    {
        var rect = NewRect(parent, name);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = rect.gameObject.AddComponent<UI.Image>();
        image.color = color;
        image.raycastTarget = interactive;
        return image;
    }

    private static RectTransform NewRect(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform Place(RectTransform rect, int x, int y, int w, int h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }
}
