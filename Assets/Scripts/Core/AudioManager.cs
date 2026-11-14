using UnityEngine;

/// <summary>
/// Ponto único de áudio. Os clipes ficam vazios por enquanto: arraste os AudioClips no Inspector
/// quando existirem e tudo passa a tocar sem mudar código (Play ignora clipes nulos).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip shoot, enemyHit, enemyDeath, playerHurt, pickup, heal;
    public AudioClip slotSpin, slotWin, jackpot, bossSpawn, gameOver, victory;

    AudioSource source;

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.ignoreListenerPause = true; // o caça-níquel toca com Time.timeScale = 0
    }

    void Play(AudioClip clip)
    {
        if (clip != null) source.PlayOneShot(clip);
    }

    public void PlayShoot() => Play(shoot);
    public void PlayEnemyHit() => Play(enemyHit);
    public void PlayEnemyDeath() => Play(enemyDeath);
    public void PlayPlayerHurt() => Play(playerHurt);
    public void PlayPickup() => Play(pickup);
    public void PlayHeal() => Play(heal);
    public void PlaySlotSpin() => Play(slotSpin);
    public void PlaySlotWin() => Play(slotWin);
    public void PlayJackpot() => Play(jackpot);
    public void PlayBossSpawn() => Play(bossSpawn);
    public void PlayGameOver() => Play(gameOver);
    public void PlayVictory() => Play(victory);
}
