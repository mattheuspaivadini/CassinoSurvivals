<div align="center">

# Cassino Survivals

**Sobreviva às hordas, gire os caça-níqueis e derrube o Tigrinho.**

Um *survivor-like* 2D top-down feito na Unity: a frenesi de hordas de *Vampire Survivors*,
a ação direta de *Hotline Miami* e o neon de um cassino de Vice City.

![Unity](https://img.shields.io/badge/Unity-6000.6.9f1-black?logo=unity)
![URP](https://img.shields.io/badge/Render-URP%202D-ff3cac)
![C#](https://img.shields.io/badge/C%23-.NET-239120?logo=csharp)
![Linux](https://img.shields.io/badge/Testado%20em-Linux%20(Vulkan)-FCC624?logo=linux&logoColor=black)

</div>

## Controles

| Tecla | Ação |
|---|---|
| WASD / Setas | Mover |
| E (perto de um caça-níquel) | Girar as roletas (gasta 1 ficha) |
| Mouse | Escolher o slot / reiniciar |

As armas disparam automaticamente no inimigo mais próximo.

## Mecânicas

- **Hordas:** o ritmo de spawn e o HP dos inimigos crescem com o tempo.
- **Elites:** sempre deixam uma Ficha de Cassino ao morrer.
- **Seta-guia:** com fichas no inventário, uma seta ao redor do jogador aponta para o caça-níquel mais próximo.
- **Caça-níquel:** o jogo pausa, três roletas sorteiam armas e o jogador escolhe uma. O prêmio depende de quantas vezes a arma escolhida apareceu:

  | Aparições | Já possui a arma | Arma nova |
  |:---:|:---:|:---:|
  | 1x | +1 nível | Desbloqueia no nível 1 |
  | 2x | +5 níveis | Desbloqueia no nível 5 |
  | 3x (Jackpot) | +9 níveis | Desbloqueia no nível 9 |

- **Armas:** cada nível aumenta o dano e o pente e reduz o tempo de recarga.
- **Ajudantes:** NPCs no mapa que, ao serem encontrados, seguem o jogador e curam periodicamente.
- **Drops:** fichas, cura e bônus de pontos.
- **Boss Tigrinho:** surge aos 5 minutos ou aos 25.000 pontos, com 60.000 de HP. Derrotá-lo vence a partida.

### Armas

| Arma | Característica |
|---|---|
| Pistola Neon | Equilibrada, arma inicial |
| Escopeta Royal Flush | Leque de 5 projéteis, curto alcance |
| SMG Vice City | Cadência alta, pente grande |
| Revólver High Roller | Dano alto, longo alcance |
| Lança-Dados | Três projéteis lentos e pesados |

## Como rodar

1. Instale a Unity **6000.6.9f1** pelo Unity Hub.
2. Clone o repositório e adicione a pasta no Hub (*Add > Add project from disk*).
3. Abra o projeto. A cena `Assets/Scenes/Main.unity` é carregada automaticamente.
4. Aperte **Play**.

Para regenerar cena, prefabs e sprites, use o menu **Cassino Survivals > Build Project**. Os ScriptableObjects de armas e inimigos existentes são preservados.

## Estrutura

```
Assets/
├── Scripts/
│   ├── Core/      GameManager, PoolManager, PrefabPool, AudioManager, CameraFollow
│   ├── Player/    PlayerController
│   ├── Weapons/   WeaponData, WeaponInstance, WeaponSystem, Projectile
│   ├── Enemies/   EnemyData, Enemy, EnemySpawner
│   ├── Casino/    SlotRules, SlotMachine, SlotMachineUI, Wayfinder
│   ├── Helpers/   Helper, Drop, DropManager
│   └── UI/        HUD, EndScreenUI
├── Editor/        CassinoBuilder, MainSceneLoader
├── Data/          ScriptableObjects de armas e inimigos
├── Prefabs/       Projectile, Enemy, Drop
└── Settings/      Configuração do URP 2D
```

## Performance

O projeto foi pensado para rodar bem em GPU integrada (Intel Iris Xe):

- **Object pooling** com `UnityEngine.Pool.ObjectPool<T>` para projéteis, inimigos e drops, com objetos criados no carregamento da cena.
- **Sem alocação no loop principal:** nada de LINQ ou `Physics2D.Overlap*`. Mira, acertos e coleta usam `sqrMagnitude`, e as referências ficam em cache.
- **URP 2D enxuto:** sprites Unlit, sem luzes 2D, pós-processamento, HDR ou MSAA.
- **UI em Canvas separados** (estático, HUD, modal e tela final). O HUD atualiza por evento ou a 10 Hz.
- **Lógica desacoplada da UI** por eventos (`OnHpChanged`, `OnScoreChanged`, etc.).
- **Input clássico** (`Input.GetAxisRaw`), compatível com Wayland/XWayland.

## Assets

Os visuais são placeholders gerados por código (quadrados, círculos e losangos), diferenciados por cor e escala. O `AudioManager` já expõe os pontos de áudio: basta atribuir os `AudioClip`s no Inspector.

## Próximos passos

- [ ] Sprites e sons definitivos
- [ ] Ataques especiais do Tigrinho
- [ ] Ajudantes invocados por drop
- [ ] Menu principal e ranking
