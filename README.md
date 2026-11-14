<div align="center">

# 🎰 Cassino Survivals

**Sobreviva às hordas, gire os caça-níqueis e derrube o Tigrinho.**

Um *survivor-like* 2D top-down feito na Unity: a frenesi de hordas de *Vampire Survivors*,
a ação direta de *Hotline Miami* e o neon de um cassino de Vice City.

![Unity](https://img.shields.io/badge/Unity-6000.6.9f1-black?logo=unity)
![URP](https://img.shields.io/badge/Render-URP%202D-ff3cac)
![C#](https://img.shields.io/badge/C%23-.NET-239120?logo=csharp)
![Linux](https://img.shields.io/badge/Testado%20em-Linux%20(Vulkan)-FCC624?logo=linux&logoColor=black)

</div>

---

## 🕹️ Como jogar

| Tecla | Ação |
|---|---|
| **WASD / Setas** | Mover |
| **E** (perto de um caça-níquel) | Girar as roletas (gasta 1 ficha) |
| **Mouse** | Escolher o slot no caça-níquel / reiniciar |

As armas **atiram sozinhas** no inimigo mais próximo. Seu trabalho é se mover, coletar e apostar.

## 🎲 Mecânicas

- **Hordas infinitas:** o ritmo e o HP dos inimigos crescem com o tempo.
- **Elites** sempre deixam uma **Ficha de Cassino** ao morrer.
- **Seta-guia:** com fichas no bolso, uma seta ao redor do jogador aponta para o caça-níquel mais próximo.
- **Caça-níquel com 3 roletas:** o jogo pausa, as roletas giram e você escolhe 1 das 3 armas sorteadas. Quanto mais vezes a arma escolhida aparecer, maior o prêmio:

  | Aparições | Já tem a arma | Arma nova |
  |:---:|:---:|:---:|
  | 1x | +1 nível | Desbloqueia no Nv 1 |
  | 2x | +5 níveis | Desbloqueia no Nv 5 |
  | 3x 🎉 **Jackpot** | +9 níveis | Desbloqueia no Nv 9 |

- **Armas escaláveis:** cada nível aumenta o dano e o pente e diminui o tempo de recarga.
- **Ajudantes:** NPCs espalhados pelo mapa. Encoste neles e eles passam a te seguir e curar.
- **Drops:** fichas, cura e bônus de pontos.
- **🐯 Boss Tigrinho:** aparece aos 5 minutos (ou aos 25.000 pontos). São 60.000 de HP: sem armas evoluídas no cassino, não tem como. Derrotá-lo vence a partida.

### Arsenal

| Arma | Estilo |
|---|---|
| 🔵 Pistola Neon | Equilibrada, arma inicial |
| 🟠 Escopeta Royal Flush | Leque de 5 projéteis, curto alcance |
| 🩷 SMG Vice City | Cadência altíssima, pente grande |
| 🟡 Revólver High Roller | Dano alto, longo alcance |
| 🟢 Lança-Dados | 3 dados lentos e pesados |

## 🚀 Rodando o projeto

1. Instale a **Unity 6000.6.9f1** pelo Unity Hub.
2. Clone o repositório e adicione a pasta no Hub (**Add → Add project from disk**).
3. Abra o projeto. A cena `Assets/Scenes/Main.unity` abre sozinha.
4. Aperte **Play**.

> 💡 Quer regenerar a cena, os prefabs e os sprites do zero? Use o menu **Cassino Survivals → Build Project**.
> Os ScriptableObjects de armas e inimigos já existentes são preservados.

## 🏗️ Arquitetura

```
Assets/
├── Scripts/
│   ├── Core/      GameManager, PoolManager, PrefabPool, AudioManager, CameraFollow
│   ├── Player/    PlayerController
│   ├── Weapons/   WeaponData (SO), WeaponInstance, WeaponSystem, Projectile
│   ├── Enemies/   EnemyData (SO), Enemy, EnemySpawner
│   ├── Casino/    SlotRules, SlotMachine, SlotMachineUI, Wayfinder
│   ├── Helpers/   Helper, Drop, DropManager
│   └── UI/        HUD, EndScreenUI
├── Editor/        CassinoBuilder (monta tudo), MainSceneLoader
├── Data/          ScriptableObjects de armas e inimigos
├── Prefabs/       Projectile, Enemy, Drop (pooled)
└── Settings/      URP 2D (configuração leve)
```

**Decisões de performance** (pensadas para iGPU Intel Iris Xe):

- ♻️ **Object Pooling** com `UnityEngine.Pool.ObjectPool<T>` para projéteis, inimigos e drops, com objetos pré-criados no carregamento.
- 🚫 **Zero GC no loop:** sem LINQ, sem `Physics2D.Overlap*`. Acertos, coleta e mira são calculados com `sqrMagnitude`, e as referências ficam em cache.
- 🎨 **URP 2D enxuto:** sprites *Unlit*, sem luzes 2D, sem pós-processamento, HDR ou MSAA.
- 🖼️ **UI em Canvas separados** (estático, HUD, modal e tela final). O HUD só atualiza por evento ou a 10 Hz.
- 🔌 **Desacoplado:** lógica dispara eventos (`OnHpChanged`, `OnScoreChanged`…) e a UI apenas escuta.
- ⌨️ **Input clássico** (`Input.GetAxisRaw`), sem dor de cabeça com Wayland/XWayland.

## 🎨 Assets

Tudo é placeholder gerado por código (quadrados, círculos, losangos), diferenciado por cor e escala.
O `AudioManager` já tem os pontos de áudio prontos: basta arrastar os `AudioClip`s no Inspector.

## 🗺️ Próximos passos

- [ ] Sprites e sons de verdade
- [ ] Ataques especiais do Tigrinho
- [ ] Ajudantes invocados por drop
- [ ] Menu principal e ranking de pontos

---

<div align="center">
Feito com ☕ e muitas fichas.
</div>
