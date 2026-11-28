<div align="center">

# Cassino Survivals

**Sobreviva às hordas, gire os caça-níqueis e derrube o Tigrinho.**

Um *survivor-like* 2D top-down feito no Godot 4 (C#): a frenesi de hordas de *Vampire Survivors*,
a ação direta de *Hotline Miami* e o neon de um cassino de Vice City.

![Godot](https://img.shields.io/badge/Godot-4.7%20.NET-478cbf?logo=godotengine&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET%2010-239120?logo=csharp)
![Linux](https://img.shields.io/badge/Testado%20em-Linux-FCC624?logo=linux&logoColor=black)
[![GitHub Projects](https://img.shields.io/badge/Quadro%20de%20Tarefas-GitHub%20Projects-8250DF?logo=github)](https://github.com/users/mattheuspaivadini/projects/5)

</div>

## Controles

| Tecla | Ação |
|---|---|
| WASD / Setas | Mover |
| E (perto de uma estação) | Usar a estação da Sala de Descanso |
| Scroll do mouse | Trocar de arma |
| Mouse | Escolher o slot / reiniciar |
| ESC | Pausa, com volume Geral / Música / Efeitos |
| T | Mira automática ou pelo mouse |
| 1–9 | Escolher arma |

As armas disparam automaticamente no inimigo mais próximo.

## Mecânicas

- **Hordas:** o ritmo de spawn e o HP dos inimigos crescem com o tempo.
- **Elites:** sempre deixam uma Ficha de Cassino ao morrer.
- **Seta-guia:** com fichas no inventário, uma seta ao redor do jogador aponta para o caça-níquel mais próximo.
- **Sala de Descanso:** quatro estações gastam as Fichas da run. O jogo pausa enquanto o modal está aberto, e sair antes de girar não cobra nada.

  | Estação | Custo | O que faz |
  |---|:---:|---|
  | Balcão do Bar | 1 ficha | Escolhe 1 bebida (Cura, Dano ou Velocidade) |
  | Caça-níquel de Melhorias | 1 ficha | 3 rolos: Jogador ou Arma, 1 de 3 atributos, de 5% a 30% (permanente na run) |
  | Caça-níquel de Armas | 2 fichas | 3 rolos com armas; escolhe 1 slot ou recusa o giro (a ficha não volta) |
  | Mesa de Roleta | 4 fichas | Sorteia 2 passivas que o jogador ainda não tem; escolhe 1. Máx. 1 por sala |

- **Caça-níquel de Armas:** o prêmio depende de quantas vezes a arma escolhida apareceu. Nível máximo 10.

  | Aparições | Já possui a arma | Arma nova |
  |:---:|:---:|:---:|
  | 1x | +1 nível | Desbloqueia no nível 1 |
  | 2x | +2 níveis | Desbloqueia no nível 2 |
  | 3x (Jackpot) | +3 níveis | Desbloqueia no nível 3 |

- **Sorte nos Caça-níqueis:** o upgrade Sorte da Loja Externa (Nv 0 a 5) melhora as chances de 2x/3x no Caça-níquel de Armas e das faixas altas no de Melhorias (Tabelas A e B do GDD).
- **Bebidas:** Cura recupera 35% do HP na hora; Dano dá +40% por 30s; Velocidade dá +25% de movimento e +30% de cadência por 30s. Tipos diferentes acumulam; repetir a mesma só reinicia o tempo. Barman (+8% por nível) e a passiva Open Bar (+50%) aumentam a eficiência. As ativas aparecem no HUD com o tempo restante.

- **Armas:** cada nível aumenta o dano e o pente e reduz o tempo de recarga.
- **Ajudantes:** NPCs no mapa que, ao serem encontrados, seguem o jogador e curam periodicamente.
- **Drops:** fichas, cura, bônus de pontos e bebidas aleatórias (2% nos inimigos comuns).
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

Trilha: `assets/sounds/musica_tema.mp3` em loop; nos rounds de boss toca `assets/sounds/boss_theme.mp3`.

A cena principal é `scenes/levels/Fase1.tscn`, montada a partir do Blockout 1 do documento de design (`Cassino Survivors.pdf`):

- Arena vertical com estátuas de dado, vasos de planta, mesa de pôquer, duas mesas de roleta e balcões de caça-níqueis. Todo obstáculo bloqueia movimento e tiros.
- 21 rounds. Em cada um, uma horda finita entra pelas 6 portas laterais (a lâmpada da porta pisca ~1s antes de abrir; as portas de baixo esperam 2,5s no início do round). A horda cresce todo round: de 36 inimigos no round 1 até 150 no round 20.
- Fichas: só Dourados e bosses soltam. 1 Dourado por round nos rounds 1–6, 2 nos rounds 8–13 e 3 nos rounds 15–20. Os bosses dos rounds 7, 14 e 21 soltam 3, 4 e 5 fichas.
- Dinheiro ($), como no PDF: comum $1, Dourado $5, round limpo $10, bosses $75/$125/$250, e cada ficha que sobra vira $10 no fim. O saldo fica salvo entre partidas e é gasto na loja da tela final: Vigor (+6% HP), Arsenal (+5% dano), Imortalidade (chance de reviver 1x), Barman (+8% eficiência nas bebidas) e Sorte (Tabelas A e B), 5 níveis cada.
- Cada balcão tem um caça-níquel jogável (brilho dourado no chão): com ficha, aperte E perto dele para girar e ganhar ou evoluir armas.
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
