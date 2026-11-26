<div align="center">

# Cassino Survivals

**Sobreviva às hordas, gire os caça-níqueis e derrube o Tigrinho.**

Um *survivor-like* 2D top-down feito no Godot 4 (C#): a frenesi de hordas de *Vampire Survivors*,
a ação direta de *Hotline Miami* e o neon de um cassino de Vice City.

![Godot](https://img.shields.io/badge/Godot-4.7%20.NET-478cbf?logo=godotengine&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET%2010-239120?logo=csharp)
![Linux](https://img.shields.io/badge/Testado%20em-Linux-FCC624?logo=linux&logoColor=black)

</div>

## Controles

| Tecla | Ação |
|---|---|
| WASD / Setas | Mover |
| E (perto de um caça-níquel) | Girar as roletas (gasta 1 ficha) |
| Scroll do mouse | Trocar de arma |
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
| Pistola | Equilibrada, arma inicial |
| Escopeta | Leque de 5 projéteis, curto alcance |
| Metralhadora | Cadência alta, pente grande |
| Revólver | Dano alto, longo alcance |
| Rifle | Três projéteis lentos e pesados |

## Fase 1 (Ato 1)

A cena principal é `scenes/levels/Fase1.tscn`, montada a partir do Blockout 1 do documento de design (`Cassino Survivors.pdf`):

- Arena vertical com pilares, mesa de pôquer, duas mesas de roleta e balcões de caça-níqueis. Todo obstáculo bloqueia movimento e tiros.
- 21 rounds. Em cada um, uma horda finita entra pelas 6 portas laterais (a lâmpada da porta pisca ~1s antes de abrir; as portas de baixo esperam 2,5s no início do round). A partir do round 3 vêm Dourados, que sempre deixam ficha.
- Rounds 7, 14 e 21 são de boss: primeiro uma mini-horda, depois o boss entra pela porta mais longe do jogador.
- A porta de cima só abre depois do boss do round 21. Uma seta aponta a saída, e setas na borda da tela mostram os últimos inimigos fora da câmera.
- Os inimigos contornam obstáculos (flow field), os rápidos tentam cercar o jogador e a horda se espalha em vez de virar fila.

O modo antigo (arena aberta por tempo, com o Tigrinho) continua em `scenes/Main.tscn`.

## Como rodar

1. Instale o **Godot 4.7 (versão .NET/Mono)** e o **.NET SDK 10**.
2. Clone o repositório e importe `project.godot` pelo gerenciador de projetos do Godot.
3. Aperte **F5** (a cena principal é `scenes/levels/Fase1.tscn`).

Pela linha de comando: `dotnet build` e depois `godot --path .`.
Teste das regras puras (armas e caça-níquel): `godot --headless --path . -- --selfcheck`.

A migração a partir da Unity está documentada em [MIGRATION_PLAN.md](MIGRATION_PLAN.md). A versão Unity continua no histórico do Git, antes do merge `feat/godot-migration`.

## Estrutura

```
autoloads/   GameManager, AudioManager (singletons globais)
scenes/      Main, Player, Enemy, Projectile, Drop, Helper, SlotMachine
scripts/
├── Player/    PlayerController
├── Weapons/   WeaponInstance, WeaponSystem, Projectile
├── Enemies/   Enemy, EnemySpawner
├── Casino/    SlotRules, SlotMachine, SlotMachineUI, Wayfinder
├── Helpers/   Helper, Drop, DropManager, DropType
├── UI/        HUD, EndScreenUI
└── Resources/ WeaponData, EnemyData (Resource)
resources/   .tres de armas e inimigos
assets/      sprites
```

## Performance

O projeto foi pensado para rodar bem em GPU integrada (Intel Iris Xe):

- **Renderer `gl_compatibility`**, suficiente para um jogo 2D de sprites.
- **Sem consultas de física no loop principal:** mira, acertos de projétil, contato e coleta usam `LengthSquared`, e o spawner mantém a lista de inimigos ativos com remoção O(1).
- **HUD por signal:** HP, pontos e fichas só atualizam quando mudam; lista de armas e timer a 10 Hz.
- **Lógica desacoplada da UI** por signals (`HpChanged`, `ScoreChanged`, etc.).

## Assets

Os visuais são placeholders (quadrados, círculos e losangos), diferenciados por cor e escala. O `AudioManager` já expõe os pontos de áudio: basta atribuir os `AudioStream`s em `autoloads/AudioManager.tscn`.

## Próximos passos

- [ ] Sprites e sons definitivos
- [ ] Ataques especiais do Tigrinho
- [ ] Ajudantes invocados por drop
- [ ] Menu principal e ranking
