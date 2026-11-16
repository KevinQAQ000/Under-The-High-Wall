using System;

internal sealed class RescueRun
{
    public const int Size = 4;
    public const int PlayerLimit = 5;
    public const int GirlLimit = 2;

    private static readonly string[] Map = { "E..#", "##M#", "...G", "T###" };
    private readonly bool[,] revealed = new bool[Size, Size];
    private bool monsterDefeated;
    private bool firstAmbush;
    private bool secondAmbush;

    public int X { get; private set; }
    public int Y { get; private set; }
    public int Steps { get; private set; }
    public int PlayerPollution { get; private set; }
    public int GirlPollution { get; private set; }
    public int Memories { get; private set; }
    public int MonsterHealth { get; private set; }
    public bool Escorting { get; private set; }
    public bool Won { get; private set; }
    public bool Lost => PlayerPollution >= PlayerLimit || GirlPollution >= GirlLimit;
    public bool InBattle => MonsterHealth > 0 && !Lost;
    public bool CanRoll => !Won && !Lost && !InBattle && Steps == 0;
    public bool CanUseMemory => Escorting && !Won && !Lost && Memories > 0 && GirlPollution > 0;
    public string Message { get; private set; } = "投掷骰子，进入通道寻找女孩。";

    public RescueRun()
    {
        revealed[0, 0] = true;
    }

    public char Tile(int x, int y)
    {
        char tile = Map[y][x];
        if (tile == '#' || !revealed[x, y])
            return tile == '#' ? '#' : '?';
        if ((tile == 'M' && monsterDefeated) || (tile == 'G' && Escorting))
            return '.';
        return tile;
    }

    public void Roll(int value)
    {
        if (!CanRoll || value < 1 || value > 6)
            return;

        Steps = value;
        Message = $"掷出了 {value} 点。点击相邻的高亮地块。";
    }

    public bool CanMove(int x, int y) =>
        !InBattle && !Won && !Lost && Steps > 0 &&
        x >= 0 && x < Size && y >= 0 && y < Size &&
        Map[y][x] != '#' && Math.Abs(x - X) + Math.Abs(y - Y) == 1;

    public void Move(int x, int y)
    {
        if (!CanMove(x, y))
            return;

        X = x;
        Y = y;
        Steps--;
        revealed[x, y] = true;
        Message = Steps == 0 ? "步数耗尽，请再次投掷骰子。" : "点击相邻的高亮地块。";

        switch (Map[y][x])
        {
            case 'M' when !monsterDefeated:
                MonsterHealth = 3;
                Message = "暗影挡住了通道。";
                break;
            case 'G' when !Escorting:
                Escorting = true;
                Message = "找到女孩了！请带她返回入口。";
                break;
            case 'E' when Escorting:
                Won = true;
                Message = "你把女孩安全带回了入口。";
                break;
            case 'T':
                PlayerPollution = Math.Min(PlayerLimit, PlayerPollution + 3);
                Message = Lost ? "陷阱的污染吞没了你。" : "触发陷阱，污染增加 3 点。";
                break;
            default:
                if (!Escorting && Math.Abs(3 - X) + Math.Abs(2 - Y) == 1)
                    Message = "附近传来了啜泣声。";
                break;
        }

        if (!Escorting || Won || Lost)
            return;

        // Two separate return hazards make the memory card matter.
        if (x == 2 && y == 2 && !firstAmbush)
        {
            firstAmbush = true;
            HitGirl();
        }
        else if (x == 1 && y == 0 && !secondAmbush)
        {
            secondAmbush = true;
            HitGirl();
        }
    }

    public void 攻击()
    {
        if (!InBattle)
            return;

        MonsterHealth--;
        if (MonsterHealth == 0)
        {
            monsterDefeated = true;
            Memories++;
            Message = "击败暗影，获得一张回忆卡。";
            return;
        }

        PlayerPollution++;
        Message = Lost ? "你被暗影吞没了。" : "暗影反击，你的污染增加 1 点。";
    }

    public void UseMemory()
    {
        if (!CanUseMemory)
            return;

        Memories--;
        GirlPollution--;
        Message = "使用回忆卡，女孩污染降低 1 点。";
    }

    private void HitGirl()
    {
        GirlPollution++;
        Message = Lost
            ? "女孩的污染已达到上限。"
            : "女孩遭到袭击！请使用回忆卡后再前进。";
    }
}
