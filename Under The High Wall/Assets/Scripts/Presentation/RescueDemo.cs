using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UI = UnityEngine.UI;

// 挂在场景中的入口脚本：引用场景中已保存的 3D 棋盘和界面，把鼠标操作交给 RescueRun，再显示结果。
// Awake 初始化游戏状态，Update 处理点击，LateUpdate 对齐文字，Refresh 在操作后刷新状态。
public sealed class RescueDemo : MonoBehaviour
{
    // 界面与棋盘配色：青色表示当前位置或入口，金色表示可走地块，红色表示危险。
    private static readonly Color Background = new(0.06f, 0.09f, 0.12f);
    private static readonly Color Panel = new(0.12f, 0.17f, 0.21f);
    private static readonly Color Teal = new(0.19f, 0.42f, 0.42f);
    private static readonly Color Amber = new(0.48f, 0.32f, 0.15f);
    private static readonly Color Red = new(0.43f, 0.19f, 0.23f);

    // 保存每格的模型和文字，刷新时直接使用，不必反复查找场景对象。
    [SerializeField] private Renderer[] tiles = new Renderer[RescueRun.Size * RescueRun.Rows];
    [SerializeField] private UI.Text[] tileLabels = new UI.Text[RescueRun.Size * RescueRun.Rows];
    private MaterialPropertyBlock tileColor;
    [SerializeField] private RectTransform map;
    [SerializeField] private Camera boardCamera;
    [SerializeField] private Transform playerPawn;
    [SerializeField] private Transform girlPawn;
    // 当前这一局的规则数据；后面的字段保存界面控件和菜单是否已开始的状态。
    private RescueRun run = new();

    [SerializeField] private UI.Text objective;
    [SerializeField] private UI.Text status;
    [SerializeField] private UI.Text message;
    [SerializeField] private UI.Text battleStatus;
    // 手牌栏：探索时显示在右侧面板底部，战斗时显示在战斗面板里。
    [SerializeField] private UI.Button[] handButtons = new UI.Button[RescueRun.HandLimit];
    [SerializeField] private UI.Text[] handLabels = new UI.Text[RescueRun.HandLimit];
    [SerializeField] private UI.Button[] battleHandButtons = new UI.Button[RescueRun.HandLimit];
    [SerializeField] private UI.Text[] battleHandLabels = new UI.Text[RescueRun.HandLimit];
    // 手牌卡面：把作品集第 15 页裁出来的道具图铺在卡槽里。
    [SerializeField] private UI.RawImage[] handArt = new UI.RawImage[RescueRun.HandLimit];
    [SerializeField] private UI.RawImage[] battleHandArt = new UI.RawImage[RescueRun.HandLimit];
    [SerializeField] private Texture2D[] propArtTextures;
    // 身份牌：开局抽两张，界面显示头像、名字和技能，右下角技能按钮随时可用。
    [SerializeField] private UI.RawImage[] partyArt = new UI.RawImage[2];
    [SerializeField] private UI.Image[] activeFrames = new UI.Image[2];
    [SerializeField] private UI.Text[] partyNames = new UI.Text[2];
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private UI.Text guideTitle;
    [SerializeField] private UI.Text guideHint;
    [SerializeField] private UI.Button skill;
    [SerializeField] private UI.Text skillLabel;
    [SerializeField] private UI.Button battleSkill;
    [SerializeField] private GameObject identityOverlay;
    [SerializeField] private UI.RawImage[] identityArt = new UI.RawImage[2];
    [SerializeField] private UI.Text[] identityNames = new UI.Text[2];
    [SerializeField] private UI.Text[] identitySkills = new UI.Text[2];
    [SerializeField] private UI.Button identityContinue;
    // 六张角色卡贴图，顺序与 IdentityInfo.ArtIndex 一致。
    [SerializeField] private Texture2D[] identityArtTextures;
    private Coroutine identityRoutine;
    [SerializeField] private UI.Button roll;
    [SerializeField] private UI.Button memory;
    [SerializeField] private GameObject battle;
    [SerializeField] private GameObject menu;
    [SerializeField] private UI.Text menuTitle;
    [SerializeField] private UI.Text menuBody;
    [SerializeField] private UI.Text startLabel;
    private bool started;
    [SerializeField] private Transform secondPawn;
    [SerializeField] private UI.Button endTurn;
    [SerializeField] private UI.Button support;
    [SerializeField] private UI.RawImage monsterPortrait;
    [SerializeField] private Texture2D[] monsterArt;
    [SerializeField] private UI.Text enemyTitle;
    // 卡背已保存在场景中，落点翻开时隐藏卡背，露出下方事件卡面。
    [SerializeField] private Renderer[] cardBacks;
    [Header("镜头：待机全图，行动聚焦")]
    [SerializeField, Min(1f)] private float actionCameraSize = 2.8f;
    // 换人镜头推移的进度：0 表示正常跟随，1 表示看全景。
    private float turnSweep;
    [SerializeField, Min(.1f)] private float cameraFollowSpeed = 6f;

    // Unity 加载组件时调用一次，读取场景引用并初始化游戏状态。
    private void Awake()
    {
        tileColor = new MaterialPropertyBlock();
        Refresh();
    }

    // 由场景中按钮保存的 On Click 事件调用。开局先抽身份牌，再进入棋盘。
    public void StartRescue()
    {
        run = new RescueRun();
        started = true;
        Refresh();
        ShowIdentityDraw();
    }

    public void RestartRescue()
    {
        run = new RescueRun();
        Refresh();
        ShowIdentityDraw();
    }

    // 使用当前角色的身份技能，探索和战斗共用一个入口。
    public void UseSkill()
    {
        int before = run.HandCount;
        run.UseSkill();
        Refresh();
        FlashNewCards(before);
    }

    // 开局抽身份：先快速滚动六张卡面，再停在本次抽到的两张上。
    private void ShowIdentityDraw()
    {
        if (guidePanel != null) guidePanel.SetActive(false);
        if (identityOverlay == null || identityArtTextures == null || identityArtTextures.Length < 6) return;
        identityOverlay.SetActive(true);
        if (identityContinue != null) identityContinue.interactable = false;
        if (identityRoutine != null) StopCoroutine(identityRoutine);
        identityRoutine = StartCoroutine(CycleIdentityCards());
    }

    private System.Collections.IEnumerator CycleIdentityCards()
    {
        for (int tick = 1; tick <= 14; tick++)
        {
            ShowIdentityCard(0, (Identity)((tick * 5 + 1) % 6));
            ShowIdentityCard(1, (Identity)((tick * 3 + 4) % 6));
            yield return new WaitForSecondsRealtime(.07f);
        }
        ShowIdentityCard(0, run.IdentityOf(0));
        ShowIdentityCard(1, run.IdentityOf(1));
        if (identityContinue != null) identityContinue.interactable = true;
        identityRoutine = null;
    }

    private void ShowIdentityCard(int index, Identity id)
    {
        if (identityArt.Length <= index || identityArt[index] == null) return;
        Texture2D art = identityArtTextures[IdentityInfo.ArtIndex(id)];
        identityArt[index].texture = art;
        // 六张卡的裁切高度略有差别，按贴图比例调整卡面高度，避免拉伸。
        if (art != null)
        {
            var rect = identityArt[index].rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.x * art.height / art.width);
        }
        if (identityNames.Length > index && identityNames[index] != null)
            identityNames[index].text = IdentityInfo.Name(id);
        if (identitySkills.Length > index && identitySkills[index] != null)
            identitySkills[index].text = IdentityInfo.Skill(id) + "\n" + IdentityInfo.Effect(id);
    }

    public void ContinueAfterIdentity()
    {
        if (identityOverlay != null) identityOverlay.SetActive(false);
        if (guidePanel != null) guidePanel.SetActive(true);
        Refresh();
    }
    public void RollDice() { run.Roll(Random.Range(1, 7)); Refresh(); }
    public void UseMemoryCard() { run.UseMemory(); Refresh(); }
    public void AttackEnemy() { run.攻击(); Refresh(); }
    // 控制当前角色结束回合，或在战斗中选择防守、支援和撤退。
    public void EndPlayerTurn()
    {
        run.EndTurn();
        Refresh();
        StartCoroutine(TurnSweep());
    }
    public void GuardEnemy() { run.Guard(); Refresh(); }
    public void SupportAlly() { run.Support(); Refresh(); }
    public void RetreatBattle() { run.Retreat(); Refresh(); }
    // 四个手牌按钮分别对应第 0–3 张牌；探索时用净化卡，战斗时用攻击/护盾卡。
    public void UseHandCard0() => PlayHandCard(0);
    public void UseHandCard1() => PlayHandCard(1);
    public void UseHandCard2() => PlayHandCard(2);
    public void UseHandCard3() => PlayHandCard(3);
    public void UseBattleCard0() => PlayHandCard(0);
    public void UseBattleCard1() => PlayHandCard(1);
    public void UseBattleCard2() => PlayHandCard(2);
    public void UseBattleCard3() => PlayHandCard(3);
    private void PlayHandCard(int index) { run.PlayCard(index); Refresh(); }

    // 编辑器里停止试玩；打包后的游戏则关闭程序。
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 只覆盖该模型的颜色，不为每个格子复制材质。两个属性名适配常见渲染管线材质。
    private void Paint(Renderer piece, Color color)
    {
        tileColor.SetColor("_BaseColor", color);
        tileColor.SetColor("_Color", color);
        piece.SetPropertyBlock(tileColor);
    }


    // 卡面按原图比例完整放进卡槽，不裁切也不拉伸，两侧留空。
    private static void FitInsideSlot(UI.RawImage image, Texture2D texture)
    {
        if (texture == null) return;
        float maxWidth = 140f, maxHeight = 150f;
        if (image.transform.parent is RectTransform slot && slot.rect.width > 1f)
        {
            maxWidth = slot.rect.width;
            maxHeight = slot.rect.height;
        }
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        float scale = Mathf.Min(maxWidth / texture.width, maxHeight / texture.height);
        rect.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
    }

    private static void ToggleCardStrip(UI.RawImage image, bool hasCard)
    {
        Transform strip = image.transform.parent != null ? image.transform.parent.Find("文字底") : null;
        if (strip != null) strip.gameObject.SetActive(!hasCard);
    }

    // 手牌颜色：攻击红、护盾蓝、净化绿，空槽保持灰色。
    private static Color CardColor(int kind) => kind switch
    {
        0 => new Color(.55f, .16f, .13f, 1f),
        1 => new Color(.16f, .30f, .50f, 1f),
        2 => new Color(.16f, .40f, .26f, 1f),
        _ => new Color(.34f, .35f, .38f, .7f)
    };

    // 每帧检查一次鼠标按下；菜单和战斗打开时，禁止操作棋盘。
    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || run.InBattle || menu.activeSelf ||
            (identityOverlay != null && identityOverlay.activeSelf))
            return;

        Vector2 cursor = mouse.position.ReadValue();
        // 鼠标压在按钮或手牌上时不操作棋盘，避免点按钮同时把棋子走出去。
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (!boardCamera.pixelRect.Contains(cursor))
            return;

        // 从鼠标位置发出相机射线，与高度为 0 的地面求交，得到点中的世界坐标。
        var surface = new Plane(Vector3.up, Vector3.zero);
        Ray ray = boardCamera.ScreenPointToRay(cursor);
        if (!surface.Raycast(ray, out float distance))
            return;

        Vector3 point = ray.GetPoint(distance);
        // 四舍五入得到格子编号。0.47 是地块宽度 0.94 的一半，点击缝隙不会移动。
        int x = Mathf.RoundToInt(point.x);
        int y = Mathf.RoundToInt(-point.z);
        if (Mathf.Abs(point.x - x) > 0.47f || Mathf.Abs(point.z + y) > 0.47f || !run.CanMove(x, y))
            return;

        int before = run.HandCount;
        run.Move(x, y);
        Refresh();
        FlashNewCards(before);
    }

    // 在本帧普通更新之后，根据界面位置调整相机视口，并让格子文字跟随 3D 地块。
    private void LateUpdate()
    {
        // Overlay Canvas 不需要 UI 相机，因此转换坐标时传 null。
        Vector2 lower = RectTransformUtility.WorldToScreenPoint(null, map.TransformPoint(0, -map.rect.height, 0));
        Vector2 upper = RectTransformUtility.WorldToScreenPoint(null, map.TransformPoint(map.rect.width, 0, 0));
        boardCamera.pixelRect = Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y);
        if (upper.y > lower.y)
            boardCamera.aspect = (upper.x - lower.x) / (upper.y - lower.y);
        UpdateCamera();
        for (int y = 0; y < RescueRun.Rows; y++)
        for (int x = 0; x < RescueRun.Size; x++)
        {
            Vector3 point = boardCamera.WorldToScreenPoint(new Vector3(x, 0.85f, -y - 0.18f));
            // 聚焦时远处的文字不能飘出棋盘区域，避免盖住状态面板。
            tileLabels[y * RescueRun.Size + x].enabled = point.z > 0 &&
                point.x > lower.x + 30 && point.x < upper.x - 30 &&
                point.y > lower.y + 15 && point.y < upper.y - 15;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(map, point, null, out Vector2 local);
            tileLabels[y * RescueRun.Size + x].rectTransform.anchoredPosition = local + new Vector2(-52, 15);
        }
    }

    // 根据投影后的棋盘宽高自动计算全景尺寸，窗口变窄时也能看全地图。
    // 还有行动步数或正在战斗时跟随当前玩家；其他时候回到地图中心。
    private void UpdateCamera()
    {
        bool acting = started && !run.Won && !run.Lost && (run.Steps > 0 || run.InBattle);
        float centerX = (RescueRun.Size - 1) * .5f;
        float centerZ = (RescueRun.Rows - 1) * .5f;
        // 牌立在 0.9 高的柱子上，取景要把这段高度算进去，否则上方两排会被裁掉。
        const float lift = .9f;
        Vector3 middle = new Vector3(centerX, .1f + lift * .66f, -centerZ);
        Vector3 target = acting ? new Vector3(run.X, .2f + lift * .66f, -run.Y) : middle;
        float extentX = RescueRun.Size * .5f + .4f;
        float extentZ = RescueRun.Rows * .5f + .4f;
        Vector3 right = boardCamera.transform.right;
        Vector3 up = boardCamera.transform.up;
        float width = extentX * (Mathf.Abs(right.x) + Mathf.Abs(right.z));
        float height = extentZ * (Mathf.Abs(up.x) + Mathf.Abs(up.z)) + .5f + lift * 1.25f;
        float overviewSize = Mathf.Max(height, width / Mathf.Max(.1f, boardCamera.aspect));
        // 换人瞬间先把镜头拉回全景看一眼棋盘，再落回当前角色。
        Vector3 look = turnSweep > 0f
            ? Vector3.Lerp(target, middle, turnSweep)
            : target;
        float size = acting ? actionCameraSize : overviewSize;
        if (turnSweep > 0f) size = Mathf.Lerp(size, overviewSize, turnSweep);
        float blend = 1f - Mathf.Exp(-cameraFollowSpeed * Time.unscaledDeltaTime);
        boardCamera.transform.position = Vector3.Lerp(boardCamera.transform.position,
            look - boardCamera.transform.forward * 18f, blend);
        boardCamera.orthographicSize = Mathf.Lerp(boardCamera.orthographicSize, size, blend);
    }


    // 抽到新牌时让卡槽横向展开，做出“翻牌”的感觉。
    private void FlashNewCards(int before)
    {
        for (int i = before; i < run.HandCount && i < handButtons.Length; i++)
            if (handButtons[i] != null)
                StartCoroutine(FlipSlot(handButtons[i].transform as RectTransform));
    }

    private IEnumerator FlipSlot(RectTransform slot)
    {
        if (slot == null) yield break;
        float t = 0f;
        while (t < .22f)
        {
            t += Time.unscaledDeltaTime;
            slot.localScale = new Vector3(Mathf.Lerp(.1f, 1f, Mathf.Clamp01(t / .22f)), 1f, 1f);
            yield return null;
        }
        slot.localScale = Vector3.one;
    }

    // 换人时镜头先拉远，再回到当前角色。
    private IEnumerator TurnSweep()
    {
        float t = 0f;
        while (t < .28f)
        {
            t += Time.unscaledDeltaTime;
            turnSweep = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .28f));
            yield return null;
        }
        t = 0f;
        while (t < .34f)
        {
            t += Time.unscaledDeltaTime;
            turnSweep = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .34f));
            yield return null;
        }
        turnSweep = 0f;
    }

    // 把当前规则数据同步到菜单、按钮、角色位置和地块颜色；每次有效操作后调用。
    private void Refresh()
    {
        menu.SetActive(!started || run.Won || run.Lost);
        // 主菜单或抽身份面板在前面时，左侧引导栏不该露出来。
        if (guidePanel != null)
            guidePanel.SetActive(!menu.activeSelf && (identityOverlay == null || !identityOverlay.activeSelf));
        menuTitle.text = !started ? "高墙之下" : run.Won ? "她安全回来了" : "迷失高墙之下";
        menuBody.text = !started
            ? "找到失踪的女孩，带她安全回家。\n开局掷骰定先后，走完步数才翻牌。\n教室抽道具卡，医务室降污染，陷阱扣血。\n遇怪时双方掷骰对抗，手牌可加攻击或护盾。\n接到女孩后地图重洗，护送她回到入口。"
            : run.Won
                ? "你穿过了通道，将女孩安全带回。\n\n回忆守护着她，走过黑暗。\n再次开始，踏上新的救援旅程。"
                : run.Message + "\n\n污染使本次救援失败。\n护送遇袭后，可用回忆卡降低污染。\n小心岔路上的陷阱。";
        startLabel.text = started ? "再试一次" : "开始救援";
        objective.text = run.Won ? "救援成功"
            : run.Lost ? "救援失败"
            : run.Escorting ? "护送 · 带女孩返回入口"
            : "探索 · 寻找失踪的女孩";
        status.text = $"行动：{run.PlayerName(run.ActivePlayer)}  骰子 {run.LastRoll} / 余步 {run.Steps}\n" +
                      $"{run.PlayerName(0)} {run.Pollution(0)}/{RescueRun.PlayerLimit}   {run.PlayerName(1)} {run.Pollution(1)}/{RescueRun.PlayerLimit}\n" +
                      $"女孩污染 {run.GirlPollution}/{RescueRun.GirlLimit}   回忆卡 {run.Memories}   手牌 {run.HandCount}/{RescueRun.HandLimit}";
        // 两处手牌栏显示同一份手牌：探索时在右侧，战斗时在战斗面板里。
        for (int i = 0; i < RescueRun.HandLimit; i++)
        {
            string label = run.HandLabel(i);
            bool usable = run.CanPlayCard(i);
            Color cardColor = CardColor(run.HandKind(i));
            int art = run.HandArt(i);
            if (handArt.Length > i && handArt[i] != null)
            {
                handArt[i].enabled = art >= 0;
                handArt[i].uvRect = new Rect(0f, 0f, 1f, 1f);
                if (art >= 0 && propArtTextures != null && propArtTextures.Length > art)
                {
                    Texture2D card = propArtTextures[art];
                    handArt[i].texture = card;
                    FitInsideSlot(handArt[i], card);
                }
                // 有卡面时把中文条收起来，避免盖住卡片本身的名称与数值。
                ToggleCardStrip(handArt[i], art >= 0);
            }
            if (battleHandArt.Length > i && battleHandArt[i] != null)
            {
                battleHandArt[i].enabled = art >= 0;
                if (art >= 0 && propArtTextures != null && propArtTextures.Length > art)
                    battleHandArt[i].texture = propArtTextures[art];
            }
            if (handLabels.Length > i && handLabels[i] != null)
            {
                handLabels[i].text = label;
                handLabels[i].enabled = art < 0;
                handButtons[i].interactable = usable;
                var image = handButtons[i].GetComponent<UI.Image>();
                if (image != null) image.color = cardColor;
            }
            if (battleHandLabels.Length > i && battleHandLabels[i] != null)
            {
                battleHandLabels[i].text = label;
                battleHandButtons[i].interactable = usable;
                var image = battleHandButtons[i].GetComponent<UI.Image>();
                if (image != null) image.color = cardColor;
            }
        }
        endTurn.interactable = run.CanEndTurn;
        support.interactable = run.CanSupport;
        message.text = run.Message;
        roll.interactable = run.CanRoll;
        memory.interactable = run.CanUseMemory;

        // 身份头像、名字和技能按钮跟着当前局面刷新。
        for (int i = 0; i < 2; i++)
        {
            if (partyArt.Length > i && partyArt[i] != null)
            {
                partyArt[i].texture = identityArtTextures != null && identityArtTextures.Length > 0
                    ? identityArtTextures[IdentityInfo.ArtIndex(run.IdentityOf(i))]
                    : null;
                // 头像只取卡面上半部分，避免把名字和技能说明一起塞进小框。
                partyArt[i].uvRect = new Rect(0.06f, 0.42f, 0.88f, 0.58f);
                // 当前行动者亮，队友压暗，一眼能看出轮到谁。
                partyArt[i].color = run.ActivePlayer == i ? Color.white : new Color(.55f, .55f, .6f, 1f);
                // 行动中的角色头像外面套一个橘黄色框。
                if (activeFrames.Length > i && activeFrames[i] != null)
                    activeFrames[i].enabled = run.ActivePlayer == i;
            }
            if (partyNames.Length > i && partyNames[i] != null)
                partyNames[i].text = run.PlayerName(i) + "\n污染 " + run.Pollution(i) + "/" + RescueRun.PlayerLimit;
        }
        if (skillLabel != null) skillLabel.text = "技能：" + run.SkillLabel(run.ActivePlayer);
        if (battleSkill != null)
        {
            battleSkill.interactable = run.CanUseSkill;
            var battleSkillText = battleSkill.GetComponentInChildren<UI.Text>();
            if (battleSkillText != null) battleSkillText.text = "技能：" + run.SkillLabel(run.ActivePlayer);
        }
        if (skill != null) skill.interactable = run.CanUseSkill;

        // 两个角色在同一格内稍微错开；女孩被找到后才显示。
        playerPawn.position = new Vector3(run.PlayerX(0) - .17f, 1.18f, -run.PlayerY(0) + .14f);
        secondPawn.position = new Vector3(run.PlayerX(1) + .17f, 1.18f, -run.PlayerY(1) + .14f);
        playerPawn.gameObject.SetActive(run.Pollution(0) < RescueRun.PlayerLimit);
        secondPawn.gameObject.SetActive(run.Pollution(1) < RescueRun.PlayerLimit);
        playerPawn.localScale = run.ActivePlayer == 0 ? new Vector3(.36f,.4f,.36f) : new Vector3(.29f,.34f,.29f);
        secondPawn.localScale = run.ActivePlayer == 1 ? new Vector3(.36f,.4f,.36f) : new Vector3(.29f,.34f,.29f);
        girlPawn.position = new Vector3(run.GirlX, 1.08f, -run.GirlY - .2f);
        girlPawn.gameObject.SetActive(run.GirlFound);
        for (int y = 0; y < RescueRun.Rows; y++)
        for (int x = 0; x < RescueRun.Size; x++)
        {
            char tile = run.Tile(x, y);
            cardBacks[y * RescueRun.Size + x].enabled = tile == '?';
            bool here = run.X == x && run.Y == y;
            Paint(tiles[y * RescueRun.Size + x], here ? Teal : run.CanMove(x, y) ? Amber : tile switch
            {
                '#' => Panel,
                '?' => new Color(0.19f, 0.23f, 0.31f),
                'E' => Teal,
                'A' or '1' or '2' or '3' or '4' => Red,
                'C' => new Color(0.42f, 0.36f, 0.18f),
                'V' => new Color(0.20f, 0.38f, 0.36f),
                _ => new Color(0.26f, 0.32f, 0.36f)
            });
            tileLabels[y * RescueRun.Size + x].text = tile switch
            {
                '#' => "",
                'E' => "入口",
                'T' => "陷阱",
                'A' => "陷阱",
                'C' => "教室",
                'V' => "医务",
                '1' => "蜘蛛",
                '2' => "稻草人",
                '3' => "双面人",
                '4' => "艺术家",
                'G' => "女孩",
                '?' => "",
                _ => ""
            };
        }

        battle.SetActive(run.InBattle);
        if (run.InBattle)
        {
            enemyTitle.text = "遭遇 · " + run.MonsterName;
            monsterPortrait.texture = monsterArt[run.MonsterId];
            // 立绘框只显示怪物卡的插画部分，卡名和数值留在战斗文字里。
            monsterPortrait.uvRect = new Rect(.06f, .34f, .88f, .60f);
            battleStatus.text = $"{run.MonsterName}  {run.MonsterHealth}/{run.MonsterMaxHealth}   攻击 {run.MonsterAttack}\n" +
                $"你：攻击力 {RescueRun.BaseAttack}   本回合手牌加成 +{run.PendingAttack}\n" +
                $"{run.BattleLog}";
        }

        RefreshGuide();
    }

    // 新手引导：按当前局面提示下一步做什么，避免玩家卡在原地。
    private void RefreshGuide()
    {
        if (guideTitle == null || guideHint == null) return;
        int step;
        string hint;
        if (run.Won || run.Lost)
        {
            step = 4;
            hint = run.Won ? "救援成功。按「重新开始」可以再来一局。" : "救援失败。按「重新开始」再试一次。";
        }
        else if (run.InBattle)
        {
            step = 3;
            hint = "先点手牌里的攻击或护盾卡，再点「攻击（掷骰对抗）」。";
        }
        else if (run.GirlFound)
        {
            step = 4;
            hint = "带着女孩往「入口」走，途中别让她被打到。";
        }
        else if (run.Steps > 0)
        {
            step = 2;
            hint = "点高亮的格子逐格移动，走完步数才会翻开落点。";
        }
        else
        {
            step = 1;
            hint = "点右下角的「投掷骰子」开始这一回合。";
        }
        guideTitle.text = $"新手引导 {step}/4";
        guideHint.text = hint;
    }

}
