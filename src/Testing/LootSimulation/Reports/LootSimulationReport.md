# Loot simulation report

Seed: `20260710`, date: 2026-07-27 17:06

## Summary

| Scenario | Kills | Difficulty | Items/kill | Value/kill | p95 value | Budget | Value/difficulty | Gold/kill |
|---|---|---|---|---|---|---|---|---|
| Baseline_Regular | 10000 | 0 | 1.92 | 6.33 | 8 | 8.91 | 6.33 | 10.69 |
| Baseline_Boss | 10000 | 0 | 16.58 | 523.92 | 803 | 344.54 | 523.92 | 6.89 |
| Axis_Npc_Modifier_Scale_Double_Health | 10000 | 1.9 | 24.77 | 1324.6 | 1678 | 999.17 | 697.16 | 49.82 |
| Axis_Npc_Modifier_Tier_Upgrade_To_Maximum | 10000 | 0.9 | 23.59 | 3063.46 | 4761 | 654.63 | 3403.85 | 40.52 |
| Axis_Npc_Modifier_Tier_Upgrade_By_One | 10000 | 0.5 | 21.74 | 1551.5 | 2066 | 516.81 | 3103 | 22.59 |
| Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource | 10000 | 0.4 | 24.97 | 731.29 | 1055 | 482.36 | 1828.23 | 15.97 |
| Axis_Npc_Modifier_Tier_Multiplier_Huge | 10000 | 0.3 | 19.66 | 665 | 975 | 447.9 | 2216.68 | 14.72 |
| Axis_Npc_Modifier_Item_Effect_Vampire | 10000 | 0.3 | 20.07 | 668.46 | 982 | 447.9 | 2228.21 | 15.09 |
| Axis_Npc_Modifier_Rarity_Upgrade_Huge | 10000 | 0.3 | 19.98 | 667.66 | 985 | 447.9 | 2225.53 | 15.07 |
| Axis_Npc_Modifier_Min_Rarity_Epic | 10000 | 0.3 | 20 | 671.58 | 986 | 447.9 | 2238.59 | 15.1 |
| Stack_1_mods | 10000 | 0.59 | 25.47 | 1966 | 3214 | 1659.58 | 3352.57 | 50.09 |
| Stack_3_mods | 10000 | 1.76 | 26.52 | 3626.5 | 6025 | 2884.33 | 2063.82 | 64.55 |
| Stack_5_mods | 10000 | 2.92 | 27.56 | 5524.49 | 9594 | 4103.02 | 1890.56 | 106.04 |
| Stack_7_mods | 10000 | 4.1 | 28.57 | 7549.07 | 12201 | 5333.23 | 1842.08 | 143.36 |
| Stack_11_mods | 10000 | 6.44 | 30.54 | 10910.02 | 13099 | 7780.43 | 1694.78 | 232.22 |
| Extreme_Archon | 10000 | 6.41 | 30.62 | 11524.25 | 12676 | 14696.11 | 1798.98 | 291.49 |
| Extreme_Lvl1 | 10000 | 0 | 1.91 | 5.82 | 6 | 6.75 | 5.82 | 3.71 |

## Baseline_Regular

Regular/Uncommon/lvl 5, modifiers: []

| Tier | Rolls | Share |
|---|---|---|
| 0 | 0 | 0% |
| 1 | 0 | 0% |
| 2 | 0 | 0% |
| 3 | 19189 | 100% |

| Rarity | Items | Share |
|---|---|---|
| Epic | 3233 | 16.85% |
| Rare | 6657 | 34.69% |
| Uncommon | 7568 | 39.44% |
| Common | 1731 | 9.02% |

Top drops:
- Crafting_Resource_Deer_Leather: 1755
- Crafting_Resource_Copper_Ore: 1731
- Crafting_Resource_Linen_Fabric: 1698
- Crafting_Resource_Iron_Ore: 1695
- Crafting_Resource_Emerald_Gem: 1690
- Crafting_Resource_Ruby_Gem: 1680
- Crafting_Resource_Silk_Fabric: 1663
- Crafting_Resource_Essence_Health: 1655
- Crafting_Resource_Gold_Ore: 1632
- Crafting_Resource_Wild_Boar_Leather: 1609
- Crafting_Resource_Diamond_Gem: 1570
- Upgrade_Resource_Jeweler_Rune: 811

Orphans (76): Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Amulet_Pathfinder_Sign, Amulet_Recovery_Source, Amulet_Ruby, Amulet_Trapper_Talisman, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Feral_Instinct, Body_Hunter_Chestplate, Body_Hunters_Dream, Body_Mysterious_Bastion, Body_Porcupine, Body_Steel_Bastion, Body_Stoneheart, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Feral_Instinct, Boots_Hunter, Boots_Hunters_Dream, Boots_Mysterious_Bastion, Boots_Steel_Greaves, Boots_Stone_Tread, Boots_Vital_Core, Crafting_Resource_Essence_Critical_Chance, Gloves_Aegis, Gloves_Archmage, Gloves_Feral_Instinct, Gloves_Hunter, Gloves_Hunters_Dream, Gloves_Mysterious_Bastion, Gloves_Steel, Gloves_Stoneheart, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Feral_Instinct, Helmet_Hunter, Helmet_Hunters_Dream, Helmet_Mysterious_Bastion, Helmet_Steel, Helmet_Stoneheart, Helmet_Vital_Core, Ring_Dexterity, Ring_Hell_Servant, Ring_Intelligence, Ring_of_Creators, Ring_Of_Fire_Demon, Ring_of_Hunter, Ring_Of_Legionnaire, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Rare, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Blacksmith_Rune, Upgrade_Resource_Fine_Flux, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Jewellery_Dust_Rare, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Journeyman_Rune, Upgrade_Resource_Master_Rune, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Quality_Flux, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Rare, Upgrade_Resource_Weapon_Dust_Uncommon, Upgrade_Resource_Weapon_Rune, Upgrade_Resource_Weaponsmith_Mark, Weapon_Bloodthirsty, Weapon_Silent_Fury, Weapon_Simple_Axe, Weapon_Simple_Dagger, Weapon_Simple_Sword

## Baseline_Boss

Boss/Legendary/lvl 50, modifiers: []

| Tier | Rolls | Share |
|---|---|---|
| 0 | 0 | 0% |
| 1 | 1149 | 0.69% |
| 2 | 45675 | 27.55% |
| 3 | 118984 | 71.76% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 153 | 0.09% |
| Epic | 18808 | 11.34% |
| Rare | 58297 | 35.16% |
| Uncommon | 82885 | 49.99% |
| Common | 5665 | 3.42% |

Top drops:
- Crafting_Resource_Diamond_Gem: 5704
- Crafting_Resource_Copper_Ore: 5665
- Crafting_Resource_Deer_Leather: 5638
- Crafting_Resource_Ruby_Gem: 5550
- Crafting_Resource_Gold_Ore: 5530
- Crafting_Resource_Wild_Boar_Leather: 5528
- Crafting_Resource_Silk_Fabric: 5526
- Crafting_Resource_Iron_Ore: 5495
- Crafting_Resource_Linen_Fabric: 5494
- Crafting_Resource_Emerald_Gem: 5479
- Gloves_Mysterious_Bastion: 4415
- Boots_Mysterious_Bastion: 4404
- Amulet_Recovery_Source: 4390
- Crafting_Resource_Essence_Barrier: 4321
- Body_Mysterious_Bastion: 4306

Orphans (31): Amulet_Creators_Nature, Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Belt_Mana_Flow, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Body_Porcupine, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Hunters_Dream, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Aegis, Gloves_Archmage, Gloves_Hunters_Dream, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Hunters_Dream, Helmet_Vital_Core, Ring_Hell_Servant, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Scale_Double_Health

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Scale_Double_Health]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 3043 | 1.23% |
| 1 | 12018 | 4.85% |
| 2 | 101141 | 40.83% |
| 3 | 131495 | 53.09% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 507 | 0.2% |
| Epic | 23152 | 9.35% |
| Rare | 96983 | 39.15% |
| Uncommon | 122855 | 49.6% |
| Common | 3460 | 1.4% |
| Unique | 578 | 0.23% |
| Mythic | 162 | 0.07% |

Top drops:
- Body_Feral_Instinct: 8946
- Gloves_Feral_Instinct: 8889
- Helmet_Feral_Instinct: 8878
- Amulet_Ruby: 8252
- Ring_Dexterity: 8009
- Upgrade_Resource_Jewellery_Dust_Rare: 7890
- Ring_Intelligence: 7870
- Upgrade_Resource_Weapon_Dust_Rare: 7825
- Upgrade_Resource_Armor_Dust_Rare: 7818
- Recipe_Body_Aegis: 7518
- Belt_Of_Life: 7308
- Upgrade_Resource_Journeyman_Rune: 7122
- Crafting_Resource_Essence_Barrier: 7079
- Upgrade_Resource_Fine_Flux: 7055
- Upgrade_Resource_Jewellery_Dust_Uncommon: 6700

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Tier_Upgrade_To_Maximum

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Upgrade_To_Maximum]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 47514 | 20.15% |
| 1 | 3204 | 1.36% |
| 2 | 69301 | 29.38% |
| 3 | 115831 | 49.11% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 3256 | 1.38% |
| Epic | 22702 | 9.63% |
| Rare | 78547 | 33.3% |
| Uncommon | 108881 | 46.17% |
| Common | 4212 | 1.79% |
| Unique | 9171 | 3.89% |
| Mythic | 9081 | 3.85% |

Top drops:
- Body_Feral_Instinct: 6095
- Helmet_Feral_Instinct: 6026
- Gloves_Feral_Instinct: 5967
- Amulet_Ruby: 5755
- Ring_Dexterity: 5516
- Ring_Intelligence: 5486
- Upgrade_Resource_Armor_Dust_Rare: 5408
- Recipe_Body_Aegis: 5326
- Upgrade_Resource_Weapon_Dust_Rare: 5320
- Upgrade_Resource_Jewellery_Dust_Rare: 5182
- Crafting_Resource_Essence_Barrier: 5081
- Upgrade_Resource_Weapon_Dust_Uncommon: 5040
- Upgrade_Resource_Simple_Flux: 4989
- Helmet_Mysterious_Bastion: 4988
- Crafting_Resource_Essence_Damage: 4978

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Tier_Upgrade_By_One

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Upgrade_By_One]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 1067 | 0.49% |
| 1 | 33496 | 15.41% |
| 2 | 110504 | 50.83% |
| 3 | 72328 | 33.27% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 368 | 0.17% |
| Epic | 25245 | 11.61% |
| Rare | 95060 | 43.73% |
| Uncommon | 93405 | 42.97% |
| Common | 3016 | 1.39% |
| Unique | 161 | 0.07% |
| Mythic | 140 | 0.06% |

Top drops:
- Helmet_Feral_Instinct: 10366
- Body_Feral_Instinct: 10348
- Gloves_Feral_Instinct: 10245
- Amulet_Ruby: 8851
- Ring_Dexterity: 8779
- Upgrade_Resource_Jewellery_Dust_Rare: 8598
- Ring_Intelligence: 8568
- Upgrade_Resource_Weapon_Dust_Rare: 8528
- Recipe_Body_Aegis: 8442
- Upgrade_Resource_Armor_Dust_Rare: 8407
- Belt_Of_Life: 8332
- Upgrade_Resource_Journeyman_Rune: 7978
- Upgrade_Resource_Fine_Flux: 7947
- Crafting_Resource_Essence_Barrier: 4397
- Crafting_Resource_Ruby_Gem: 3056

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Guaranteed_Items_Crafting_Resource]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 293 | 0.14% |
| 1 | 1469 | 0.7% |
| 2 | 63437 | 30.25% |
| 3 | 144513 | 68.91% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 218 | 0.09% |
| Epic | 32724 | 13.1% |
| Rare | 75629 | 30.29% |
| Uncommon | 124806 | 49.98% |
| Common | 16309 | 6.53% |
| Unique | 26 | 0.01% |

Top drops:
- Crafting_Resource_Copper_Ore: 16309
- Crafting_Resource_Deer_Leather: 16252
- Crafting_Resource_Linen_Fabric: 16216
- Crafting_Resource_Diamond_Gem: 16214
- Crafting_Resource_Silk_Fabric: 6496
- Crafting_Resource_Wild_Boar_Leather: 6325
- Crafting_Resource_Iron_Ore: 6310
- Crafting_Resource_Ruby_Gem: 6304
- Crafting_Resource_Gold_Ore: 6274
- Crafting_Resource_Emerald_Gem: 6235
- Crafting_Resource_Essence_Barrier: 5666
- Boots_Mysterious_Bastion: 5620
- Amulet_Recovery_Source: 5603
- Gloves_Mysterious_Bastion: 5569
- Body_Mysterious_Bastion: 5560

Orphans (12): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Carapace, Boots_Aegis, Cloak_Creators_Cassock, Helmet_Aegis, Ring_of_Creators, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Tier_Multiplier_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Multiplier_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 343 | 0.17% |
| 1 | 1665 | 0.85% |
| 2 | 57786 | 29.39% |
| 3 | 136831 | 69.59% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 177 | 0.09% |
| Epic | 21586 | 10.98% |
| Rare | 70597 | 35.9% |
| Uncommon | 98158 | 49.92% |
| Common | 6078 | 3.09% |
| Unique | 29 | 0.01% |

Top drops:
- Crafting_Resource_Deer_Leather: 6324
- Crafting_Resource_Diamond_Gem: 6274
- Crafting_Resource_Emerald_Gem: 6258
- Crafting_Resource_Linen_Fabric: 6166
- Crafting_Resource_Silk_Fabric: 6150
- Crafting_Resource_Gold_Ore: 6142
- Crafting_Resource_Iron_Ore: 6130
- Crafting_Resource_Ruby_Gem: 6105
- Crafting_Resource_Copper_Ore: 6078
- Crafting_Resource_Wild_Boar_Leather: 6052
- Crafting_Resource_Essence_Barrier: 5202
- Boots_Mysterious_Bastion: 5161
- Amulet_Recovery_Source: 5132
- Gloves_Mysterious_Bastion: 5108
- Body_Feral_Instinct: 5107

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Item_Effect_Vampire

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Item_Effect_Vampire]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 214 | 0.11% |
| 1 | 1329 | 0.66% |
| 2 | 58843 | 29.32% |
| 3 | 140314 | 69.91% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 189 | 0.09% |
| Epic | 21900 | 10.91% |
| Rare | 71760 | 35.75% |
| Uncommon | 100631 | 50.14% |
| Common | 6199 | 3.09% |
| Unique | 21 | 0.01% |

Top drops:
- Crafting_Resource_Emerald_Gem: 6327
- Crafting_Resource_Silk_Fabric: 6323
- Crafting_Resource_Linen_Fabric: 6315
- Crafting_Resource_Gold_Ore: 6247
- Crafting_Resource_Diamond_Gem: 6246
- Crafting_Resource_Copper_Ore: 6199
- Crafting_Resource_Ruby_Gem: 6196
- Crafting_Resource_Wild_Boar_Leather: 6176
- Crafting_Resource_Deer_Leather: 6168
- Crafting_Resource_Iron_Ore: 6145
- Body_Mysterious_Bastion: 5412
- Upgrade_Resource_Blacksmith_Rune: 5318
- Amulet_Recovery_Source: 5274
- Boots_Mysterious_Bastion: 5273
- Upgrade_Resource_Weapon_Dust_Uncommon: 5250

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Rarity_Upgrade_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Rarity_Upgrade_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 214 | 0.11% |
| 1 | 1346 | 0.67% |
| 2 | 59246 | 29.66% |
| 3 | 138953 | 69.56% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 191 | 0.1% |
| Epic | 27701 | 13.87% |
| Rare | 69414 | 34.75% |
| Uncommon | 96082 | 48.1% |
| Common | 6352 | 3.18% |
| Unique | 19 | 0.01% |

Top drops:
- Crafting_Resource_Gold_Ore: 6359
- Crafting_Resource_Copper_Ore: 6352
- Crafting_Resource_Iron_Ore: 6304
- Crafting_Resource_Diamond_Gem: 6291
- Crafting_Resource_Silk_Fabric: 6290
- Crafting_Resource_Deer_Leather: 6259
- Crafting_Resource_Emerald_Gem: 6182
- Crafting_Resource_Linen_Fabric: 6073
- Crafting_Resource_Wild_Boar_Leather: 6057
- Crafting_Resource_Ruby_Gem: 6007
- Body_Mysterious_Bastion: 5285
- Boots_Mysterious_Bastion: 5283
- Amulet_Recovery_Source: 5244
- Helmet_Feral_Instinct: 5234
- Upgrade_Resource_Weapon_Rune: 5213

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Min_Rarity_Epic

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Min_Rarity_Epic]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 214 | 0.11% |
| 1 | 1363 | 0.68% |
| 2 | 58910 | 29.46% |
| 3 | 139508 | 69.76% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 207 | 0.1% |
| Epic | 72935 | 36.47% |
| Rare | 54869 | 27.44% |
| Uncommon | 65640 | 32.82% |
| Common | 6323 | 3.16% |
| Unique | 21 | 0.01% |

Top drops:
- Crafting_Resource_Copper_Ore: 6323
- Crafting_Resource_Deer_Leather: 6320
- Crafting_Resource_Linen_Fabric: 6308
- Crafting_Resource_Gold_Ore: 6265
- Crafting_Resource_Silk_Fabric: 6220
- Crafting_Resource_Ruby_Gem: 6212
- Crafting_Resource_Diamond_Gem: 6183
- Crafting_Resource_Iron_Ore: 6175
- Crafting_Resource_Wild_Boar_Leather: 6112
- Crafting_Resource_Emerald_Gem: 6106
- Body_Mysterious_Bastion: 5396
- Crafting_Resource_Essence_Barrier: 5326
- Gloves_Mysterious_Bastion: 5306
- Boots_Mysterious_Bastion: 5266
- Crafting_Resource_Essence_Damage: 5247

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Stack_1_mods

Boss/Mythic/lvl 100, modifiers: [] + 1 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 13688 | 5.48% |
| 1 | 21101 | 8.45% |
| 2 | 110162 | 44.14% |
| 3 | 104634 | 41.92% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 2082 | 0.82% |
| Epic | 27175 | 10.67% |
| Rare | 98572 | 38.71% |
| Uncommon | 119548 | 46.95% |
| Common | 2511 | 0.99% |
| Unique | 2577 | 1.01% |
| Mythic | 2185 | 0.86% |

Top drops:
- Gloves_Feral_Instinct: 10862
- Helmet_Feral_Instinct: 10859
- Body_Feral_Instinct: 10777
- Amulet_Ruby: 9738
- Ring_Intelligence: 9519
- Upgrade_Resource_Armor_Dust_Rare: 9260
- Ring_Dexterity: 9222
- Upgrade_Resource_Weapon_Dust_Rare: 9164
- Upgrade_Resource_Jewellery_Dust_Rare: 9017
- Amulet_Trinity: 8764
- Upgrade_Resource_Fine_Flux: 8271
- Upgrade_Resource_Journeyman_Rune: 8127
- Helmet_Mysterious_Bastion: 6171
- Upgrade_Resource_Armor_Dust_Uncommon: 6084
- Upgrade_Resource_Weapon_Rune: 6015

No orphan items — every table entry dropped at least once.

## Stack_3_mods

Boss/Mythic/lvl 100, modifiers: [] + 3 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 39251 | 15.7% |
| 1 | 44234 | 17.7% |
| 2 | 117153 | 46.87% |
| 3 | 49318 | 19.73% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 6515 | 2.46% |
| Epic | 38777 | 14.62% |
| Rare | 110361 | 41.61% |
| Uncommon | 92360 | 34.83% |
| Common | 2674 | 1.01% |
| Unique | 7550 | 2.85% |
| Mythic | 6963 | 2.63% |

Top drops:
- Body_Feral_Instinct: 12219
- Helmet_Feral_Instinct: 12084
- Gloves_Feral_Instinct: 11469
- Upgrade_Resource_Armor_Dust_Rare: 10183
- Upgrade_Resource_Jewellery_Dust_Rare: 10154
- Upgrade_Resource_Weapon_Dust_Rare: 10018
- Ring_Intelligence: 9991
- Ring_Dexterity: 9979
- Amulet_Trinity: 9791
- Amulet_Ruby: 9494
- Upgrade_Resource_Journeyman_Rune: 9471
- Upgrade_Resource_Fine_Flux: 9317
- Helmet_Mysterious_Bastion: 4206
- Upgrade_Resource_Blacksmith_Rune: 4043
- Upgrade_Resource_Weapon_Rune: 4033

No orphan items — every table entry dropped at least once.

## Stack_5_mods

Boss/Mythic/lvl 100, modifiers: [] + 5 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 74267 | 29.71% |
| 1 | 66586 | 26.64% |
| 2 | 92128 | 36.85% |
| 3 | 17011 | 6.8% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 10743 | 3.9% |
| Epic | 53997 | 19.59% |
| Rare | 115724 | 41.99% |
| Uncommon | 63292 | 22.96% |
| Common | 3878 | 1.41% |
| Unique | 14261 | 5.17% |
| Mythic | 13722 | 4.98% |

Top drops:
- Body_Feral_Instinct: 9492
- Helmet_Feral_Instinct: 9294
- Amulet_Trinity: 9199
- Upgrade_Resource_Journeyman_Rune: 9105
- Upgrade_Resource_Fine_Flux: 9006
- Upgrade_Resource_Jewellery_Dust_Rare: 8883
- Upgrade_Resource_Armor_Dust_Rare: 8854
- Upgrade_Resource_Weapon_Dust_Rare: 8720
- Gloves_Feral_Instinct: 8483
- Ring_Intelligence: 7593
- Ring_Dexterity: 7514
- Amulet_Ruby: 5802
- Boots_Stone_Tread: 4260
- Gloves_Stoneheart: 4233
- Body_Stoneheart: 4030

No orphan items — every table entry dropped at least once.

## Stack_7_mods

Boss/Mythic/lvl 100, modifiers: [] + 7 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 118993 | 47.6% |
| 1 | 71806 | 28.72% |
| 2 | 54222 | 21.69% |
| 3 | 4976 | 1.99% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 16468 | 5.76% |
| Epic | 66127 | 23.15% |
| Rare | 105389 | 36.89% |
| Uncommon | 47410 | 16.59% |
| Common | 5386 | 1.89% |
| Unique | 22888 | 8.01% |
| Mythic | 22021 | 7.71% |

Top drops:
- Upgrade_Resource_Journeyman_Rune: 6971
- Upgrade_Resource_Fine_Flux: 6620
- Amulet_Trinity: 6546
- Body_Feral_Instinct: 5784
- Helmet_Feral_Instinct: 5752
- Upgrade_Resource_Weapon_Dust_Rare: 5517
- Upgrade_Resource_Armor_Dust_Rare: 5448
- Upgrade_Resource_Jewellery_Dust_Rare: 5433
- Crafting_Resource_Linen_Fabric: 5392
- Crafting_Resource_Copper_Ore: 5386
- Crafting_Resource_Deer_Leather: 5385
- Crafting_Resource_Diamond_Gem: 5385
- Gloves_Feral_Instinct: 5213
- Gloves_Stoneheart: 5069
- Boots_Stone_Tread: 4855

No orphan items — every table entry dropped at least once.

## Stack_11_mods

Boss/Mythic/lvl 100, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 205716 | 82.29% |
| 1 | 34428 | 13.77% |
| 2 | 9553 | 3.82% |
| 3 | 303 | 0.12% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 25762 | 8.43% |
| Epic | 79077 | 25.89% |
| Rare | 73072 | 23.92% |
| Uncommon | 40901 | 13.39% |
| Common | 8189 | 2.68% |
| Unique | 39375 | 12.89% |
| Mythic | 39066 | 12.79% |

Top drops:
- Crafting_Resource_Linen_Fabric: 8190
- Crafting_Resource_Copper_Ore: 8189
- Crafting_Resource_Deer_Leather: 8189
- Crafting_Resource_Diamond_Gem: 8189
- Amulet_Onyx_Medallion: 7051
- Helmet_Vital_Core: 7017
- Helmet_Archmage_Cap: 6850
- Gloves_Hunters_Dream: 6796
- Boots_Hunters_Dream: 6787
- Gloves_Vital_Core: 6785
- Body_Vital_Core: 6711
- Boots_Vital_Core: 6709
- Gloves_Aegis: 6706
- Boots_Archmage_Sandals: 6693
- Upgrade_Resource_Grandmaster_Rune: 6668

Orphans (3): Amulet_Recovery_Source, Crafting_Resource_Gold_Ore, Crafting_Resource_Wild_Boar_Leather

## Extreme_Archon

Archon/Mythic/lvl 150, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 237364 | 94.95% |
| 1 | 11559 | 4.62% |
| 2 | 1072 | 0.43% |
| 3 | 5 | 0% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 28891 | 9.43% |
| Epic | 83216 | 27.17% |
| Rare | 67987 | 22.2% |
| Uncommon | 41249 | 13.47% |
| Common | 8463 | 2.76% |
| Unique | 38264 | 12.5% |
| Mythic | 38153 | 12.46% |

Top drops:
- Crafting_Resource_Copper_Ore: 8463
- Crafting_Resource_Deer_Leather: 8463
- Crafting_Resource_Diamond_Gem: 8463
- Crafting_Resource_Linen_Fabric: 8463
- Boots_Vital_Core: 7853
- Boots_Aegis: 7842
- Helmet_Vital_Core: 7774
- Ring_Hell_Servant: 7770
- Ring_of_Creators: 7756
- Helmet_Archmage_Cap: 7730
- Boots_Hunters_Dream: 7721
- Amulet_Onyx_Medallion: 7718
- Boots_Archmage_Sandals: 7709
- Body_Aegis: 7701
- Upgrade_Resource_Armorsmith_Mark: 7673

Orphans (15): Amulet_Garnet, Amulet_Recovery_Source, Body_Mysterious_Bastion, Boots_Mysterious_Bastion, Crafting_Resource_Emerald_Gem, Crafting_Resource_Gold_Ore, Crafting_Resource_Iron_Ore, Crafting_Resource_Ruby_Gem, Crafting_Resource_Silk_Fabric, Crafting_Resource_Wild_Boar_Leather, Gloves_Mysterious_Bastion, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Uncommon

## Extreme_Lvl1

Regular/Uncommon/lvl 1, modifiers: []

| Tier | Rolls | Share |
|---|---|---|
| 0 | 0 | 0% |
| 1 | 0 | 0% |
| 2 | 0 | 0% |
| 3 | 19110 | 100% |

| Rarity | Items | Share |
|---|---|---|
| Epic | 3559 | 18.62% |
| Rare | 6346 | 33.21% |
| Uncommon | 7309 | 38.25% |
| Common | 1896 | 9.92% |

Top drops:
- Crafting_Resource_Copper_Ore: 1896
- Crafting_Resource_Gold_Ore: 1880
- Crafting_Resource_Linen_Fabric: 1862
- Crafting_Resource_Iron_Ore: 1839
- Crafting_Resource_Emerald_Gem: 1827
- Crafting_Resource_Deer_Leather: 1811
- Crafting_Resource_Silk_Fabric: 1803
- Crafting_Resource_Wild_Boar_Leather: 1797
- Crafting_Resource_Diamond_Gem: 1756
- Crafting_Resource_Ruby_Gem: 1749
- Crafting_Resource_Essence_Health: 890

Orphans (77): Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Amulet_Pathfinder_Sign, Amulet_Recovery_Source, Amulet_Ruby, Amulet_Trapper_Talisman, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Feral_Instinct, Body_Hunter_Chestplate, Body_Hunters_Dream, Body_Mysterious_Bastion, Body_Porcupine, Body_Steel_Bastion, Body_Stoneheart, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Feral_Instinct, Boots_Hunter, Boots_Hunters_Dream, Boots_Mysterious_Bastion, Boots_Steel_Greaves, Boots_Stone_Tread, Boots_Vital_Core, Crafting_Resource_Essence_Critical_Chance, Gloves_Aegis, Gloves_Archmage, Gloves_Feral_Instinct, Gloves_Hunter, Gloves_Hunters_Dream, Gloves_Mysterious_Bastion, Gloves_Steel, Gloves_Stoneheart, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Feral_Instinct, Helmet_Hunter, Helmet_Hunters_Dream, Helmet_Mysterious_Bastion, Helmet_Steel, Helmet_Stoneheart, Helmet_Vital_Core, Ring_Dexterity, Ring_Hell_Servant, Ring_Intelligence, Ring_of_Creators, Ring_Of_Fire_Demon, Ring_of_Hunter, Ring_Of_Legionnaire, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Rare, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Blacksmith_Rune, Upgrade_Resource_Fine_Flux, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Jeweler_Rune, Upgrade_Resource_Jewellery_Dust_Rare, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Journeyman_Rune, Upgrade_Resource_Master_Rune, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Quality_Flux, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Rare, Upgrade_Resource_Weapon_Dust_Uncommon, Upgrade_Resource_Weapon_Rune, Upgrade_Resource_Weaponsmith_Mark, Weapon_Bloodthirsty, Weapon_Silent_Fury, Weapon_Simple_Axe, Weapon_Simple_Dagger, Weapon_Simple_Sword
