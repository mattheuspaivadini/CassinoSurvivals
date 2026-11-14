using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD dinâmico (Canvas separado do estático). HP/pontos/fichas só atualizam por evento;
/// lista de armas e timer atualizam a 10 Hz para não reconstruir o Canvas todo frame.
/// </summary>
public class HUD : MonoBehaviour
{
    [Tooltip("Barra com pivot X=0; a largura é controlada por localScale.x (sem rebuild de layout).")]
    public RectTransform hpFill;
    public Text hpText, scoreText, chipsText, weaponsText, timerText, bannerText;
    public WeaponSystem weaponSystem;
    public float refreshInterval = 0.1f;

    readonly StringBuilder sb = new StringBuilder(512);
    float refreshTimer, bannerTimer;

    void Start()
    {
        var gm = GameManager.Instance;
        gm.OnScoreChanged += UpdateScore;
        gm.OnChipsChanged += UpdateChips;
        PlayerController.Instance.OnHpChanged += UpdateHp;
        EnemySpawner.Instance.OnBossSpawned += ShowBossBanner;

        UpdateScore(gm.Score);
        UpdateChips(gm.Chips);
        UpdateHp(PlayerController.Instance.CurrentHp, PlayerController.Instance.maxHp);
        bannerText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnChipsChanged -= UpdateChips;
        }
        if (PlayerController.Instance != null) PlayerController.Instance.OnHpChanged -= UpdateHp;
        if (EnemySpawner.Instance != null) EnemySpawner.Instance.OnBossSpawned -= ShowBossBanner;
    }

    void Update()
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
            if (bannerTimer <= 0f) bannerText.gameObject.SetActive(false);
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = refreshInterval;
        RefreshWeapons();
        RefreshTimer();
    }

    void RefreshWeapons()
    {
        sb.Clear();
        var list = weaponSystem.Weapons;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            sb.Append("<color=#").Append(w.ColorHex).Append('>').Append(w.Data.weaponName).Append("</color>  Nv ")
              .Append(w.Level).Append("  ");
            if (w.IsReloading) sb.Append("<color=#FF5555>RECARREGANDO ").Append((int)(w.ReloadProgress * 100f)).Append("%</color>");
            else sb.Append(w.Ammo).Append('/').Append(w.Data.Magazine(w.Level));
            sb.Append('\n');
        }
        weaponsText.text = sb.ToString();
    }

    void RefreshTimer()
    {
        var gm = GameManager.Instance;
        if (EnemySpawner.Instance.BossSpawned) { timerText.text = "TIGRINHO EM CAMPO!"; return; }
        int left = Mathf.Max(0, Mathf.CeilToInt(gm.bossSpawnTime - gm.Elapsed));
        sb.Clear();
        sb.Append("Tigrinho em ").Append(left / 60).Append(':').Append((left % 60).ToString("00"));
        timerText.text = sb.ToString();
    }

    void UpdateHp(float hp, float max)
    {
        hpFill.localScale = new Vector3(Mathf.Clamp01(hp / max), 1f, 1f);
        hpText.text = $"{Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(max)}";
    }

    void UpdateScore(int score) => scoreText.text = $"PONTOS: {score}";
    void UpdateChips(int chips) => chipsText.text = $"FICHAS: {chips}";

    void ShowBossBanner()
    {
        bannerText.text = "O TIGRINHO CHEGOU!";
        bannerText.gameObject.SetActive(true);
        bannerTimer = 4f;
    }
}
