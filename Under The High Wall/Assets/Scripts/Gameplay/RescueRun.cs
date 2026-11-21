using System;
using System.Collections.Generic;

// 道具卡：战斗时加攻击或护盾，也可以直接净化污染。
internal enum CardKind { Attack, Shield, Cleanse }

internal readonly struct PropCard
{
    public readonly string Name;
    public readonly CardKind Kind;
    public readonly int Value;
    // 对应 Assets/Art/Props 里裁出来的卡面序号。
    public readonly int ArtIndex;

    public PropCard(string name, CardKind kind, int value, int artIndex)
    {
        Name = name;
        Kind = kind;
        Value = value;
        ArtIndex = artIndex;
    }

    // 卡面第二行文字，直接显示在界面按钮上。
    public string Effect => Kind switch
    {
        CardKind.Attack => "攻击 +" + Value,
        CardKind.Shield => "护盾 +" + Value,
        _ => "污染 -" + Value
    };
}

// 身份牌：对应策划案第 8 节的六位角色，能力沿用作品集里的六张角色卡。
internal enum Identity { Roommate, Teacher, Guard, Mother, Doctor, Confidant }

internal static class IdentityInfo
{
    public static string Name(Identity id) => id switch
    {
        Identity.Roommate => "室友",
        Identity.Teacher => "老师",
        Identity.Guard => "保安",
        Identity.Mother => "妈妈",
        Identity.Doctor => "医生",
        _ => "挚友"
    };

    public static string Skill(Identity id) => id switch
    {
        Identity.Roommate => "可分享的护盾",
        Identity.Teacher => "开局的准备",
        Identity.Guard => "钥匙串",
        Identity.Mother => "闪避",
        Identity.Doctor => "消除感染",
        _ => "阻止攻击"
    };

    public static string Effect(Identity id) => id switch
    {
        Identity.Roommate => "战斗中给自己或队友加护盾 4，每场战斗一次",
        Identity.Teacher => "开局多抽 2 张道具卡",
        Identity.Guard => "挑 1 张道具卡，并免疫一次陷阱",
        Identity.Mother => "接下来两次污染免疫",
        Identity.Doctor => "自己与队友的污染各 -2",
        _ => "本场战斗怪物攻击 -3，每场一次"
    };

    // 对应 Assets/Art/Characters 里的六张角色卡贴图。
    public static int ArtIndex(Identity id) => id switch
    {
        Identity.Roommate => 0,
        Identity.Teacher => 1,
        Identity.Guard => 2,
        Identity.Mother => 3,
        Identity.Doctor => 4,
        _ => 5
    };
}

// 一局游戏的规则数据。场景只显示结果，不在运行时创建地图或角色对象。
// v0.2：参考《吉星派对》把战斗改成双方掷骰对抗，手里的道具卡可以加骰点。
internal sealed class RescueRun
{
    public const int Size = 8;   // 横向列数
    public const int Rows = 7;   // 纵向行数（去掉原来最下面一排）
    public const int PlayerLimit = 8;
    public const int GirlLimit = 4;
    public const int HandLimit = 4;
    public const int BaseAttack = 2;

    // 通道母版：每局只在这张图上随机撒事件，墙和路保持固定。
    private static readonly string[] Maze =
    {
        "........", ".#.##.#.", "........", ".#.##.#.",
        "........", ".##.#.#.", "........"
    };
    // 以入口为起点到每一格的步数，用来让怪物和女孩离入口远一点。
    private static readonly int[,] Distance = BuildDistance();
    // E 入口，C 教室（抽道具卡），V 医务室（净化污染），A 陷阱，1-4 怪物，G 女孩。
    private string[] map;
    private readonly bool[,] revealed = new bool[Size, Rows];
    private readonly int[] positionsX = { 0, 0 };
    private readonly int[] positionsY = { 0, 0 };
    private readonly int[] pollution = { 0, 0 };
    private readonly List<PropCard> hand = new();
    private readonly Random random = new Random();
    private bool rolled;
    private bool supportUsed;
    private int previousX;
    private int previousY;
    private int carrier = -1;
    private int girlX = Size - 1;
    private int girlY = Rows - 1;

    // 本回合临时加值：打出的攻击卡、护盾卡和队友支援都记在这里，结算后清空。
    private int pendingAttack;
    private int pendingShield;

    // 身份牌：开局从六张里随机抽两张，两名角色各一张。
    private readonly Identity[] identities = new Identity[2];
    private readonly int[] skillLeft = new int[2];
    private int evadeLeft;
    private int trapImmune;
    private int monsterPenalty;

    public int ActivePlayer { get; private set; }
    public int X => positionsX[ActivePlayer];
    public int Y => positionsY[ActivePlayer];
    public int Steps { get; private set; }
    public int LastRoll { get; private set; }
    public int PlayerPollution => pollution[ActivePlayer];
    public int GirlPollution { get; private set; }
    public int Memories { get; private set; }
    public int MonsterHealth { get; private set; }
    public int MonsterId { get; private set; } = -1;
    public int MonsterMaxHealth => MonsterId < 0 ? 0 : new[] { 4, 5, 6, 4 }[MonsterId];
    public string MonsterName => MonsterId < 0 ? "" : new[] { "蜘蛛", "稻草人", "双面人", "艺术家" }[MonsterId];
    // 怪物攻击力参与掷骰对抗，数值越高越容易压过玩家。
    public int MonsterAttack => MonsterId < 0 ? 0 : new[] { 1, 2, 2, 1 }[MonsterId];
    public string MonsterIntent => MonsterId switch
    {
        0 => "缠网：无法撤退",
        2 => "重击：攻击 2",
        _ => "攻击 " + MonsterAttack
    };
    // 战斗日志显示双方骰点和加减值，让玩家看懂这一下为什么赢或输。
    public string BattleLog { get; private set; } = "";
    public int PendingAttack => pendingAttack;
    public bool GirlFound { get; private set; }
    public bool Escorting => carrier >= 0;
    public int GirlX => Escorting ? positionsX[carrier] : girlX;
    public int GirlY => Escorting ? positionsY[carrier] : girlY;
    public bool Won { get; private set; }
    public bool Lost => (pollution[0] >= PlayerLimit && pollution[1] >= PlayerLimit) || GirlPollution >= GirlLimit;
    public bool InBattle => MonsterHealth > 0 && !Lost;
    public bool CanRoll => !Won && !Lost && !InBattle && !rolled && Steps == 0;
    public bool CanEndTurn => !Won && !Lost && !InBattle && rolled;
    public bool CanUseMemory => GirlFound && !Won && !Lost && Memories > 0 && GirlPollution > 0;
    public bool CanSupport => InBattle && !supportUsed && pollution[1 - ActivePlayer] < PlayerLimit &&
        Math.Abs(positionsX[1 - ActivePlayer] - X) + Math.Abs(positionsY[1 - ActivePlayer] - Y) <= 2;
    public int HandCount => hand.Count;
    public string Message { get; private set; } = "老师先行动。投掷骰子，寻找女孩；回合结束后切换队友。";
    public int PlayerX(int index) => positionsX[index];
    public int PlayerY(int index) => positionsY[index];
    public int Pollution(int index) => pollution[index];
    public string PlayerName(int index) => IdentityInfo.Name(identities[index]);
    public Identity IdentityOf(int index) => identities[index];
    public string SkillName(int index) => IdentityInfo.Skill(identities[index]);
    public string SkillEffect(int index) => IdentityInfo.Effect(identities[index]);
    public int SkillLeftCount(int index) => skillLeft[index];

    // 技能按钮上的文字，剩余次数用完后也能看出这个技能是什么。
    public string SkillLabel(int index) => skillLeft[index] > 0
        ? $"{SkillName(index)}（剩 {skillLeft[index]} 次）"
        : $"{SkillName(index)}（已用完）";

    public bool CanUseSkill
    {
        get
        {
            if (Won || Lost || skillLeft[ActivePlayer] <= 0) return false;
            switch (identities[ActivePlayer])
            {
                case Identity.Teacher: return hand.Count < HandLimit;
                case Identity.Doctor: return pollution[0] > 0 || pollution[1] > 0;
                case Identity.Roommate:
                case Identity.Confidant: return InBattle;
                default: return true;
            }
        }
    }

    // 使用身份技能：效果写进消息和战斗日志，让玩家知道发生了什么。
    public void UseSkill()
    {
        if (!CanUseSkill) return;
        int me = ActivePlayer;
        switch (identities[me])
        {
            case Identity.Roommate:
                pendingShield += 4;
                BattleLog = "室友举起护盾：本回合护盾 +4。";
                Message = "室友把护盾分给了你，本回合护盾 +4。";
                break;
            case Identity.Teacher:
                DrawCard();
                DrawCard();
                Message = "老师早有准备，多抽了 2 张道具卡。";
                break;
            case Identity.Guard:
                DrawCard();
                trapImmune = 1;
                Message = "保安挑了一张道具卡，还能替你拆掉一次陷阱。";
                break;
            case Identity.Mother:
                evadeLeft += 2;
                Message = "妈妈护住了你，接下来两次污染免疫。";
                break;
            case Identity.Doctor:
                pollution[0] = Math.Max(0, pollution[0] - 2);
                pollution[1] = Math.Max(0, pollution[1] - 2);
                Message = "医生处理了伤口，两人污染各 -2。";
                break;
            case Identity.Confidant:
                monsterPenalty = 3;
                BattleLog = "挚友挡在前面：本场怪物攻击 -3。";
                Message = "挚友帮你挡住了怪物，下次对抗怪物攻击 -3。";
                break;
        }
        skillLeft[me]--;
    }

    public RescueRun()
    {
        // 每局重新随机：怪物种类、怪物位置、女孩位置、教室和医务室都重排。
        var monsterKinds = new List<char> { '1', '2', '3', '4' };
        Shuffle(monsterKinds);
        BuildMap(monsterKinds, true);
        // 开局从六张身份牌里随机抽两张，两名角色各拿一张，互不重复。
        var deck = new List<Identity>
        {
            Identity.Roommate, Identity.Teacher, Identity.Guard,
            Identity.Mother, Identity.Doctor, Identity.Confidant
        };
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        for (int i = 0; i < 2; i++)
        {
            identities[i] = deck[i];
            // 室友和挚友的技能每场战斗刷新，其余整局只有固定次数。
            skillLeft[i] = deck[i] switch
            {
                Identity.Teacher => 1,
                Identity.Guard => 1,
                Identity.Mother => 2,
                Identity.Doctor => 1,
                _ => 0
            };
        }
        // 开局双方各掷一次骰子定先后，同点重掷，避免第一回合固定由老师行动。
        int first = 0, second = 0;
        while (first == second)
        {
            first = random.Next(1, 7);
            second = random.Next(1, 7);
        }
        ActivePlayer = first > second ? 0 : 1;
        Message = $"身份牌：{PlayerName(0)}、{PlayerName(1)}。开局掷骰 {first} 比 {second}，{PlayerName(ActivePlayer)}先手。";
        DrawCard();
    }

    // 隐藏未探索内容，女孩被接走后不再在原格显示。
    public char Tile(int x, int y) => map[y][x] == '#' ? '#' : revealed[x, y] ? map[y][x] : '?';

    public void Roll(int value)
    {
        if (!CanRoll || value < 1 || value > 6) return;
        rolled = true;
        LastRoll = Steps = value;
        Message = $"掷出 {value} 点，走完步数才翻牌。";
        UpdateGirlHint();
    }

    public bool CanMove(int x, int y) => !Won && !Lost && !InBattle && Steps > 0 &&
        x >= 0 && x < Size && y >= 0 && y < Rows && map[y][x] != '#' &&
        Math.Abs(x - X) + Math.Abs(y - Y) == 1;

    public void Move(int x, int y)
    {
        if (!CanMove(x, y)) return;
        previousX = X;
        previousY = Y;
        positionsX[ActivePlayer] = x;
        positionsY[ActivePlayer] = y;
        Steps--;
        Message = Steps > 0 ? "继续移动，只有最终落点翻牌。" : "行动结束，可切换队友。";
        UpdateGirlHint();
        if (Steps > 0) return;
        revealed[x, y] = true;
        char tile = map[y][x];
        if (tile >= '1' && tile <= '4')
        {
            TryEncounterUnderFoot();
        }
        else if (tile == 'G')
        {
            carrier = ActivePlayer;
            SetTile(x, y, '.');
            if (!GirlFound)
            {
                GirlFound = true;
                ShuffleForEscort();
                Message = "接到女孩！事件卡已重洗，队伍会合后护送回入口。";
            }
            else Message = "成功接应女孩，继续护送。";
        }
        else if (tile == 'A')
        {
            SetTile(x, y, '.');
            if (trapImmune > 0)
            {
                trapImmune = 0;
                Message = "保安提前拆掉了陷阱，没有受伤。";
            }
            else
            {
                Message = "踩到陷阱，污染 +1。";
                HurtPlayer(1);
            }
        }
        else if (tile == 'C')
        {
            SetTile(x, y, '.');
            Message = DrawCard()
                ? $"在教室里找到道具：{hand[^1].Name}（{hand[^1].Effect}）。"
                : "教室里的道具已经拿空了。";
        }
        else if (tile == 'V')
        {
            SetTile(x, y, '.');
            if (pollution[ActivePlayer] > 0)
            {
                pollution[ActivePlayer]--;
                Message = "在医务室处理伤口，污染 -1。";
            }
            else Message = "医务室里很安静，污染没有变化。";
        }
        if (carrier == ActivePlayer && X == 0 && Y == 0 && !Lost)
        {
            Won = true;
            Message = "女孩安全撤离。所有退场角色返回表世界恢复。";
        }
    }

    // 抽一张道具卡；手牌满时不再增加，避免溢出界面。
    public bool DrawCard()
    {
        if (hand.Count >= HandLimit) return false;
        var pool = new[]
        {
            new PropCard("匕首", CardKind.Attack, 2, 0),
            new PropCard("斧头", CardKind.Attack, 4, 1),
            new PropCard("破碎玩偶", CardKind.Shield, 2, 2),
            new PropCard("圣水", CardKind.Cleanse, 1, 3),
            new PropCard("圣十字", CardKind.Cleanse, 2, 4)
        };
        int roll = random.Next(100);
        hand.Add(roll < 45 ? pool[0] : roll < 60 ? pool[1] : roll < 80 ? pool[2] : roll < 92 ? pool[3] : pool[4]);
        return true;
    }

    // 手牌第 index 张的显示文字；没有牌时返回“空”。
    public string HandLabel(int index) => index < 0 || index >= hand.Count
        ? "空"
        : hand[index].Name + "\n" + hand[index].Effect;

    public bool CanPlayCard(int index) => index >= 0 && index < hand.Count && !Won && !Lost;

    // 手牌类型：0 攻击、1 护盾、2 净化、-1 空槽。界面据此给卡牌上色。
    public int HandKind(int index) => index < 0 || index >= hand.Count ? -1 : (int)hand[index].Kind;

    // 卡面对应的贴图序号；空槽返回 -1。
    public int HandArt(int index) => index < 0 || index >= hand.Count ? -1 : hand[index].ArtIndex;

    // 打出一张牌：净化卡随时可用，攻击/护盾卡只在战斗里生效。
    public bool PlayCard(int index)
    {
        if (!CanPlayCard(index)) return false;
        PropCard card = hand[index];
        if (card.Kind == CardKind.Cleanse)
        {
            if (pollution[ActivePlayer] <= 0)
            {
                Message = "污染已经是 0，先留着这张牌。";
                return false;
            }
            pollution[ActivePlayer] = Math.Max(0, pollution[ActivePlayer] - card.Value);
            hand.RemoveAt(index);
            Message = $"使用{card.Name}，污染 -{card.Value}。";
            return true;
        }
        if (!InBattle)
        {
            Message = $"{card.Name}要留到战斗里用：{card.Effect}。";
            return false;
        }
        if (card.Kind == CardKind.Attack) pendingAttack += card.Value;
        else pendingShield += card.Value;
        hand.RemoveAt(index);
        Message = $"打出{card.Name}，本回合{(card.Kind == CardKind.Attack ? "攻击" : "护盾")} +{card.Value}。";
        BattleLog = $"打出{card.Name}，{(card.Kind == CardKind.Attack ? "攻击" : "护盾")} +{card.Value}。";
        return true;
    }

    // 完整回合后切换角色；队友已退场时，仍由幸存者继续行动。
    public void EndTurn()
    {
        if (!CanEndTurn) return;
        int left = Steps;
        Steps = 0;
        if (pollution[1 - ActivePlayer] < PlayerLimit) ActivePlayer = 1 - ActivePlayer;
        rolled = false;
        Message = left > 0
            ? $"放弃剩余 {left} 步，轮到{PlayerName(ActivePlayer)}，请掷骰。"
            : $"轮到{PlayerName(ActivePlayer)}，请掷骰。";
        TryEncounterUnderFoot();
    }

    // 站在怪物格上结束回合的人，下一回合继续面对这只怪，不能绕开。
    private void TryEncounterUnderFoot()
    {
        char tile = map[Y][X];
        if (tile < '1' || tile > '4') return;
        MonsterId = tile - '1';
        MonsterHealth = MonsterMaxHealth;
        supportUsed = false;
        pendingAttack = 0;
        pendingShield = 0;
        monsterPenalty = 0;
        if (identities[ActivePlayer] == Identity.Roommate || identities[ActivePlayer] == Identity.Confidant)
            skillLeft[ActivePlayer] = 1;
        BattleLog = $"怪物攻击 {MonsterAttack}，你攻击 {BaseAttack}。打出手牌后掷骰对抗。";
        Message = $"遭遇{MonsterName}。选择攻击、防守或呼唤队友。";
    }

    // 攻击：玩家掷骰 + 攻击力 + 手牌加值，怪物掷骰 + 怪物攻击力，点数高的一方压制对方。
    public void 攻击() => Strike(false);
    public void Guard() => Strike(true);

    private void Strike(bool guarding)
    {
        if (!InBattle) return;
        int roll = random.Next(1, 7);
        int bonus = guarding ? pendingShield : BaseAttack + pendingAttack;
        int player = roll + bonus;
        int monster = Math.Max(1, random.Next(1, 7) + MonsterAttack - monsterPenalty);
        monsterPenalty = 0;
        string mine = guarding && pendingShield > 0
            ? $"{roll}+护盾{pendingShield}={player}"
            : $"{roll}+{BaseAttack}" + (pendingAttack > 0 ? $"+手牌{pendingAttack}" : "") + $"={player}";
        if (guarding)
        {
            if (player >= monster)
            {
                MonsterHealth = Math.Max(0, MonsterHealth - 1);
                BattleLog = $"防守：你 {mine}，怪物 {monster}，格挡成功并反击 1 点。";
                Message = "成功防守，怪物受到 1 点伤害。";
            }
            else
            {
                BattleLog = $"防守：你 {mine}，怪物 {monster}，被压制，污染 +1。";
                Message = "防守失败，污染 +1。";
                HurtPlayer(1);
            }
        }
        else if (player > monster)
        {
            int damage = Math.Min(3, player - monster);
            MonsterHealth = Math.Max(0, MonsterHealth - damage);
            BattleLog = $"攻击：你 {mine}，怪物 {monster}，造成 {damage} 点伤害。";
            Message = $"压制成功，造成 {damage} 点伤害。";
        }
        else if (player < monster)
        {
            BattleLog = $"攻击：你 {mine}，怪物 {monster}，被反击，污染 +1。";
            Message = "攻击被挡下，污染 +1。";
            HurtPlayer(1);
        }
        else
        {
            BattleLog = $"攻击：你 {mine}，怪物 {monster}，僵持，本轮没有伤害。";
            Message = "点数相同，双方僵持。";
        }
        pendingAttack = 0;
        pendingShield = 0;
        if (MonsterHealth <= 0) FinishBattle();
    }

    // 支援只允许两格内的存活队友，每场一次：队友掷一个骰子，加到下一次攻击的点数上。
    public void Support()
    {
        if (!CanSupport) return;
        supportUsed = true;
        int roll = random.Next(1, 7);
        pendingAttack += roll;
        BattleLog = $"队友掷出 {roll} 点，下次攻击 +{roll}。怪物攻击 {MonsterAttack}。";
        Message = $"队友掷出 {roll} 点，已计入下次攻击。";
    }

    // 撤回入战前的位置，消耗该回合并承受 1 点污染；蜘蛛会缠住玩家，不能撤退。
    public void Retreat()
    {
        if (!InBattle) return;
        if (MonsterId == 0)
        {
            Message = "蜘蛛的网缠住了你，无法撤退。";
            BattleLog = "蜘蛛：无法撤退。";
            return;
        }
        MonsterHealth = 0;
        pendingAttack = 0;
        pendingShield = 0;
        Steps = 0;
        Message = "怪物挡住去路，你只能留在原地等下一回合，污染 +1。";
        BattleLog = "撤退失败：无法绕过这只怪物。";
        HurtPlayer(1);
    }

    private bool FinishBattle()
    {
        if (MonsterHealth > 0) return false;
        SetTile(X, Y, '.');
        Memories++;
        pendingAttack = 0;
        pendingShield = 0;
        Message = $"击败{MonsterName}，获得回忆卡 ×1。";
        return true;
    }

    // 携带者受击连带女孩；携带者退场则把女孩留在原地，让另一人接应。
    private void HurtPlayer(int amount)
    {
        if (amount == 0) return;
        // 妈妈的闪避先消耗，挡掉这一次污染。
        if (evadeLeft > 0)
        {
            evadeLeft--;
            Message += " 闪避成功，没有受到污染。";
            return;
        }
        pollution[ActivePlayer] = Math.Min(PlayerLimit, pollution[ActivePlayer] + amount);
        if (carrier == ActivePlayer)
        {
            GirlPollution++;
            Message += " 女孩受击，污染 +1。";
        }
        if (pollution[ActivePlayer] < PlayerLimit) return;
        if (carrier == ActivePlayer)
        {
            girlX = X; girlY = Y; carrier = -1;
            SetTile(X, Y, 'G');
        }
        MonsterHealth = 0;
        Steps = 0;
        Message += $" {PlayerName(ActivePlayer)}退场。";
        if (!Lost)
        {
            ActivePlayer = 1 - ActivePlayer;
            rolled = false;
            Message += $" 由{PlayerName(ActivePlayer)}继续救援。";
        }
    }

    public void UseMemory()
    {
        if (!CanUseMemory) return;
        Memories--;
        GirlPollution--;
        Message = "消耗回忆卡，女孩污染 -1。";
    }

    // 接到女孩后重洗道路：保留还没被击败的怪物，其余事件全部重新随机摆放。
    private void ShuffleForEscort()
    {
        var monsters = new List<char>();
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Size; x++)
            if (map[y][x] >= '1' && map[y][x] <= '4') monsters.Add(map[y][x]);
        Shuffle(monsters);
        BuildMap(monsters, false);
    }

    // 清空旧布局，在通道母版上随机摆放怪物、女孩、教室、医务室和陷阱。
    private void BuildMap(List<char> monsters, bool placeGirl)
    {
        map = (string[])Maze.Clone();
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Size; x++)
            revealed[x, y] = false;

        var cells = new List<int>();
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Size; x++)
        {
            if (map[y][x] == '#') continue;
            if (x == 0 && y == 0) continue;
            if (x == positionsX[0] && y == positionsY[0]) continue;
            if (x == positionsX[1] && y == positionsY[1]) continue;
            cells.Add(y * Size + x);
        }
        Shuffle(cells);

        int cursor = 0;
        foreach (char kind in monsters)
        {
            int index = Take(cells, ref cursor, 3);
            if (index >= 0) SetTile(index % Size, index / Size, kind);
        }
        if (placeGirl)
        {
            int index = Take(cells, ref cursor, 5);
            if (index >= 0)
            {
                girlX = index % Size;
                girlY = index / Size;
                SetTile(girlX, girlY, 'G');
            }
        }
        for (int i = 0; i < 2; i++)
        {
            int index = Take(cells, ref cursor, 0);
            if (index >= 0) SetTile(index % Size, index / Size, 'C');
        }
        for (int i = 0; i < 2; i++)
        {
            int index = Take(cells, ref cursor, 0);
            if (index >= 0) SetTile(index % Size, index / Size, 'V');
        }
        for (int i = 0; i < 3; i++)
        {
            int index = Take(cells, ref cursor, 0);
            if (index >= 0) SetTile(index % Size, index / Size, 'A');
        }
    }

    // 从打乱后的格子里取下第一个离入口足够远的格子。
    private static int Take(List<int> cells, ref int cursor, int minDistance)
    {
        while (cursor < cells.Count)
        {
            int index = cells[cursor++];
            if (minDistance <= 0 || Distance[index % Size, index / Size] >= minDistance) return index;
        }
        return -1;
    }

    // 离女孩越近，提示越清楚。
    private void UpdateGirlHint()
    {
        if (GirlFound || Escorting) return;
        int distance = Math.Abs(girlX - X) + Math.Abs(girlY - Y);
        Message += distance switch
        {
            <= 1 => " 哭声就在隔壁。",
            2 => " 隐约听见女孩在哭。",
            3 => " 远处传来细微的哭声。",
            _ => ""
        };
    }

    // 洗牌，用于怪物种类和格子的随机排列。
    private void Shuffle<TItem>(List<TItem> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 从入口做一次广度优先，算出每格最短步数。
    private static int[,] BuildDistance()
    {
        var dist = new int[Size, Rows];
        for (int y = 0; y < Rows; y++)
        for (int x = 0; x < Size; x++)
            dist[x, y] = -1;

        var queue = new Queue<(int x, int y)>();
        dist[0, 0] = 0;
        queue.Enqueue((0, 0));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var step in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + step.Item1;
                int ny = y + step.Item2;
                if (nx < 0 || nx >= Size || ny < 0 || ny >= Rows) continue;
                if (Maze[ny][nx] == '#' || dist[nx, ny] >= 0) continue;
                dist[nx, ny] = dist[x, y] + 1;
                queue.Enqueue((nx, ny));
            }
        }
        return dist;
    }

    private void SetTile(int x, int y, char value)
    {
        char[] row = map[y].ToCharArray();
        row[x] = value;
        map[y] = new string(row);
    }
}
