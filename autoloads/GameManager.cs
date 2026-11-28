using System;
using System.Linq;
using Godot;

public enum GameState { Playing, Paused, GameOver, Victory }

/// <summary>Estado global da partida: pontos, fichas, tempo, pausa e fim de jogo. A UI só escuta os signals.</summary>
public partial class GameManager : Node
{
    public const string CutsceneScenePath = "res://scenes/Cutscene.tscn";
    public const string LoungeScenePath = "res://scenes/levels/SalaDescanso.tscn";

    public static GameManager Instance { get; private set; }

    /// <summary>Segundos de partida até o Tigrinho aparecer.</summary>
    [Export] public float BossSpawnTime { get; set; } = 300f;
    /// <summary>Pontos que também chamam o Tigrinho antes do tempo (0 = desativado).</summary>
    [Export] public int BossScoreThreshold { get; set; } = 25000;

    [Signal] public delegate void ScoreChangedEventHandler(int score);
    [Signal] public delegate void ChipsChangedEventHandler(int chips);
    [Signal] public delegate void DollarsChangedEventHandler(int dollars);
    [Signal] public delegate void StateChangedEventHandler(int state); // GameState

    public int Score { get; private set; }
    public int Chips { get; private set; }
    /// <summary>$ ganhos nesta partida (vão para o saldo da Loja Externa no fim).</summary>
    public int RunDollars { get; private set; }
    /// <summary>Fichas convertidas em $ no fim da partida (Cash Out, $10 cada).</summary>
    public int CashedOutChips { get; private set; }
    public const int CashOutPerChip = 10;
    public float Elapsed { get; private set; }
    public GameState State { get; private set; }
    public int CurrentStage { get; private set; } = 1;
    public bool InLounge { get; private set; }
    public bool CampaignActive { get; private set; }

    public bool IsBossTime =>
        Elapsed >= BossSpawnTime || (BossScoreThreshold > 0 && Score >= BossScoreThreshold);

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // o jogo pausa via SceneTree.Paused; o manager não

        if (!OS.GetCmdlineUserArgs().Contains("--selfcheck")) return;
        try { SelfCheck.Run(); GD.Print("selfcheck OK"); GetTree().Quit(); }
        catch (Exception e) { GD.PrintErr(e.Message); GetTree().Quit(1); }
    }

    public override void _Process(double delta)
    {
        if (State != GameState.Playing) return;
        Elapsed += (float)delta;
        RunState.Tick((float)delta);
    }

    public void AddScore(int amount)
    {
        Score += amount;
        EmitSignal(SignalName.ScoreChanged, Score);
    }

    public void AddChips(int amount)
    {
        Chips += amount;
        EmitSignal(SignalName.ChipsChanged, Chips);
    }

    public void AddDollars(int amount)
    {
        if (amount <= 0 || State == GameState.GameOver || State == GameState.Victory) return;
        RunDollars += amount;
        EmitSignal(SignalName.DollarsChanged, RunDollars);
    }

    public bool TrySpendChip()
    {
        if (Chips <= 0) return false;
        Chips--;
        EmitSignal(SignalName.ChipsChanged, Chips);
        return true;
    }

    public void SetPaused(bool paused)
    {
        if (State == GameState.GameOver || State == GameState.Victory) return;
        State = paused ? GameState.Paused : GameState.Playing;
        GetTree().Paused = paused;
        EmitSignal(SignalName.StateChanged, (int)State);
    }

    public void GameOver()
    {
        if (End(GameState.GameOver)) AudioManager.Instance?.PlayGameOver();
    }

    public void Victory()
    {
        if (End(GameState.Victory)) AudioManager.Instance?.PlayVictory();
    }

    bool End(GameState s)
    {
        if (State == GameState.GameOver || State == GameState.Victory) return false;
        State = s;
        GetTree().Paused = true;
        // Cash Out (PDF): fichas não gastas viram $ e o total da partida entra no saldo.
        CashedOutChips = Chips;
        RunDollars += Chips * CashOutPerChip;
        MetaProgress.Bank(RunDollars);
        EmitSignal(SignalName.StateChanged, (int)State);
        return true;
    }

    public void ActivateCampaignAt(int stage)
    {
        CurrentStage = Mathf.Clamp(stage, 1, CampaignRules.TotalStages);
        InLounge = false;
        CampaignActive = true;
    }

    public void StartNewRun(bool showCutscene = true)
    {
        Score = Chips = RunDollars = CashedOutChips = 0;
        Elapsed = 0f;
        CurrentStage = 1;
        InLounge = false;
        CampaignActive = true;
        LoungeStation.RouletteUsesInRun = 0;
        RunState.Reset();
        State = GameState.Playing;
        GetTree().Paused = false;
        string target = showCutscene ? CutsceneScenePath : CampaignRules.ForStage(1).ScenePath;
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, target);
    }

    public void AdvanceFromExit()
    {
        if (!CampaignActive)
        {
            Victory();
            return;
        }
        if (InLounge)
        {
            CurrentStage = Mathf.Clamp(CurrentStage + 1, 1, CampaignRules.TotalStages);
            InLounge = false;
            RunState.SavePlayerBuild();
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, CampaignRules.ForStage(CurrentStage).ScenePath);
            return;
        }
        if (CurrentStage >= CampaignRules.TotalStages)
        {
            Victory();
            return;
        }
        RunState.SavePlayerBuild();
        InLounge = true;
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, LoungeScenePath);
    }

    /// <summary>Este nó sobrevive ao reload da cena (autoload), então o estado é zerado à mão.</summary>
    public void Restart()
    {
        string scene = GetTree().CurrentScene?.SceneFilePath;
        if (!CampaignActive && scene == "res://scenes/Main.tscn")
        {
            Score = Chips = 0;
            RunDollars = CashedOutChips = 0;
            Elapsed = 0f;
            RunState.Reset();
            State = GameState.Playing;
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
            return;
        }
        StartNewRun(showCutscene: false);
    }
}
