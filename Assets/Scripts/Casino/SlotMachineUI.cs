using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Modal do caça-níquel: pausa o jogo, gira 3 roletas (tempo real), jogador escolhe 1 slot.</summary>
public class SlotMachineUI : MonoBehaviour
{
    public static SlotMachineUI Instance { get; private set; }

    public GameObject panel;
    public Button[] slotButtons = new Button[3];
    public Image[] slotImages = new Image[3];
    public Text[] slotLabels = new Text[3];
    public Text resultText;

    [Tooltip("Banco de armas sorteáveis.")]
    public WeaponData[] weaponPool;
    public WeaponSystem weaponSystem;

    public float spinDuration = 1.2f;
    public float spinTick = 0.06f;
    public float closeDelay = 1.3f;

    readonly WeaponData[] reels = new WeaponData[3];
    WaitForSecondsRealtime tickWait, closeWait;
    bool choosing;

    void Awake()
    {
        Instance = this;
        tickWait = new WaitForSecondsRealtime(spinTick);
        closeWait = new WaitForSecondsRealtime(closeDelay);
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i;
            slotButtons[i].onClick.AddListener(() => Choose(idx));
        }
        panel.SetActive(false);
    }

    public void Open()
    {
        if (panel.activeSelf || GameManager.Instance.Chips <= 0) return;
        GameManager.Instance.SetPaused(true);
        panel.SetActive(true);
        StartCoroutine(Spin());
    }

    IEnumerator Spin()
    {
        SetButtons(false);
        resultText.text = "Girando...";
        AudioManager.Instance?.PlaySlotSpin();

        for (float t = 0f; t < spinDuration; t += spinTick)
        {
            for (int i = 0; i < 3; i++) Show(i, RandomWeapon());
            yield return tickWait;
        }

        for (int i = 0; i < 3; i++) { reels[i] = RandomWeapon(); Show(i, reels[i]); }

        bool jackpot = reels[0] == reels[1] && reels[1] == reels[2];
        resultText.text = jackpot ? "JACKPOT!!! Escolha sua arma" : "Escolha uma arma";
        if (jackpot) AudioManager.Instance?.PlayJackpot();
        SetButtons(true);
        choosing = true;
    }

    void Choose(int index)
    {
        if (!choosing) return;
        choosing = false;
        SetButtons(false);

        var chosen = reels[index];
        int matches = SlotRules.CountMatches(reels, chosen);
        int levels = SlotRules.LevelsFor(matches);
        bool owned = weaponSystem.Find(chosen) != null;

        GameManager.Instance.TrySpendChip();
        weaponSystem.AddOrUpgrade(chosen, levels);
        AudioManager.Instance?.PlaySlotWin();

        resultText.text = owned
            ? $"{chosen.weaponName} x{matches}: +{levels} nível(is)! Agora Nv {weaponSystem.Find(chosen).Level}"
            : $"{chosen.weaponName} x{matches}: DESBLOQUEADA no Nv {levels}!";
        StartCoroutine(CloseAfterDelay());
    }

    IEnumerator CloseAfterDelay()
    {
        yield return closeWait;
        panel.SetActive(false);
        GameManager.Instance.SetPaused(false);
    }

    WeaponData RandomWeapon() => weaponPool[Random.Range(0, weaponPool.Length)];

    void Show(int i, WeaponData w)
    {
        slotImages[i].color = w.projectileColor;
        slotLabels[i].text = w.weaponName;
    }

    void SetButtons(bool on)
    {
        for (int i = 0; i < slotButtons.Length; i++) slotButtons[i].interactable = on;
    }
}
