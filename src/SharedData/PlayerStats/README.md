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
