# LastBreath — Demo Roadmap

*Created 2026-07-24. Target: a playable public demo (1–2 hours of gameplay). Asset strategy: AI-generated placeholders continuously, key art (hero, bosses, UI) commissioned/hand-made after mechanics freeze. Rough estimate: ~11–16 weeks.*

*Updated 2026-08-26 (tracking resumed after the 2026-07-27 pause): stages 2–3 closed with named tails; the martial-arts rework interlude that consumed the pause is recorded below; stage 6 gains the tween-animation plan for NPCs.*

Tracking: GitHub board #3 (Toddless/Last-Breath). This file is the stage-level view; individual items live as issues.

## Ground rules

- **System freeze from Stage 1**: new system ideas (weekly events, archon squads, endgame portals, conditional modifiers) go to the backlog, not into the demo.
- **"First hour" polish rule**: if a new player won't see it in their first hour, it is not polished before the demo.
- **Assets follow mechanics, never lead**: commission art only for frozen mechanics.
- **Every stage ends with a Godot pass** of its area (compile-check by Claude, in-engine testing by Todd). Bugs from a stage do not carry into the next one.

## Stage 0 — Stabilization ✅ (closed 2026-07-24)

Goal: the core loop never breaks in any normal scenario. Playtesters forgive placeholders, not crashes.

- [x] Land the current uncommitted tail (`projects-clean-up` branch) — committed `ecb13026` (2026-07-24). Mythic mark handles and beat coalescing (#86 in "Доработать") Godot-confirmed 2026-07-24.
- [x] #147 — defeat on an NPC spawn point breaks the battle-exit cycle. Presumed fixed indirectly by #146 (recovery-zone regen broke the stasis transition); retest on occasion. Residual edge: a FRIENDLY camp/campfire still heals the player — if it reproduces there, gate recovery by the defeated state, not just IsAlive/IsFighting.
- [x] #146 — player regenerates while standing on an NPC spawn point. Fixed 2026-07-24 (zone eligibility filter by faction standing), confirmed by Todd.
- [x] #145 — first-level abilities not shown on the battle panel until the mastery window is opened. Fixed 2026-07-24 (ctor catch-up in AbilityUnlockService), confirmed by Todd.
- [x] #132 — IceShards stage 2 ignores SecondStage numbers. Fixed and Godot-confirmed 2026-07-24.
- [x] #130 — crash 0xC0000005 when closing the app via the window X. Clean quit path implemented and CONFIRMED by Todd 2026-07-24 (`BattleArena.AbortBattle` → `BattleContext.Abort` → `Main.QuitGracefully`; in-game quit buttons routed through the close request; sandbox mirrored). The underlying crash (#66) stays under observation.
- [x] #142 / #143 — gchandle / Handle-not-initialized errors on battle start: not reproducible after the freed-node era fixes; closed 2026-07-24, reopen with a fresh stack if they return.
- [x] #149 — scheduler reaction depth fuse: real off-by-one (`>` instead of `>=`) masked by a test depth value (128 → reverted to 25 by Todd); fixed 2026-07-24, full fast suite green. Leftover test values found in `CraftingMastery.json`: mythicGiftBaseChance restored to 0.15; `extraEffectBaseChance: 1` (reference 0.1) and `ascensionLevelGate: 1` (design 35) left for Todd's call.

## Stage 1 — Bosses ✅ (closed 2026-07-24)

Biggest gameplay payoff; the demo's climax. Status audit 2026-07-24: FAR more is done than assumed — RatKing (stages/StageGuard/rage/attack effects) tested in Block 0; Bone Pack Leader + wolf summons coded and summon path tested; Twins fully coded (TwinAssist reactions with maxPerTurn / blockedByFinalDeathOf, ShieldEffect layer); BossSpawnPoint with both respawn modes placed in the world; CC diminishing done. Remaining:

- [x] Deep Wounds for the Bone Pack Leader — done and Godot-confirmed 2026-07-24: generic `passives` channel in Npc.json (id + properties → skill registry, both BaseNpc copies); the Leader carries Bleeding 120% / 3 turns / 999 stacks.
- [x] Godot pass 2026-07-24: all bosses confirmed working by Todd (Twins assists, Bone Pack Leader, RatKing).
- Moved to Stage 5 (polish): boss presentation iteration — stage-transition beat, "Immune!" beat + log (EffectResistedEvent), StageGuard "N prevented", shield bar over the barrier, arena-wide cast presentation ("Доработать" #66).
- Deferred by decision: boss intro lines (silent aggro for now), world-boss spawn from bones idea.

## Stage 2 — Crafting resources rework ✅ (closed 2026-08-26; two named tails below)

Current crafting resources are broken against the new affix model (prefix/suffix + global/local split). Design drafts: Obsidian `06_Крафт/Список ресурсов для крафта.md` and `03_Предметы/Список пулов по категориям.md` (WIP — several category sections still empty).

- [x] Code layer (2026-07-24): `byCategory` sections in CraftingResources.json (flat list = every category, section entries stamped `OnlyFor`), `ForCategory` gate in `EquipItemPoolExtensions` (reroll) and the creation handler; scope/ranges already existed in the DTO. Test: `ResourceEntriesOfAnotherEquipmentCategory_NeverEnterTheRerollPool`.
- [x] Data migration (2026-07-24, awaiting Godot test): category base pools (Metal/Fabric/Leather/Gem/Bone per the draft, essence shared pool emptied) + 27 existing resources updated (ranges, affixes, locals). Weights are a uniform placeholder (100) — balance pass later.
- [ ] Deferred to #151: new resource ids from the draft + non-line essence effects (DoT stack duration, mastery perks, max-sharpening ops).
- [ ] Finish the empty draft sections (Todd): Fabric-Weapon, Leather-Jewellery/Weapon, Bone-Armor.

## Stage 3 — Trade ✅ (closed 2026-08-26)

Closes the economy loop (loot → gold → purchases) and activates already-built hooks. Design locked with Todd 2026-07-24: gold is a WALLET counter (not a bag item); price is an INSTANCE VALUATION (`basePrice(blueprint) × rarityMult × (1 + k × upgradeLevel) × ascension`), authored basePrice on blueprints/items + multipliers in `SharedData/Trade/TradeConfiguration.json`; loot-table prices are generator budget units, NOT gold — untouched; trader stock is a HYBRID (authored json catalog id+count+chance + a few random equip slots from the loot pipeline), restocked on game time; entry via the `StartTrade` dialogue action.

- [x] Core (2026-07-24): `IWalletService` (events, save section v1, session reset) + `ItemValuation` + `TradeConfiguration.json`; `basePrice` plumbed through blueprints and resources.
- [x] basePrice placeholder pass (2026-07-24, Todd's call — balance later): 105 blueprints by piece (Weapon 100 … Belt 50), 46 resources by rarity target (final shelf price 5…400).
- [x] Trader service (2026-07-24): `TraderProvider` (`SharedData/Traders/Traders.json`) + `TraderService` — catalog chance rolls, purchases decrement, lazy game-time restock, random equip slots minted through `IItemCreationService` (uniques/mythics never roll on shelves), session reset. First trader authored: `Trader_Human_Merchant`. KNOWN GAP: stock is NOT saved yet (a reload restocks — revisit with the anti-savescam pass).
- [x] Buy/Sell via the bus (2026-07-24): `BuyItemRequest`/`SellItemRequest` handlers (all-or-nothing: wallet check-then-spend, full bag refunds); `TradePricing` — buy = valuation × (1 + `Perk_Price_Change`), sell = valuation × buyback × (1 − perk) — first consumer of `ReputationPerkProvider`; unpriced items refuse instead of guessing.
- [x] Trade operations complete (2026-07-24): quantity on Buy/Sell, buyback shelf at the earned price (cap 12, survives restocks), `OpenTradeWindowMessage` + `StartTrade` dialogue action + `trade` console command; TradeWindow.cs written against a node contract (offer rows code-built, bag grid borrowed from the Inventory service, sell = RMB / Ctrl+RMB on a bag slot).
- [x] TradeWindow.tscn + merchant NPC + StartTrade dialogue — full loop Godot-confirmed 2026-07-24 (repro also uncovered and fixed: hardcoded veteran npcId in the shared NPC scene's DialogueActor; contact battles starting with NEUTRAL NPCs — OnBodyEnter now gates on ConsidersPlayerAnEnemy, half of "Доработать" #7).
- Trade tails → Stage 5 polish: window refinements per the final Umbral prototype (expensive-purchase confirm, compare-with-equipped, sell quantity), gold in the PlayerHud, ~~trader stock persistence~~ (DONE `eb450c8e` — traderShelf v1, reload no longer rerolls the shelf), quest gold rewards (narrative action), gold pile icon.
- [x] Gold entry (design Todd + sim-tuned 2026-07-24): the kill's FINAL budget leftover (after the quality swap) mints a `GoldItem` pile on the floor (normal drop channel; pickup credits the wallet, never the bag). `goldPerBudgetUnit: 4` → 10.7 gold per regular kill, 3.7 for a lvl-1, 5.8 for a boss (its budget buys items instead); `maxGoldPerKill: 300` anti-jackpot fuse (uncapped, an overfed archon minted 46.5k). Curve with the cap: Stack_3 62 → Stack_7 130 → Extreme_Archon 291. Invariant pins both ends. Still open: quest gold rewards (narrative action), pile icon art.

## Interlude — Martial-arts rework & foundations (2026-07-27 … 2026-08-26, done while the roadmap was paused)

The demo's core system was rebuilt and the savegame/exploit ground hardened. Highlights, all reviewed and landed (tests 1565 → 1883):

- **Passive tree in game** (allocation/draft/respec, wheel rendering, popups, summary) + **ability sockets + augments as items** (minting, conversion, rivalry, ornaments as the fourth socket) — the free-upgrade system is gone.
- **TreeUx phase** (Т-1…Т-14): wheel UX, localization pass, tooltips; **SharedUi** shared catalog + symlink restorer.
- **Bug wave to the first playable**: effect-duration contract (N turns = N host turn-ends), freshness rule for foreign effects, death animations no longer cut, self-damage visible to taken-modifiers, crit formula trims only the surplus.
- **Combat systems for keystones**: barrier regen at turn start, resistance maximums (default 75 / ceiling 90), block rolls only in the Strength stance (draw always burned — RNG streams stable), Unblockable registered.
- **Savegame cluster closed** (anti-savescam): persisted-enum convention test, trader shelf (`traderShelf` v1), ground items (`groundItems` v1), ability cooldowns (`abilityBook` v9), pending-load hardening (no frame of the previous playthrough); reload no longer rerolls shelves, eats unpicked loot, or refreshes cooldowns.
- **Keystones-as-passives wiring (#202, phases 1–3.1)**: `passiveId` + properties channel on tree nodes, on-the-fly grant service, stat-passive grammar, wheel popup speaks for passive nodes, tree editor with dropdown line authoring, passive catalog + registry sync tests, rebuilt editor exe. Phase 4 (populating keystones) is the owner's current step.
- **Tree v1/v2 analytics**: equipment budgets per focus (mid/late), wedge inventory, ≤15-cluster proposals per wedge, three reference-build routes (Str/health, mana/cold, DoT+extra attacks) with point arithmetic.
- Also landed earlier in the pause: reputation system with raids, narrative phase 1 (dialogues/quests/world facts), villagers foundation, NPC visual library + asset-generator pipeline, English "+X to Parameter" formatting.

## Stage 4 — Demo content & balance (~2 weeks, parallel with Stage 5)

- [ ] Define the demo arc: starting zone → quest thread → boss finale. Map stays small.
- [ ] Quests/dialogues for the demo arc (system is ready; this is content).
- [ ] Balance pass via existing simulations (loot Monte-Carlo, reputation invariants) + manual: #75 (unarmed barrier 10000), elemental damage weights ("Доработать" #87).
- [ ] #61 — English text debt (EquipmentPiece literals, "Day" in HUD, etc.).

## Stage 5 — First-impression UX polish (~1–2 weeks)

Only what a new player sees in the first hour:

- [ ] Boss presentation iteration (moved from Stage 1; absorbs "Доработать" #66): stage-transition beat, "Immune!" beat + log line, StageGuard "N prevented" display, shield bar over the barrier + log, arena-wide cast presentation.
- [ ] Loading screen ("Доработать" #18) and ally/enemy join-battle notification (#19).
- [ ] Destroy-mode confirmation (#22) — a misclicked legendary ruins a playtest.
- [ ] Equipped-item comparison (#38) and modifier ranges on reroll (#85) — minimum for loot readability.
- [ ] Crafting mastery level display (#72), craft forecast with resource effects (#90).
- [ ] Damage numbers above the spot (#95), NPC modifier fonts with 3+ enemies (#71).

## Stage 6 — Assets & audio (~3–4 weeks, starts alongside Stage 4)

- [ ] **AI batch continuously** (godot-asset-generator pipeline is ready): item/ability icons (97 augment icons landed `ba94ce6b`), regular NPC sprites, ability VFX clips (data-driven: clip in VfxFrames + config, no code), zone tiles. Each boss gets at least AI art immediately.
- [ ] **NPC tween animation (owner decision 2026-08-26)**: full NPC animations are out of reach for the demo — each NPC gets 4 static sprites (one per facing) and a dedicated tween-based AnimationComponent (movement, attack, ability cast), swappable later for a real-animation component behind the same interface. Scouting the current AnimationsComponent contract first; implementation card to follow.
- [ ] **Key art commissioned/hand-made near the end**: hero, three bosses, UI frame. Order only after boss mechanics freeze.
- [ ] **Audio — do not leave for last** (nothing exists yet; half the feel of combat): minimal pass (hits, casts, UI clicks, death, 1–2 music loops — library/CC0 acceptable) before the first external playtests; polish after.

## Stage 7 — Demo finalization (~1–2 weeks)

- [ ] First export: SharedData symlinks in pack (#57), dead `Main/export_presets.cfg` ("Доработать" #25), isolated project startup (#24).
- [ ] Full Godot pass (Block-0 style) + new game → save/load → session reset cycle.
- [ ] Closed playtests (friends) → feedback iteration → public page (itch).

## Out of scope for the demo

Endgame (#33 Portals, #34 Netherworld), map expansion, weekly events ("Доработать" #61-idea), squad leaders/archons (#64), items conversion (#12), the low-priority tail of "Доработать".
