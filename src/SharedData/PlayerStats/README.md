# PlayerStats

The player's base parameter values — the single source both the game (`Main/Player/Player.cs` through
`Core.Entity.PlayerStatsProvider`) and the passive-tree tool measure against. The file is a map of
named profiles; today there is one, `unarmed`: the character with no weapon and no gear. A parameter
the profile does not name has a base of zero.

Data files carry no comments, so the design notes behind individual values live here.

## Value notes

* **AdditionalHitChance = 0.05** — a rare treat, not a machine gun: extra attacks chain (each one
  re-rolls), so a high base made attack series balloon to 2-3x their planned length. Items and
  passives are the intended source of this stat.
* **BlockChance = 0.05** — the baseline every fighter blocks at ("Базовый шанс 5%", `1_Механики боя`).
  A blocked attack deals no damage and lays no effects, so percent block lines on gear amplify this
  base even when nothing flat is worn. NPCs name their own value in `Npc.json`.
