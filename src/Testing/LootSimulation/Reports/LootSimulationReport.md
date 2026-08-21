# Loot simulation report

Seed: `20260710`, scenarios: 17

## Summary

| Scenario | Kills | Difficulty | Items/kill | Value/kill | p95 value | Budget | Value/difficulty | Gold/kill |
|---|---|---|---|---|---|---|---|---|
| Baseline_Regular | 10000 | 0 | 1.92 | 6.33 | 8 | 8.91 | 6.33 | 10.69 |
| Baseline_Boss | 10000 | 0 | 15.47 | 405.53 | 700 | 344.54 | 405.53 | 6.37 |
| Axis_Npc_Modifier_Scale_Double_Health | 10000 | 1.9 | 24.7 | 1045.58 | 1445 | 999.17 | 550.3 | 49.11 |
| Axis_Npc_Modifier_Tier_Upgrade_To_Maximum | 10000 | 0.9 | 23.08 | 2708.17 | 4368 | 654.63 | 3009.08 | 33.3 |
| Axis_Npc_Modifier_Tier_Upgrade_By_One | 10000 | 0.5 | 20.65 | 1199.91 | 1719 | 516.81 | 2399.82 | 17.13 |
| Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource | 10000 | 0.4 | 23.76 | 564.46 | 912 | 482.36 | 1411.16 | 11.85 |
| Axis_Npc_Modifier_Tier_Multiplier_Huge | 10000 | 0.3 | 18.56 | 514.95 | 842 | 447.9 | 1716.5 | 12.08 |
| Axis_Npc_Modifier_Item_Effect_Vampire | 10000 | 0.3 | 18.74 | 515.84 | 858 | 447.9 | 1719.48 | 11.98 |
| Axis_Npc_Modifier_Rarity_Upgrade_Huge | 10000 | 0.3 | 18.88 | 514.67 | 848 | 447.9 | 1715.56 | 12.23 |
| Axis_Npc_Modifier_Min_Rarity_Epic | 10000 | 0.3 | 18.86 | 517.47 | 847 | 447.9 | 1724.91 | 12.65 |
| Stack_1_mods | 10000 | 0.59 | 25.46 | 1594.48 | 2846 | 1663.09 | 2703.57 | 48.99 |
| Stack_3_mods | 10000 | 1.75 | 26.54 | 3100.95 | 5473 | 2874.55 | 1774.17 | 64.09 |
| Stack_5_mods | 10000 | 2.93 | 27.53 | 4898.14 | 8926 | 4114.53 | 1669.92 | 100.65 |
| Stack_7_mods | 10000 | 4.1 | 28.62 | 6890.19 | 11551 | 5335.97 | 1680.23 | 135.47 |
| Stack_11_mods | 10000 | 6.45 | 30.64 | 10099.84 | 12648 | 7790.9 | 1566.49 | 226.04 |
| Extreme_Archon | 10000 | 6.42 | 30.6 | 10856.9 | 12271 | 14728.58 | 1690.49 | 290.89 |
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
| 1 | 1010 | 0.65% |
| 2 | 41988 | 27.14% |
| 3 | 111687 | 72.2% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 123 | 0.08% |
| Epic | 18495 | 11.96% |
| Rare | 52060 | 33.66% |
| Uncommon | 78701 | 50.88% |
| Common | 5306 | 3.43% |

Top drops:
- Crafting_Resource_Ruby_Gem: 5348
- Crafting_Resource_Copper_Ore: 5306
- Crafting_Resource_Gold_Ore: 5306
- Crafting_Resource_Iron_Ore: 5301
- Crafting_Resource_Linen_Fabric: 5280
- Crafting_Resource_Deer_Leather: 5278
- Crafting_Resource_Emerald_Gem: 5241
- Crafting_Resource_Wild_Boar_Leather: 5224
- Crafting_Resource_Diamond_Gem: 5216
- Crafting_Resource_Silk_Fabric: 5215
- Amulet_Recovery_Source: 4185
- Gloves_Mysterious_Bastion: 4054
- Boots_Mysterious_Bastion: 4033
- Crafting_Resource_Essence_Barrier: 4019
- Body_Mysterious_Bastion: 3929

Orphans (31): Amulet_Creators_Nature, Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Belt_Mana_Flow, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Body_Porcupine, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Hunters_Dream, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Aegis, Gloves_Archmage, Gloves_Hunters_Dream, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Hunters_Dream, Helmet_Vital_Core, Ring_Hell_Servant, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Scale_Double_Health

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Scale_Double_Health]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 2271 | 0.92% |
| 1 | 10232 | 4.14% |
| 2 | 100620 | 40.74% |
| 3 | 133835 | 54.19% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 862 | 0.35% |
| Epic | 26353 | 10.67% |
| Rare | 90992 | 36.85% |
| Uncommon | 124571 | 50.44% |
| Common | 3684 | 1.49% |
| Unique | 364 | 0.15% |
| Mythic | 132 | 0.05% |

Top drops:
- Crafting_Resource_Essence_Barrier: 6948
- Body_Feral_Instinct: 6749
- Helmet_Feral_Instinct: 6709
- Gloves_Feral_Instinct: 6636
- Upgrade_Resource_Jeweler_Rune: 6560
- Upgrade_Resource_Blacksmith_Rune: 6545
- Upgrade_Resource_Jewellery_Dust_Uncommon: 6511
- Upgrade_Resource_Weapon_Rune: 6476
- Helmet_Mysterious_Bastion: 6436
- Upgrade_Resource_Weapon_Dust_Uncommon: 6433
- Upgrade_Resource_Armor_Dust_Uncommon: 6432
- Upgrade_Resource_Simple_Flux: 6415
- Crafting_Resource_Essence_Damage: 6391
- Gloves_Mysterious_Bastion: 6373
- Amulet_Recovery_Source: 6373

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Tier_Upgrade_To_Maximum

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Upgrade_To_Maximum]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 46766 | 20.27% |
| 1 | 2252 | 0.98% |
| 2 | 63642 | 27.58% |
| 3 | 118102 | 51.18% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 5936 | 2.57% |
| Epic | 24311 | 10.54% |
| Rare | 71720 | 31.08% |
| Uncommon | 107412 | 46.55% |
| Common | 4543 | 1.97% |
| Unique | 8379 | 3.63% |
| Mythic | 8461 | 3.67% |

Top drops:
- Crafting_Resource_Essence_Barrier: 4910
- Amulet_Recovery_Source: 4903
- Gloves_Mysterious_Bastion: 4888
- Upgrade_Resource_Weapon_Rune: 4885
- Upgrade_Resource_Apprentice_Rune: 4874
- Boots_Mysterious_Bastion: 4872
- Crafting_Resource_Essence_Damage: 4870
- Body_Mysterious_Bastion: 4859
- Helmet_Mysterious_Bastion: 4858
- Upgrade_Resource_Weapon_Dust_Uncommon: 4856
- Upgrade_Resource_Blacksmith_Rune: 4839
- Upgrade_Resource_Armor_Dust_Uncommon: 4811
- Upgrade_Resource_Simple_Flux: 4805
- Upgrade_Resource_Jewellery_Dust_Uncommon: 4782
- Upgrade_Resource_Jeweler_Rune: 4777

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Tier_Upgrade_By_One

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Upgrade_By_One]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 982 | 0.48% |
| 1 | 31279 | 15.15% |
| 2 | 104024 | 50.37% |
| 3 | 70239 | 34.01% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 1694 | 0.82% |
| Epic | 28252 | 13.68% |
| Rare | 83330 | 40.35% |
| Uncommon | 89977 | 43.57% |
| Common | 3021 | 1.46% |
| Unique | 146 | 0.07% |
| Mythic | 104 | 0.05% |

Top drops:
- Helmet_Feral_Instinct: 7545
- Body_Feral_Instinct: 7433
- Gloves_Feral_Instinct: 7415
- Ring_Dexterity: 5895
- Ring_Intelligence: 5878
- Upgrade_Resource_Weapon_Dust_Rare: 5870
- Upgrade_Resource_Jewellery_Dust_Rare: 5843
- Amulet_Ruby: 5841
- Recipe_Body_Aegis: 5745
- Upgrade_Resource_Armor_Dust_Rare: 5719
- Belt_Of_Life: 5679
- Upgrade_Resource_Fine_Flux: 5594
- Upgrade_Resource_Journeyman_Rune: 5499
- Crafting_Resource_Essence_Barrier: 4047
- Crafting_Resource_Gold_Ore: 3162

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Guaranteed_Items_Crafting_Resource]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 285 | 0.14% |
| 1 | 1312 | 0.66% |
| 2 | 58142 | 29.43% |
| 3 | 137854 | 69.77% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 239 | 0.1% |
| Epic | 33207 | 13.98% |
| Rare | 67506 | 28.41% |
| Uncommon | 120470 | 50.7% |
| Common | 16138 | 6.79% |
| Unique | 33 | 0.01% |

Top drops:
- Crafting_Resource_Deer_Leather: 16171
- Crafting_Resource_Linen_Fabric: 16168
- Crafting_Resource_Copper_Ore: 16138
- Crafting_Resource_Diamond_Gem: 16092
- Crafting_Resource_Iron_Ore: 6237
- Crafting_Resource_Ruby_Gem: 6169
- Crafting_Resource_Gold_Ore: 6160
- Crafting_Resource_Wild_Boar_Leather: 6115
- Crafting_Resource_Silk_Fabric: 6084
- Crafting_Resource_Emerald_Gem: 6071
- Body_Mysterious_Bastion: 5288
- Boots_Mysterious_Bastion: 5284
- Amulet_Recovery_Source: 5271
- Gloves_Mysterious_Bastion: 5243
- Crafting_Resource_Essence_Barrier: 5208

Orphans (11): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Carapace, Boots_Aegis, Cloak_Creators_Cassock, Helmet_Aegis, Ring_of_Creators, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Tier_Multiplier_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Multiplier_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 325 | 0.18% |
| 1 | 1480 | 0.8% |
| 2 | 53103 | 28.61% |
| 3 | 130730 | 70.42% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 214 | 0.12% |
| Epic | 21862 | 11.78% |
| Rare | 63401 | 34.15% |
| Uncommon | 94183 | 50.73% |
| Common | 5949 | 3.2% |
| Unique | 29 | 0.02% |

Top drops:
- Crafting_Resource_Gold_Ore: 6051
- Crafting_Resource_Wild_Boar_Leather: 6003
- Crafting_Resource_Emerald_Gem: 5997
- Crafting_Resource_Silk_Fabric: 5977
- Crafting_Resource_Copper_Ore: 5949
- Crafting_Resource_Deer_Leather: 5928
- Crafting_Resource_Ruby_Gem: 5890
- Crafting_Resource_Iron_Ore: 5858
- Crafting_Resource_Diamond_Gem: 5840
- Crafting_Resource_Linen_Fabric: 5813
- Boots_Mysterious_Bastion: 4979
- Body_Mysterious_Bastion: 4907
- Gloves_Mysterious_Bastion: 4901
- Amulet_Recovery_Source: 4870
- Crafting_Resource_Essence_Barrier: 4811

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Item_Effect_Vampire

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Item_Effect_Vampire]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 241 | 0.13% |
| 1 | 1280 | 0.68% |
| 2 | 53846 | 28.73% |
| 3 | 132032 | 70.46% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 218 | 0.12% |
| Epic | 21970 | 11.72% |
| Rare | 64191 | 34.25% |
| Uncommon | 94995 | 50.69% |
| Common | 6003 | 3.2% |
| Unique | 22 | 0.01% |

Top drops:
- Crafting_Resource_Gold_Ore: 6129
- Crafting_Resource_Wild_Boar_Leather: 6117
- Crafting_Resource_Iron_Ore: 6061
- Crafting_Resource_Ruby_Gem: 6040
- Crafting_Resource_Copper_Ore: 6003
- Crafting_Resource_Diamond_Gem: 5982
- Crafting_Resource_Deer_Leather: 5965
- Crafting_Resource_Linen_Fabric: 5961
- Crafting_Resource_Emerald_Gem: 5945
- Crafting_Resource_Silk_Fabric: 5932
- Amulet_Recovery_Source: 4991
- Crafting_Resource_Essence_Barrier: 4982
- Gloves_Mysterious_Bastion: 4965
- Body_Mysterious_Bastion: 4946
- Upgrade_Resource_Jeweler_Rune: 4881

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Rarity_Upgrade_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Rarity_Upgrade_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 218 | 0.12% |
| 1 | 1222 | 0.65% |
| 2 | 53934 | 28.56% |
| 3 | 133439 | 70.67% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 207 | 0.11% |
| Epic | 26449 | 14.01% |
| Rare | 63069 | 33.4% |
| Uncommon | 92965 | 49.24% |
| Common | 6110 | 3.24% |
| Unique | 13 | 0.01% |

Top drops:
- Crafting_Resource_Deer_Leather: 6143
- Crafting_Resource_Iron_Ore: 6130
- Crafting_Resource_Copper_Ore: 6110
- Crafting_Resource_Emerald_Gem: 6078
- Crafting_Resource_Linen_Fabric: 6061
- Crafting_Resource_Wild_Boar_Leather: 6040
- Crafting_Resource_Gold_Ore: 6036
- Crafting_Resource_Ruby_Gem: 6009
- Crafting_Resource_Diamond_Gem: 5971
- Crafting_Resource_Silk_Fabric: 5966
- Boots_Mysterious_Bastion: 5058
- Body_Mysterious_Bastion: 5041
- Gloves_Mysterious_Bastion: 5035
- Amulet_Recovery_Source: 4931
- Crafting_Resource_Essence_Damage: 4922

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Min_Rarity_Epic

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Min_Rarity_Epic]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 217 | 0.12% |
| 1 | 1293 | 0.69% |
| 2 | 53997 | 28.63% |
| 3 | 133088 | 70.57% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 202 | 0.11% |
| Epic | 63213 | 33.52% |
| Rare | 50603 | 26.83% |
| Uncommon | 68492 | 36.32% |
| Common | 6069 | 3.22% |
| Unique | 16 | 0.01% |

Top drops:
- Crafting_Resource_Iron_Ore: 6201
- Crafting_Resource_Wild_Boar_Leather: 6133
- Crafting_Resource_Ruby_Gem: 6120
- Crafting_Resource_Deer_Leather: 6089
- Crafting_Resource_Copper_Ore: 6069
- Crafting_Resource_Emerald_Gem: 6042
- Crafting_Resource_Linen_Fabric: 6031
- Crafting_Resource_Gold_Ore: 6002
- Crafting_Resource_Diamond_Gem: 5975
- Crafting_Resource_Silk_Fabric: 5932
- Body_Mysterious_Bastion: 5054
- Boots_Mysterious_Bastion: 5044
- Crafting_Resource_Essence_Barrier: 4915
- Gloves_Mysterious_Bastion: 4905
- Amulet_Recovery_Source: 4888

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Stack_1_mods

Boss/Mythic/lvl 100, modifiers: [] + 1 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 12291 | 4.93% |
| 1 | 19808 | 7.94% |
| 2 | 111155 | 44.58% |
| 3 | 106095 | 42.55% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 3207 | 1.26% |
| Epic | 30533 | 11.99% |
| Rare | 92270 | 36.25% |
| Uncommon | 121916 | 47.89% |
| Common | 2680 | 1.05% |
| Unique | 2131 | 0.84% |
| Mythic | 1820 | 0.71% |

Top drops:
- Helmet_Feral_Instinct: 8191
- Gloves_Feral_Instinct: 8065
- Body_Feral_Instinct: 8030
- Amulet_Ruby: 7014
- Ring_Intelligence: 6844
- Ring_Dexterity: 6790
- Upgrade_Resource_Armor_Dust_Rare: 6738
- Upgrade_Resource_Jewellery_Dust_Rare: 6711
- Upgrade_Resource_Weapon_Dust_Rare: 6675
- Amulet_Trinity: 6334
- Upgrade_Resource_Armor_Dust_Uncommon: 6176
- Helmet_Mysterious_Bastion: 6054
- Upgrade_Resource_Weapon_Dust_Uncommon: 6052
- Upgrade_Resource_Apprentice_Rune: 6045
- Upgrade_Resource_Jewellery_Dust_Uncommon: 6021

No orphan items — every table entry dropped at least once.

## Stack_3_mods

Boss/Mythic/lvl 100, modifiers: [] + 3 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 37310 | 14.93% |
| 1 | 42287 | 16.92% |
| 2 | 120544 | 48.23% |
| 3 | 49804 | 19.93% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 9283 | 3.5% |
| Epic | 41761 | 15.73% |
| Rare | 102267 | 38.53% |
| Uncommon | 96151 | 36.23% |
| Common | 2857 | 1.08% |
| Unique | 6800 | 2.56% |
| Mythic | 6284 | 2.37% |

Top drops:
- Body_Feral_Instinct: 9234
- Helmet_Feral_Instinct: 9193
- Gloves_Feral_Instinct: 8663
- Upgrade_Resource_Armor_Dust_Rare: 7279
- Ring_Dexterity: 7237
- Upgrade_Resource_Jewellery_Dust_Rare: 7236
- Upgrade_Resource_Weapon_Dust_Rare: 7203
- Ring_Intelligence: 7199
- Amulet_Trinity: 7091
- Amulet_Ruby: 7050
- Upgrade_Resource_Journeyman_Rune: 6906
- Upgrade_Resource_Fine_Flux: 6867
- Helmet_Mysterious_Bastion: 4224
- Upgrade_Resource_Blacksmith_Rune: 3994
- Upgrade_Resource_Jeweler_Rune: 3927

No orphan items — every table entry dropped at least once.

## Stack_5_mods

Boss/Mythic/lvl 100, modifiers: [] + 5 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 72328 | 28.93% |
| 1 | 65459 | 26.18% |
| 2 | 95428 | 38.17% |
| 3 | 16785 | 6.71% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 16572 | 6.02% |
| Epic | 55608 | 20.2% |
| Rare | 103858 | 37.72% |
| Uncommon | 69802 | 25.35% |
| Common | 3869 | 1.41% |
| Unique | 13145 | 4.77% |
| Mythic | 12473 | 4.53% |

Top drops:
- Body_Feral_Instinct: 7385
- Helmet_Feral_Instinct: 7303
- Gloves_Feral_Instinct: 6674
- Upgrade_Resource_Journeyman_Rune: 6647
- Upgrade_Resource_Fine_Flux: 6589
- Amulet_Trinity: 6357
- Upgrade_Resource_Jewellery_Dust_Rare: 5740
- Upgrade_Resource_Armor_Dust_Rare: 5654
- Upgrade_Resource_Weapon_Dust_Rare: 5569
- Ring_Dexterity: 5109
- Ring_Intelligence: 5056
- Amulet_Ruby: 4021
- Boots_Stone_Tread: 3913
- Crafting_Resource_Copper_Ore: 3869
- Crafting_Resource_Diamond_Gem: 3859

No orphan items — every table entry dropped at least once.

## Stack_7_mods

Boss/Mythic/lvl 100, modifiers: [] + 7 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 117796 | 47.12% |
| 1 | 72270 | 28.91% |
| 2 | 54614 | 21.85% |
| 3 | 5320 | 2.13% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 25132 | 8.78% |
| Epic | 65878 | 23.02% |
| Rare | 94477 | 33.02% |
| Uncommon | 53459 | 18.68% |
| Common | 5405 | 1.89% |
| Unique | 21319 | 7.45% |
| Mythic | 20489 | 7.16% |

Top drops:
- Crafting_Resource_Copper_Ore: 5405
- Crafting_Resource_Diamond_Gem: 5399
- Crafting_Resource_Linen_Fabric: 5399
- Crafting_Resource_Deer_Leather: 5397
- Body_Feral_Instinct: 4889
- Helmet_Feral_Instinct: 4851
- Upgrade_Resource_Journeyman_Rune: 4798
- Gloves_Stoneheart: 4745
- Upgrade_Resource_Fine_Flux: 4715
- Boots_Stone_Tread: 4596
- Body_Stoneheart: 4190
- Gloves_Feral_Instinct: 4105
- Amulet_Trinity: 4021
- Helmet_Vital_Core: 3940
- Amulet_Onyx_Medallion: 3875

No orphan items — every table entry dropped at least once.

## Stack_11_mods

Boss/Mythic/lvl 100, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 202884 | 81.15% |
| 1 | 35759 | 14.3% |
| 2 | 10983 | 4.39% |
| 3 | 374 | 0.15% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 37402 | 12.21% |
| Epic | 76843 | 25.08% |
| Rare | 68910 | 22.49% |
| Uncommon | 41984 | 13.7% |
| Common | 8482 | 2.77% |
| Unique | 36792 | 12.01% |
| Mythic | 36033 | 11.76% |

Top drops:
- Crafting_Resource_Copper_Ore: 8482
- Crafting_Resource_Deer_Leather: 8482
- Crafting_Resource_Diamond_Gem: 8482
- Crafting_Resource_Linen_Fabric: 8482
- Amulet_Onyx_Medallion: 6581
- Helmet_Vital_Core: 6512
- Helmet_Archmage_Cap: 6466
- Body_Porcupine: 6289
- Ring_Hell_Servant: 6279
- Boots_Hunters_Dream: 6253
- Helmet_Aegis: 6225
- Body_Archmage_Mantle: 6208
- Gloves_Hunters_Dream: 6208
- Body_Vital_Core: 6203
- Gloves_Aegis: 6200

Orphans (5): Crafting_Resource_Gold_Ore, Crafting_Resource_Iron_Ore, Crafting_Resource_Ruby_Gem, Crafting_Resource_Silk_Fabric, Crafting_Resource_Wild_Boar_Leather

## Extreme_Archon

Archon/Mythic/lvl 150, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 237977 | 95.19% |
| 1 | 11059 | 4.42% |
| 2 | 962 | 0.38% |
| 3 | 2 | 0% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 41628 | 13.6% |
| Epic | 79934 | 26.12% |
| Rare | 62809 | 20.52% |
| Uncommon | 40936 | 13.38% |
| Common | 8402 | 2.75% |
| Unique | 36306 | 11.86% |
| Mythic | 36033 | 11.77% |

Top drops:
- Crafting_Resource_Copper_Ore: 8402
- Crafting_Resource_Deer_Leather: 8402
- Crafting_Resource_Diamond_Gem: 8402
- Crafting_Resource_Linen_Fabric: 8402
- Upgrade_Resource_Armorsmith_Mark: 7454
- Body_Carapace: 7419
- Amulet_Lazurite: 7414
- Boots_Hunters_Dream: 7271
- Upgrade_Resource_Grandmaster_Rune: 7269
- Body_Hunters_Dream: 7268
- Ring_Of_Limitless_Magic: 7259
- Amulet_Goliath_Seal: 7258
- Helmet_Vital_Core: 7258
- Gloves_Vital_Core: 7254
- Body_Vital_Core: 7242

Orphans (16): Amulet_Garnet, Amulet_Recovery_Source, Body_Mysterious_Bastion, Boots_Mysterious_Bastion, Crafting_Resource_Emerald_Gem, Crafting_Resource_Gold_Ore, Crafting_Resource_Iron_Ore, Crafting_Resource_Ruby_Gem, Crafting_Resource_Silk_Fabric, Crafting_Resource_Wild_Boar_Leather, Gloves_Mysterious_Bastion, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Uncommon

## Extreme_Lvl1

Regular/Uncommon/lvl 1, modifiers: []

| Tier | Rolls | Share |
|---|---|---|
| 0 | 0 | 0% |
| 1 | 0 | 0% |
| 2 | 0 | 0% |
| 3 | 19136 | 100% |

| Rarity | Items | Share |
|---|---|---|
| Epic | 3661 | 19.13% |
| Rare | 6274 | 32.79% |
| Uncommon | 7429 | 38.82% |
| Common | 1772 | 9.26% |

Top drops:
- Crafting_Resource_Iron_Ore: 1912
- Crafting_Resource_Deer_Leather: 1880
- Crafting_Resource_Linen_Fabric: 1866
- Crafting_Resource_Diamond_Gem: 1838
- Crafting_Resource_Silk_Fabric: 1823
- Crafting_Resource_Emerald_Gem: 1822
- Crafting_Resource_Ruby_Gem: 1805
- Crafting_Resource_Gold_Ore: 1783
- Crafting_Resource_Copper_Ore: 1772
- Crafting_Resource_Wild_Boar_Leather: 1771
- Crafting_Resource_Essence_Health: 864

Orphans (77): Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Amulet_Pathfinder_Sign, Amulet_Recovery_Source, Amulet_Ruby, Amulet_Trapper_Talisman, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Feral_Instinct, Body_Hunter_Chestplate, Body_Hunters_Dream, Body_Mysterious_Bastion, Body_Porcupine, Body_Steel_Bastion, Body_Stoneheart, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Feral_Instinct, Boots_Hunter, Boots_Hunters_Dream, Boots_Mysterious_Bastion, Boots_Steel_Greaves, Boots_Stone_Tread, Boots_Vital_Core, Crafting_Resource_Essence_Critical_Chance, Gloves_Aegis, Gloves_Archmage, Gloves_Feral_Instinct, Gloves_Hunter, Gloves_Hunters_Dream, Gloves_Mysterious_Bastion, Gloves_Steel, Gloves_Stoneheart, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Feral_Instinct, Helmet_Hunter, Helmet_Hunters_Dream, Helmet_Mysterious_Bastion, Helmet_Steel, Helmet_Stoneheart, Helmet_Vital_Core, Ring_Dexterity, Ring_Hell_Servant, Ring_Intelligence, Ring_of_Creators, Ring_Of_Fire_Demon, Ring_of_Hunter, Ring_Of_Legionnaire, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Rare, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Blacksmith_Rune, Upgrade_Resource_Fine_Flux, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Jeweler_Rune, Upgrade_Resource_Jewellery_Dust_Rare, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Journeyman_Rune, Upgrade_Resource_Master_Rune, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Quality_Flux, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Rare, Upgrade_Resource_Weapon_Dust_Uncommon, Upgrade_Resource_Weapon_Rune, Upgrade_Resource_Weaponsmith_Mark, Weapon_Bloodthirsty, Weapon_Silent_Fury, Weapon_Simple_Axe, Weapon_Simple_Dagger, Weapon_Simple_Sword
