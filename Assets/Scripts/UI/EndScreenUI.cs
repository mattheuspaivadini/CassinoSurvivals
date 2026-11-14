using UnityEngine;
using UnityEngine.UI;

/// <summary>Tela de Game Over / Vitória com botão de reiniciar.</summary>
public class EndScreenUI : MonoBehaviour
{
    public GameObject panel;
    public Text titleText, statsText;
    public Button restartButton;

    void Start()
    {
        panel.SetActive(false);
        GameManager.Instance.OnStateChanged += OnStateChanged;
        restartButton.onClick.AddListener(GameManager.Instance.Restart);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState s)
    {
        if (s != GameState.GameOver && s != GameState.Victory) return;
        var gm = GameManager.Instance;
        bool win = s == GameState.Victory;
        titleText.text = win ? "VOCÊ QUEBROU A BANCA!" : "GAME OVER";
        titleText.color = win ? new Color(1f, 0.85f, 0.1f) : new Color(1f, 0.25f, 0.35f);
        int t = (int)gm.Elapsed;
        statsText.text = $"Pontos: {gm.Score}\nTempo: {t / 60}:{t % 60:00}";
        panel.SetActive(true);
    }
}
