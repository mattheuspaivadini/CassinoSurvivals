using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, GameOver, Victory }

/// <summary>Estado global da partida: pontos, fichas, tempo, pausa e fim de jogo. A UI só escuta os eventos.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Performance (Intel Iris Xe)")]
    public int targetFrameRate = 60;

    [Header("Boss Tigrinho")]
    [Tooltip("Segundos de partida até o Tigrinho aparecer.")]
    public float bossSpawnTime = 300f;
    [Tooltip("Pontos que também chamam o Tigrinho antes do tempo (0 = desativado).")]
    public int bossScoreThreshold = 25000;

    public int Score { get; private set; }
    public int Chips { get; private set; }
    public float Elapsed { get; private set; }
    public GameState State { get; private set; }

    public event Action<int> OnScoreChanged;
    public event Action<int> OnChipsChanged;
    public event Action<GameState> OnStateChanged;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = targetFrameRate;
        Time.timeScale = 1f;
        State = GameState.Playing;
    }

    void Update()
    {
        if (State == GameState.Playing) Elapsed += Time.deltaTime;
    }

    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    public void AddChips(int amount)
    {
        Chips += amount;
        OnChipsChanged?.Invoke(Chips);
    }

    public bool TrySpendChip()
    {
        if (Chips <= 0) return false;
        Chips--;
        OnChipsChanged?.Invoke(Chips);
        return true;
    }

    public bool IsBossTime =>
        Elapsed >= bossSpawnTime || (bossScoreThreshold > 0 && Score >= bossScoreThreshold);

    public void SetPaused(bool paused)
    {
        if (State == GameState.GameOver || State == GameState.Victory) return;
        State = paused ? GameState.Paused : GameState.Playing;
        Time.timeScale = paused ? 0f : 1f;
        OnStateChanged?.Invoke(State);
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
        Time.timeScale = 0f;
        OnStateChanged?.Invoke(State);
        return true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
