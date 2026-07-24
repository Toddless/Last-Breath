# LastBreath — Demo Roadmap

*Created 2026-07-24. Target: a playable public demo (1–2 hours of gameplay). Asset strategy: AI-generated placeholders continuously, key art (hero, bosses, UI) commissioned/hand-made after mechanics freeze. Rough estimate: ~10–14 weeks.*

Tracking: GitHub board #3 (Toddless/Last-Breath). This file is the stage-level view; individual items live as issues.

## Ground rules

- **System freeze from Stage 1**: new system ideas (weekly events, archon squads, endgame portals, conditional modifiers) go to the backlog, not into the demo.
- **"First hour" polish rule**: if a new player won't see it in their first hour, it is not polished before the demo.
- **Assets follow mechanics, never lead**: commission art only for frozen mechanics.
- **Every stage ends with a Godot pass** of its area (compile-check by Claude, in-engine testing by Todd). Bugs from a stage do not carry into the next one.

## Stage 0 — Stabilization (~1–2 weeks) ← CURRENT

Goal: the core loop never breaks in any normal scenario. Playtesters forgive placeholders, not crashes.

- [ ] Land the current uncommitted tail (`projects-clean-up` branch); Godot-test mythic mark handles (2026-07-20) and beat coalescing (#86 in "Доработать").
- [ ] #147 — defeat on an NPC spawn point breaks the battle-exit cycle.
- [ ] #146 — player regenerates while standing on an NPC spawn point.
- [ ] #145 — first-level abilities not shown on the battle panel until the mastery window is opened (first-impression bug).
- [ ] #132 — IceShards stage 2 ignores SecondStage numbers.
- [ ] #130 — crash 0xC0000005 on quit mid-battle: build a clean quit path (wind down the battle before `Quit()`) instead of waiting for repro.
- [ ] #142 / #143 — gchandle / Handle-not-initialized errors on battle start (possibly same family as #130).
- [ ] Scheduler reaction depth-fuse regression: 3 failures in `AttackContextSchedulerTests` on clean HEAD (ping-pong fuse expected 25 attacks, got 129) — the fuse against infinite reaction chains is effectively broken (found 2026-07-24).

## Stage 1 — Bosses (~2–3 weeks)

Biggest gameplay payoff; the demo's climax. Pipeline already proven on RatKing (stages, StageGuard, CC resist, BossSpawnPoint).

- [ ] "Доработать" #67: decide manual placement vs BossSpawnPoint; finish the NPC class for unique mechanics.
- [ ] Three bosses, one of them two-stage (design drafts in Obsidian `07_Нпс/Боссы.md`).
- [ ] "Доработать" #66: BattleDirector arena-wide ability presentation (global casts, boss stage transitions) — bosses look poor without it.

## Stage 2 — Trade (~1–2 weeks)

Closes the economy loop (loot → gold → purchases) and activates already-built hooks: reputation perks await a consumer, `StartTrade` dialogue action is promised in the enum, prices-from-RelationLevel are designed.

- [ ] Trader window + trader inventory (JSON data like everything else).
- [ ] Item gold price (decide storage: in item data — recommended).
- [ ] Prices × RelationLevel + `Perk_Price_Change` — first consumer of `ReputationPerkProvider`.

## Stage 3 — Demo content & balance (~2 weeks, parallel with Stage 4)

- [ ] Define the demo arc: starting zone → quest thread → boss finale. Map stays small.
- [ ] Quests/dialogues for the demo arc (system is ready; this is content).
- [ ] Balance pass via existing simulations (loot Monte-Carlo, reputation invariants) + manual: #75 (unarmed barrier 10000), elemental damage weights ("Доработать" #87).
- [ ] #61 — English text debt (EquipmentPiece literals, "Day" in HUD, etc.).

## Stage 4 — First-impression UX polish (~1–2 weeks)

Only what a new player sees in the first hour:

- [ ] Loading screen ("Доработать" #18) and ally/enemy join-battle notification (#19).
- [ ] Destroy-mode confirmation (#22) — a misclicked legendary ruins a playtest.
- [ ] Equipped-item comparison (#38) and modifier ranges on reroll (#85) — minimum for loot readability.
- [ ] Crafting mastery level display (#72), craft forecast with resource effects (#90).
- [ ] Damage numbers above the spot (#95), NPC modifier fonts with 3+ enemies (#71).

## Stage 5 — Assets & audio (~3–4 weeks, starts alongside Stage 3)

- [ ] **AI batch continuously** (godot-asset-generator pipeline is ready): item/ability icons, regular NPC sprites, ability VFX clips (data-driven: clip in VfxFrames + config, no code), zone tiles. Each boss gets at least AI art immediately.
- [ ] **Key art commissioned/hand-made near the end**: hero, three bosses, UI frame. Order only after boss mechanics freeze.
- [ ] **Audio — do not leave for last** (nothing exists yet; half the feel of combat): minimal pass (hits, casts, UI clicks, death, 1–2 music loops — library/CC0 acceptable) before the first external playtests; polish after.

## Stage 6 — Demo finalization (~1–2 weeks)

- [ ] First export: SharedData symlinks in pack (#57), dead `Main/export_presets.cfg` ("Доработать" #25), isolated project startup (#24).
- [ ] Full Godot pass (Block-0 style) + new game → save/load → session reset cycle.
- [ ] Closed playtests (friends) → feedback iteration → public page (itch).

## Out of scope for the demo

Endgame (#33 Portals, #34 Netherworld), map expansion, weekly events ("Доработать" #61-idea), squad leaders/archons (#64), items conversion (#12), the low-priority tail of "Доработать".
