using System;
using System.Linq;
using Godot;

public enum GameState { Playing, Paused, GameOver, Victory }

/// <summary>Estado global da partida: pontos, fichas, tempo, pausa e fim de jogo. A UI só escuta os signals.</summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    /// <summary>Segundos de partida até o Tigrinho aparecer.</summary>
    [Export] public float BossSpawnTime { get; set; } = 300f;
    /// <summary>Pontos que também chamam o Tigrinho antes do tempo (0 = desativado).</summary>
    [Export] public int BossScoreThreshold { get; set; } = 25000;

    [Signal] public delegate void ScoreChangedEventHandler(int score);
    [Signal] public delegate void ChipsChangedEventHandler(int chips);
    [Signal] public delegate void StateChangedEventHandler(int state); // GameState

    public int Score { get; private set; }
    public int Chips { get; private set; }
    public float Elapsed { get; private set; }
    public GameState State { get; private set; }

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
        if (State == GameState.Playing) Elapsed += (float)delta;
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
        EmitSignal(SignalName.StateChanged, (int)State);
        return true;
    }

    /// <summary>Este nó sobrevive ao reload da cena (autoload), então o estado é zerado à mão.</summary>
    public void Restart()
    {
        Score = Chips = 0;
        Elapsed = 0f;
        State = GameState.Playing;
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
