# LastBreath — Demo Roadmap

*Created 2026-07-24. Target: a playable public demo (1–2 hours of gameplay). Asset strategy: AI-generated placeholders continuously, key art (hero, bosses, UI) commissioned/hand-made after mechanics freeze. Rough estimate: ~11–16 weeks.*

Tracking: GitHub board #3 (Toddless/Last-Breath). This file is the stage-level view; individual items live as issues.

## Ground rules

- **System freeze from Stage 1**: new system ideas (weekly events, archon squads, endgame portals, conditional modifiers) go to the backlog, not into the demo.
- **"First hour" polish rule**: if a new player won't see it in their first hour, it is not polished before the demo.
- **Assets follow mechanics, never lead**: commission art only for frozen mechanics.
- **Every stage ends with a Godot pass** of its area (compile-check by Claude, in-engine testing by Todd). Bugs from a stage do not carry into the next one.

## Stage 0 — Stabilization (~1–2 weeks) ← CURRENT

Goal: the core loop never breaks in any normal scenario. Playtesters forgive placeholders, not crashes.

- [x] Land the current uncommitted tail (`projects-clean-up` branch) — committed `ecb13026` (2026-07-24). Godot-test of mythic mark handles (2026-07-20) and beat coalescing (#86 in "Доработать") still pending.
- [x] #147 — defeat on an NPC spawn point breaks the battle-exit cycle. Presumed fixed indirectly by #146 (recovery-zone regen broke the stasis transition); retest on occasion. Residual edge: a FRIENDLY camp/campfire still heals the player — if it reproduces there, gate recovery by the defeated state, not just IsAlive/IsFighting.
- [x] #146 — player regenerates while standing on an NPC spawn point. Fixed 2026-07-24 (zone eligibility filter by faction standing), confirmed by Todd.
- [x] #145 — first-level abilities not shown on the battle panel until the mastery window is opened. Fixed 2026-07-24 (ctor catch-up in AbilityUnlockService), confirmed by Todd.
- [ ] #132 — IceShards stage 2 ignores SecondStage numbers. Fixed 2026-07-24 (stage 2 now reads the SecondStage* parameters, as the ability description promises) — awaiting Godot test.
- [x] #130 — crash 0xC0000005 when closing the app via the window X. Clean quit path implemented and CONFIRMED by Todd 2026-07-24 (`BattleArena.AbortBattle` → `BattleContext.Abort` → `Main.QuitGracefully`; in-game quit buttons routed through the close request; sandbox mirrored). The underlying crash (#66) stays under observation.
- [ ] #142 / #143 — gchandle / Handle-not-initialized errors on battle start (possibly same family as #130).
- [x] #149 — scheduler reaction depth fuse: real off-by-one (`>` instead of `>=`) masked by a test depth value (128 → reverted to 25 by Todd); fixed 2026-07-24, full fast suite green. Leftover test values found in `CraftingMastery.json`: mythicGiftBaseChance restored to 0.15; `extraEffectBaseChance: 1` (reference 0.1) and `ascensionLevelGate: 1` (design 35) left for Todd's call.

## Stage 1 — Bosses (~2–3 weeks)

Biggest gameplay payoff; the demo's climax. Pipeline already proven on RatKing (stages, StageGuard, CC resist, BossSpawnPoint).

- [ ] "Доработать" #67: decide manual placement vs BossSpawnPoint; finish the NPC class for unique mechanics.
- [ ] Three bosses, one of them two-stage (design drafts in Obsidian `07_Нпс/Боссы.md`).
- [ ] "Доработать" #66: BattleDirector arena-wide ability presentation (global casts, boss stage transitions) — bosses look poor without it.

## Stage 2 — Crafting resources rework (~1–2 weeks)

Current crafting resources are broken against the new affix model (prefix/suffix + global/local split). Design drafts: Obsidian `06_Крафт/Список ресурсов для крафта.md` and `03_Предметы/Список пулов по категориям.md` (WIP — several category sections still empty).

- [ ] Data schema: material-category base pools split per EQUIPMENT category (Armor/Weapon/Jewellery sections inside Metal/Fabric/Hide/Gem/Bone), same split for per-resource modifier lists — a resource contributes different modifiers depending on what is being crafted (closes "Доработать" #94).
- [ ] `scope` field on pool entries (global vs local) — parse into `ModifierScope` (the local bucket already exists on `EquipItem`); draft marks locals with «(локальный)».
- [ ] Roll ranges on resource-descriptor entries (draft values are min–max, current data is single-value).
- [ ] Pool assembly (`EquipItemPoolExtensions`) picks the resource sub-pool by the target item's category; affix dedup invariants (ModifierKey) must keep holding.
- [ ] Finish the Obsidian lists (Todd) → translate into `SharedData/Resources/CraftingResources.json`.

## Stage 3 — Trade (~1–2 weeks)

Closes the economy loop (loot → gold → purchases) and activates already-built hooks: reputation perks await a consumer, `StartTrade` dialogue action is promised in the enum, prices-from-RelationLevel are designed.

- [ ] Trader window + trader inventory (JSON data like everything else).
- [ ] Item gold price (decide storage: in item data — recommended).
- [ ] Prices × RelationLevel + `Perk_Price_Change` — first consumer of `ReputationPerkProvider`.

## Stage 4 — Demo content & balance (~2 weeks, parallel with Stage 5)

- [ ] Define the demo arc: starting zone → quest thread → boss finale. Map stays small.
- [ ] Quests/dialogues for the demo arc (system is ready; this is content).
- [ ] Balance pass via existing simulations (loot Monte-Carlo, reputation invariants) + manual: #75 (unarmed barrier 10000), elemental damage weights ("Доработать" #87).
- [ ] #61 — English text debt (EquipmentPiece literals, "Day" in HUD, etc.).

## Stage 5 — First-impression UX polish (~1–2 weeks)

Only what a new player sees in the first hour:

- [ ] Loading screen ("Доработать" #18) and ally/enemy join-battle notification (#19).
- [ ] Destroy-mode confirmation (#22) — a misclicked legendary ruins a playtest.
- [ ] Equipped-item comparison (#38) and modifier ranges on reroll (#85) — minimum for loot readability.
- [ ] Crafting mastery level display (#72), craft forecast with resource effects (#90).
- [ ] Damage numbers above the spot (#95), NPC modifier fonts with 3+ enemies (#71).

## Stage 6 — Assets & audio (~3–4 weeks, starts alongside Stage 4)

- [ ] **AI batch continuously** (godot-asset-generator pipeline is ready): item/ability icons, regular NPC sprites, ability VFX clips (data-driven: clip in VfxFrames + config, no code), zone tiles. Each boss gets at least AI art immediately.
- [ ] **Key art commissioned/hand-made near the end**: hero, three bosses, UI frame. Order only after boss mechanics freeze.
- [ ] **Audio — do not leave for last** (nothing exists yet; half the feel of combat): minimal pass (hits, casts, UI clicks, death, 1–2 music loops — library/CC0 acceptable) before the first external playtests; polish after.

## Stage 7 — Demo finalization (~1–2 weeks)

- [ ] First export: SharedData symlinks in pack (#57), dead `Main/export_presets.cfg` ("Доработать" #25), isolated project startup (#24).
- [ ] Full Godot pass (Block-0 style) + new game → save/load → session reset cycle.
- [ ] Closed playtests (friends) → feedback iteration → public page (itch).

## Out of scope for the demo

Endgame (#33 Portals, #34 Netherworld), map expansion, weekly events ("Доработать" #61-idea), squad leaders/archons (#64), items conversion (#12), the low-priority tail of "Доработать".
