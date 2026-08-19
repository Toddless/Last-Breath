# Loot simulation report

Seed: `20260710`, scenarios: 17

## Summary

| Scenario | Kills | Difficulty | Items/kill | Value/kill | p95 value | Budget | Value/difficulty | Gold/kill |
|---|---|---|---|---|---|---|---|---|
| Baseline_Regular | 10000 | 0 | 1.92 | 6.33 | 8 | 8.91 | 6.33 | 10.69 |
| Baseline_Boss | 10000 | 0 | 15.58 | 405.61 | 699 | 344.54 | 405.61 | 6.34 |
| Axis_Npc_Modifier_Scale_Double_Health | 10000 | 1.9 | 24.7 | 1039.57 | 1442 | 999.17 | 547.14 | 48.77 |
| Axis_Npc_Modifier_Tier_Upgrade_To_Maximum | 10000 | 0.9 | 23.02 | 2698.99 | 4372 | 654.63 | 2998.87 | 32.71 |
| Axis_Npc_Modifier_Tier_Upgrade_By_One | 10000 | 0.5 | 20.63 | 1193.15 | 1721 | 516.81 | 2386.3 | 16.69 |
| Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource | 10000 | 0.4 | 23.76 | 564.49 | 906 | 482.36 | 1411.21 | 11.63 |
| Axis_Npc_Modifier_Tier_Multiplier_Huge | 10000 | 0.3 | 18.57 | 516.18 | 841 | 447.9 | 1720.59 | 11.82 |
| Axis_Npc_Modifier_Item_Effect_Vampire | 10000 | 0.3 | 18.82 | 516.42 | 850 | 447.9 | 1721.38 | 12.03 |
| Axis_Npc_Modifier_Rarity_Upgrade_Huge | 10000 | 0.3 | 18.85 | 515.07 | 846 | 447.9 | 1716.9 | 12.09 |
| Axis_Npc_Modifier_Min_Rarity_Epic | 10000 | 0.3 | 18.81 | 517.76 | 849 | 447.9 | 1725.88 | 12.48 |
| Stack_1_mods | 10000 | 0.58 | 25.44 | 1583.79 | 2835 | 1650.74 | 2740.29 | 49.03 |
| Stack_3_mods | 10000 | 1.77 | 26.53 | 3132.14 | 5524 | 2897.85 | 1769.47 | 65.33 |
| Stack_5_mods | 10000 | 2.9 | 27.54 | 4830.77 | 8854 | 4083.16 | 1663.97 | 98.42 |
| Stack_7_mods | 10000 | 4.05 | 28.57 | 6792.54 | 11440 | 5279.25 | 1678.61 | 134.68 |
| Stack_11_mods | 10000 | 6.41 | 30.57 | 10086.27 | 12627 | 7751.75 | 1573.51 | 224.84 |
| Extreme_Archon | 10000 | 6.38 | 30.63 | 10849.01 | 12261 | 14645.59 | 1700.33 | 290.72 |
| Extreme_Lvl1 | 10000 | 0 | 1.91 | 5.83 | 6 | 6.75 | 5.83 | 3.69 |

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
| 1 | 1026 | 0.66% |
| 2 | 42091 | 27.02% |
| 3 | 112685 | 72.33% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 148 | 0.09% |
| Epic | 18583 | 11.93% |
| Rare | 52749 | 33.86% |
| Uncommon | 78939 | 50.67% |
| Common | 5383 | 3.46% |

Top drops:
- Crafting_Resource_Gold_Ore: 5497
- Crafting_Resource_Emerald_Gem: 5460
- Crafting_Resource_Ruby_Gem: 5428
- Crafting_Resource_Deer_Leather: 5398
- Crafting_Resource_Copper_Ore: 5383
- Crafting_Resource_Wild_Boar_Leather: 5356
- Crafting_Resource_Iron_Ore: 5348
- Crafting_Resource_Silk_Fabric: 5256
- Crafting_Resource_Diamond_Gem: 5238
- Crafting_Resource_Linen_Fabric: 5204
- Amulet_Recovery_Source: 4140
- Boots_Mysterious_Bastion: 4112
- Gloves_Mysterious_Bastion: 4079
- Crafting_Resource_Essence_Barrier: 4057
- Body_Mysterious_Bastion: 3977

Orphans (31): Amulet_Creators_Nature, Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Belt_Mana_Flow, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Body_Porcupine, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Hunters_Dream, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Aegis, Gloves_Archmage, Gloves_Hunters_Dream, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Hunters_Dream, Helmet_Vital_Core, Ring_Hell_Servant, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Scale_Double_Health

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Scale_Double_Health]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 2247 | 0.91% |
| 1 | 10151 | 4.11% |
| 2 | 100875 | 40.84% |
| 3 | 133718 | 54.14% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 875 | 0.35% |
| Epic | 26354 | 10.67% |
| Rare | 90959 | 36.83% |
| Uncommon | 124691 | 50.48% |
| Common | 3606 | 1.46% |
| Unique | 357 | 0.14% |
| Mythic | 149 | 0.06% |

Top drops:
- Crafting_Resource_Essence_Barrier: 6871
- Helmet_Feral_Instinct: 6773
- Body_Feral_Instinct: 6728
- Gloves_Feral_Instinct: 6568
- Upgrade_Resource_Apprentice_Rune: 6553
- Upgrade_Resource_Jeweler_Rune: 6519
- Upgrade_Resource_Weapon_Rune: 6515
- Upgrade_Resource_Simple_Flux: 6513
- Body_Mysterious_Bastion: 6478
- Crafting_Resource_Essence_Damage: 6474
- Upgrade_Resource_Weapon_Dust_Uncommon: 6454
- Helmet_Mysterious_Bastion: 6440
- Upgrade_Resource_Jewellery_Dust_Uncommon: 6433
- Upgrade_Resource_Blacksmith_Rune: 6385
- Upgrade_Resource_Armor_Dust_Uncommon: 6374

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Tier_Upgrade_To_Maximum

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Upgrade_To_Maximum]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 46572 | 20.23% |
| 1 | 2283 | 0.99% |
| 2 | 63917 | 27.77% |
| 3 | 117410 | 51.01% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 5999 | 2.61% |
| Epic | 24332 | 10.57% |
| Rare | 71820 | 31.2% |
| Uncommon | 106865 | 46.43% |
| Common | 4453 | 1.93% |
| Unique | 8330 | 3.62% |
| Mythic | 8383 | 3.64% |

Top drops:
- Boots_Mysterious_Bastion: 4958
- Upgrade_Resource_Weapon_Rune: 4915
- Gloves_Mysterious_Bastion: 4873
- Helmet_Mysterious_Bastion: 4854
- Crafting_Resource_Essence_Barrier: 4833
- Crafting_Resource_Essence_Damage: 4828
- Upgrade_Resource_Jeweler_Rune: 4821
- Amulet_Recovery_Source: 4820
- Upgrade_Resource_Apprentice_Rune: 4801
- Upgrade_Resource_Armor_Dust_Uncommon: 4780
- Upgrade_Resource_Simple_Flux: 4752
- Upgrade_Resource_Weapon_Dust_Uncommon: 4746
- Upgrade_Resource_Jewellery_Dust_Uncommon: 4746
- Body_Mysterious_Bastion: 4746
- Upgrade_Resource_Blacksmith_Rune: 4709

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Tier_Upgrade_By_One

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Upgrade_By_One]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 964 | 0.47% |
| 1 | 31110 | 15.08% |
| 2 | 104206 | 50.51% |
| 3 | 70013 | 33.94% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 1688 | 0.82% |
| Epic | 28224 | 13.68% |
| Rare | 83331 | 40.39% |
| Uncommon | 89702 | 43.48% |
| Common | 3066 | 1.49% |
| Unique | 160 | 0.08% |
| Mythic | 122 | 0.06% |

Top drops:
- Helmet_Feral_Instinct: 7453
- Gloves_Feral_Instinct: 7432
- Body_Feral_Instinct: 7333
- Amulet_Ruby: 6024
- Ring_Intelligence: 5966
- Ring_Dexterity: 5910
- Recipe_Body_Aegis: 5887
- Upgrade_Resource_Jewellery_Dust_Rare: 5811
- Upgrade_Resource_Weapon_Dust_Rare: 5777
- Upgrade_Resource_Armor_Dust_Rare: 5771
- Belt_Of_Life: 5640
- Upgrade_Resource_Journeyman_Rune: 5492
- Upgrade_Resource_Fine_Flux: 5449
- Crafting_Resource_Essence_Barrier: 3862
- Crafting_Resource_Silk_Fabric: 3156

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Guaranteed_Items_Crafting_Resource]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 272 | 0.14% |
| 1 | 1366 | 0.69% |
| 2 | 58099 | 29.4% |
| 3 | 137856 | 69.77% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 224 | 0.09% |
| Epic | 33167 | 13.96% |
| Rare | 67701 | 28.49% |
| Uncommon | 120298 | 50.63% |
| Common | 16169 | 6.81% |
| Unique | 34 | 0.01% |

Top drops:
- Crafting_Resource_Copper_Ore: 16169
- Crafting_Resource_Deer_Leather: 16153
- Crafting_Resource_Linen_Fabric: 16120
- Crafting_Resource_Diamond_Gem: 16093
- Crafting_Resource_Emerald_Gem: 6288
- Crafting_Resource_Wild_Boar_Leather: 6275
- Crafting_Resource_Gold_Ore: 6201
- Crafting_Resource_Silk_Fabric: 6195
- Crafting_Resource_Ruby_Gem: 6128
- Crafting_Resource_Iron_Ore: 6074
- Crafting_Resource_Essence_Barrier: 5272
- Boots_Mysterious_Bastion: 5265
- Gloves_Mysterious_Bastion: 5252
- Body_Mysterious_Bastion: 5203
- Upgrade_Resource_Simple_Flux: 5132

Orphans (11): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Carapace, Boots_Aegis, Cloak_Creators_Cassock, Helmet_Aegis, Ring_of_Creators, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Tier_Multiplier_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Multiplier_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 331 | 0.18% |
| 1 | 1462 | 0.79% |
| 2 | 53027 | 28.56% |
| 3 | 130859 | 70.48% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 197 | 0.11% |
| Epic | 22061 | 11.88% |
| Rare | 63147 | 34.01% |
| Uncommon | 94148 | 50.7% |
| Common | 6096 | 3.28% |
| Unique | 30 | 0.02% |

Top drops:
- Crafting_Resource_Copper_Ore: 6096
- Crafting_Resource_Wild_Boar_Leather: 6010
- Crafting_Resource_Emerald_Gem: 6003
- Crafting_Resource_Iron_Ore: 5992
- Crafting_Resource_Diamond_Gem: 5986
- Crafting_Resource_Ruby_Gem: 5981
- Crafting_Resource_Gold_Ore: 5967
- Crafting_Resource_Silk_Fabric: 5880
- Crafting_Resource_Deer_Leather: 5865
- Crafting_Resource_Linen_Fabric: 5865
- Body_Mysterious_Bastion: 4908
- Amulet_Recovery_Source: 4888
- Boots_Mysterious_Bastion: 4883
- Crafting_Resource_Essence_Barrier: 4843
- Gloves_Mysterious_Bastion: 4795

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Item_Effect_Vampire

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Item_Effect_Vampire]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 204 | 0.11% |
| 1 | 1259 | 0.67% |
| 2 | 54052 | 28.72% |
| 3 | 132720 | 70.51% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 194 | 0.1% |
| Epic | 22279 | 11.84% |
| Rare | 64504 | 34.27% |
| Uncommon | 95156 | 50.55% |
| Common | 6084 | 3.23% |
| Unique | 18 | 0.01% |

Top drops:
- Crafting_Resource_Gold_Ore: 6194
- Crafting_Resource_Emerald_Gem: 6114
- Crafting_Resource_Copper_Ore: 6084
- Crafting_Resource_Silk_Fabric: 6038
- Crafting_Resource_Deer_Leather: 6020
- Crafting_Resource_Diamond_Gem: 6012
- Crafting_Resource_Ruby_Gem: 6008
- Crafting_Resource_Iron_Ore: 6006
- Crafting_Resource_Wild_Boar_Leather: 5956
- Crafting_Resource_Linen_Fabric: 5830
- Body_Mysterious_Bastion: 5040
- Crafting_Resource_Essence_Barrier: 4982
- Boots_Mysterious_Bastion: 4980
- Amulet_Recovery_Source: 4933
- Gloves_Mysterious_Bastion: 4914

Orphans (21): Amulet_Creators_Nature, Amulet_Goliath_Seal, Belt_Mana_Flow, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Rarity_Upgrade_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Rarity_Upgrade_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 217 | 0.12% |
| 1 | 1256 | 0.67% |
| 2 | 54003 | 28.65% |
| 3 | 133018 | 70.57% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 179 | 0.09% |
| Epic | 26991 | 14.32% |
| Rare | 62574 | 33.2% |
| Uncommon | 92722 | 49.19% |
| Common | 6008 | 3.19% |
| Unique | 20 | 0.01% |

Top drops:
- Crafting_Resource_Silk_Fabric: 6158
- Crafting_Resource_Wild_Boar_Leather: 6064
- Crafting_Resource_Diamond_Gem: 6047
- Crafting_Resource_Deer_Leather: 6035
- Crafting_Resource_Copper_Ore: 6008
- Crafting_Resource_Iron_Ore: 6000
- Crafting_Resource_Ruby_Gem: 5992
- Crafting_Resource_Emerald_Gem: 5990
- Crafting_Resource_Gold_Ore: 5983
- Crafting_Resource_Linen_Fabric: 5980
- Body_Mysterious_Bastion: 5106
- Gloves_Mysterious_Bastion: 5050
- Boots_Mysterious_Bastion: 4991
- Crafting_Resource_Essence_Damage: 4949
- Amulet_Recovery_Source: 4948

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Min_Rarity_Epic

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Min_Rarity_Epic]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 196 | 0.1% |
| 1 | 1241 | 0.66% |
| 2 | 54163 | 28.79% |
| 3 | 132524 | 70.45% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 199 | 0.11% |
| Epic | 63066 | 33.52% |
| Rare | 50647 | 26.92% |
| Uncommon | 68161 | 36.23% |
| Common | 6035 | 3.21% |
| Unique | 16 | 0.01% |

Top drops:
- Crafting_Resource_Gold_Ore: 6103
- Crafting_Resource_Deer_Leather: 6102
- Crafting_Resource_Iron_Ore: 6096
- Crafting_Resource_Silk_Fabric: 6055
- Crafting_Resource_Wild_Boar_Leather: 6048
- Crafting_Resource_Copper_Ore: 6035
- Crafting_Resource_Ruby_Gem: 6008
- Crafting_Resource_Linen_Fabric: 6007
- Crafting_Resource_Emerald_Gem: 5999
- Crafting_Resource_Diamond_Gem: 5993
- Body_Mysterious_Bastion: 4952
- Crafting_Resource_Essence_Barrier: 4951
- Boots_Mysterious_Bastion: 4937
- Gloves_Mysterious_Bastion: 4936
- Upgrade_Resource_Weapon_Rune: 4891

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Stack_1_mods

Boss/Mythic/lvl 100, modifiers: [] + 1 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 12177 | 4.88% |
| 1 | 19339 | 7.75% |
| 2 | 112097 | 44.95% |
| 3 | 105778 | 42.41% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 3192 | 1.25% |
| Epic | 30194 | 11.87% |
| Rare | 92663 | 36.42% |
| Uncommon | 121797 | 47.88% |
| Common | 2652 | 1.04% |
| Unique | 2164 | 0.85% |
| Mythic | 1734 | 0.68% |

Top drops:
- Body_Feral_Instinct: 8181
- Helmet_Feral_Instinct: 8086
- Gloves_Feral_Instinct: 8082
- Amulet_Ruby: 7140
- Ring_Intelligence: 6999
- Ring_Dexterity: 6937
- Upgrade_Resource_Jewellery_Dust_Rare: 6819
- Upgrade_Resource_Weapon_Dust_Rare: 6684
- Upgrade_Resource_Armor_Dust_Rare: 6653
- Amulet_Trinity: 6292
- Upgrade_Resource_Jewellery_Dust_Uncommon: 6099
- Helmet_Mysterious_Bastion: 6073
- Upgrade_Resource_Apprentice_Rune: 6047
- Upgrade_Resource_Armor_Dust_Uncommon: 6014
- Upgrade_Resource_Weapon_Rune: 6007

No orphan items — every table entry dropped at least once.

## Stack_3_mods

Boss/Mythic/lvl 100, modifiers: [] + 3 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 37895 | 15.16% |
| 1 | 42904 | 17.17% |
| 2 | 119971 | 48% |
| 3 | 49180 | 19.68% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 9345 | 3.52% |
| Epic | 41108 | 15.5% |
| Rare | 102586 | 38.67% |
| Uncommon | 96450 | 36.36% |
| Common | 2728 | 1.03% |
| Unique | 6688 | 2.52% |
| Mythic | 6350 | 2.39% |

Top drops:
- Helmet_Feral_Instinct: 9202
- Body_Feral_Instinct: 9122
- Gloves_Feral_Instinct: 8806
- Ring_Dexterity: 7350
- Upgrade_Resource_Weapon_Dust_Rare: 7335
- Upgrade_Resource_Jewellery_Dust_Rare: 7276
- Upgrade_Resource_Armor_Dust_Rare: 7201
- Ring_Intelligence: 7136
- Amulet_Trinity: 7133
- Amulet_Ruby: 6908
- Upgrade_Resource_Fine_Flux: 6820
- Upgrade_Resource_Journeyman_Rune: 6755
- Helmet_Mysterious_Bastion: 4101
- Upgrade_Resource_Blacksmith_Rune: 3996
- Upgrade_Resource_Weapon_Rune: 3925

No orphan items — every table entry dropped at least once.

## Stack_5_mods

Boss/Mythic/lvl 100, modifiers: [] + 5 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 70742 | 28.3% |
| 1 | 66144 | 26.46% |
| 2 | 96009 | 38.41% |
| 3 | 17088 | 6.84% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 16652 | 6.05% |
| Epic | 56108 | 20.37% |
| Rare | 104142 | 37.81% |
| Uncommon | 69507 | 25.24% |
| Common | 3958 | 1.44% |
| Unique | 12875 | 4.67% |
| Mythic | 12174 | 4.42% |

Top drops:
- Helmet_Feral_Instinct: 7564
- Body_Feral_Instinct: 7367
- Upgrade_Resource_Journeyman_Rune: 6769
- Upgrade_Resource_Fine_Flux: 6760
- Gloves_Feral_Instinct: 6470
- Amulet_Trinity: 6391
- Upgrade_Resource_Weapon_Dust_Rare: 5770
- Upgrade_Resource_Jewellery_Dust_Rare: 5769
- Upgrade_Resource_Armor_Dust_Rare: 5642
- Ring_Dexterity: 5101
- Ring_Intelligence: 5090
- Amulet_Ruby: 4056
- Crafting_Resource_Copper_Ore: 3958
- Crafting_Resource_Deer_Leather: 3947
- Crafting_Resource_Diamond_Gem: 3936

No orphan items — every table entry dropped at least once.

## Stack_7_mods

Boss/Mythic/lvl 100, modifiers: [] + 7 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 115399 | 46.16% |
| 1 | 72663 | 29.07% |
| 2 | 56657 | 22.66% |
| 3 | 5269 | 2.11% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 24936 | 8.73% |
| Epic | 66770 | 23.37% |
| Rare | 94406 | 33.04% |
| Uncommon | 53496 | 18.72% |
| Common | 5382 | 1.88% |
| Unique | 20675 | 7.24% |
| Mythic | 20079 | 7.03% |

Top drops:
- Crafting_Resource_Diamond_Gem: 5388
- Crafting_Resource_Copper_Ore: 5382
- Crafting_Resource_Deer_Leather: 5378
- Crafting_Resource_Linen_Fabric: 5377
- Upgrade_Resource_Journeyman_Rune: 5122
- Helmet_Feral_Instinct: 4977
- Body_Feral_Instinct: 4878
- Upgrade_Resource_Fine_Flux: 4820
- Gloves_Stoneheart: 4762
- Boots_Stone_Tread: 4589
- Gloves_Feral_Instinct: 4260
- Amulet_Trinity: 4168
- Body_Stoneheart: 4008
- Helmet_Vital_Core: 3838
- Crafting_Resource_Essence_Barrier: 3758

No orphan items — every table entry dropped at least once.

## Stack_11_mods

Boss/Mythic/lvl 100, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 202451 | 80.98% |
| 1 | 36610 | 14.64% |
| 2 | 10650 | 4.26% |
| 3 | 289 | 0.12% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 38154 | 12.48% |
| Epic | 75771 | 24.78% |
| Rare | 69109 | 22.6% |
| Uncommon | 41944 | 13.72% |
| Common | 8302 | 2.72% |
| Unique | 36627 | 11.98% |
| Mythic | 35822 | 11.72% |

Top drops:
- Crafting_Resource_Deer_Leather: 8303
- Crafting_Resource_Copper_Ore: 8302
- Crafting_Resource_Diamond_Gem: 8302
- Crafting_Resource_Linen_Fabric: 8302
- Amulet_Onyx_Medallion: 6489
- Helmet_Vital_Core: 6380
- Helmet_Archmage_Cap: 6368
- Gloves_Vital_Core: 6367
- Body_Vital_Core: 6351
- Ring_Hell_Servant: 6350
- Gloves_Archmage: 6269
- Body_Archmage_Mantle: 6266
- Boots_Archmage_Sandals: 6230
- Gloves_Hunters_Dream: 6212
- Boots_Hunters_Dream: 6194

Orphans (6): Crafting_Resource_Emerald_Gem, Crafting_Resource_Gold_Ore, Crafting_Resource_Iron_Ore, Crafting_Resource_Ruby_Gem, Crafting_Resource_Wild_Boar_Leather, Gloves_Mysterious_Bastion

## Extreme_Archon

Archon/Mythic/lvl 150, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 237986 | 95.19% |
| 1 | 10878 | 4.35% |
| 2 | 1136 | 0.45% |
| 3 | 0 | 0% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 41814 | 13.65% |
| Epic | 79334 | 25.9% |
| Rare | 63938 | 20.88% |
| Uncommon | 40893 | 13.35% |
| Common | 8306 | 2.71% |
| Unique | 35948 | 11.74% |
| Mythic | 36034 | 11.77% |

Top drops:
- Crafting_Resource_Copper_Ore: 8306
- Crafting_Resource_Deer_Leather: 8306
- Crafting_Resource_Diamond_Gem: 8306
- Crafting_Resource_Linen_Fabric: 8306
- Amulet_Lazurite: 7421
- Gloves_Hunters_Dream: 7381
- Amulet_Onyx_Medallion: 7327
- Upgrade_Resource_Weaponsmith_Mark: 7319
- Helmet_Archmage_Cap: 7291
- Weapon_All_Cutting: 7289
- Ring_Of_Limitless_Magic: 7278
- Boots_Vital_Core: 7255
- Upgrade_Resource_Armorsmith_Mark: 7254
- Amulet_Goliath_Seal: 7238
- Body_Vital_Core: 7236

Orphans (17): Amulet_Garnet, Amulet_Recovery_Source, Body_Mysterious_Bastion, Boots_Mysterious_Bastion, Crafting_Resource_Emerald_Gem, Crafting_Resource_Gold_Ore, Crafting_Resource_Iron_Ore, Crafting_Resource_Ruby_Gem, Crafting_Resource_Silk_Fabric, Crafting_Resource_Wild_Boar_Leather, Gloves_Mysterious_Bastion, Helmet_Mysterious_Bastion, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Uncommon

## Extreme_Lvl1

Regular/Uncommon/lvl 1, modifiers: []

| Tier | Rolls | Share |
|---|---|---|
| 0 | 0 | 0% |
| 1 | 0 | 0% |
| 2 | 0 | 0% |
| 3 | 19135 | 100% |

| Rarity | Items | Share |
|---|---|---|
| Epic | 3681 | 19.24% |
| Rare | 6287 | 32.86% |
| Uncommon | 7386 | 38.6% |
| Common | 1781 | 9.31% |

Top drops:
- Crafting_Resource_Iron_Ore: 1894
- Crafting_Resource_Diamond_Gem: 1875
- Crafting_Resource_Linen_Fabric: 1863
- Crafting_Resource_Deer_Leather: 1846
- Crafting_Resource_Ruby_Gem: 1832
- Crafting_Resource_Emerald_Gem: 1816
- Crafting_Resource_Silk_Fabric: 1806
- Crafting_Resource_Wild_Boar_Leather: 1783
- Crafting_Resource_Copper_Ore: 1781
- Crafting_Resource_Gold_Ore: 1774
- Crafting_Resource_Essence_Health: 865

Orphans (77): Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Amulet_Pathfinder_Sign, Amulet_Recovery_Source, Amulet_Ruby, Amulet_Trapper_Talisman, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Feral_Instinct, Body_Hunter_Chestplate, Body_Hunters_Dream, Body_Mysterious_Bastion, Body_Porcupine, Body_Steel_Bastion, Body_Stoneheart, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Feral_Instinct, Boots_Hunter, Boots_Hunters_Dream, Boots_Mysterious_Bastion, Boots_Steel_Greaves, Boots_Stone_Tread, Boots_Vital_Core, Crafting_Resource_Essence_Critical_Chance, Gloves_Aegis, Gloves_Archmage, Gloves_Feral_Instinct, Gloves_Hunter, Gloves_Hunters_Dream, Gloves_Mysterious_Bastion, Gloves_Steel, Gloves_Stoneheart, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Feral_Instinct, Helmet_Hunter, Helmet_Hunters_Dream, Helmet_Mysterious_Bastion, Helmet_Steel, Helmet_Stoneheart, Helmet_Vital_Core, Ring_Dexterity, Ring_Hell_Servant, Ring_Intelligence, Ring_of_Creators, Ring_Of_Fire_Demon, Ring_of_Hunter, Ring_Of_Legionnaire, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Rare, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Blacksmith_Rune, Upgrade_Resource_Fine_Flux, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Jeweler_Rune, Upgrade_Resource_Jewellery_Dust_Rare, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Journeyman_Rune, Upgrade_Resource_Master_Rune, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Quality_Flux, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Rare, Upgrade_Resource_Weapon_Dust_Uncommon, Upgrade_Resource_Weapon_Rune, Upgrade_Resource_Weaponsmith_Mark, Weapon_Bloodthirsty, Weapon_Silent_Fury, Weapon_Simple_Axe, Weapon_Simple_Dagger, Weapon_Simple_Sword
