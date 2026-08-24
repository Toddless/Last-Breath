# Loot simulation report

Seed: `20260710`, scenarios: 17

## Summary

| Scenario | Kills | Difficulty | Items/kill | Value/kill | p95 value | Budget | Value/difficulty | Gold/kill |
|---|---|---|---|---|---|---|---|---|
| Baseline_Regular | 10000 | 0 | 1.92 | 6.33 | 8 | 8.91 | 6.33 | 10.69 |
| Baseline_Boss | 10000 | 0 | 15.47 | 405.53 | 700 | 344.54 | 405.53 | 6.37 |
| Axis_Npc_Modifier_Scale_Double_Health | 10000 | 1.9 | 24.7 | 1045.58 | 1445 | 999.17 | 550.3 | 49.11 |
| Axis_Npc_Modifier_Tier_Upgrade_To_Maximum | 10000 | 0.9 | 23.07 | 2706.47 | 4371 | 654.63 | 3007.19 | 33.28 |
| Axis_Npc_Modifier_Tier_Upgrade_By_One | 10000 | 0.5 | 20.65 | 1200.17 | 1719 | 516.81 | 2400.34 | 17.11 |
| Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource | 10000 | 0.4 | 23.75 | 564.18 | 911 | 482.36 | 1410.45 | 11.82 |
| Axis_Npc_Modifier_Tier_Multiplier_Huge | 10000 | 0.3 | 18.58 | 515.27 | 841 | 447.9 | 1717.55 | 12.11 |
| Axis_Npc_Modifier_Item_Effect_Vampire | 10000 | 0.3 | 18.75 | 515.88 | 857 | 447.9 | 1719.6 | 12.01 |
| Axis_Npc_Modifier_Rarity_Upgrade_Huge | 10000 | 0.3 | 18.88 | 514.63 | 848 | 447.9 | 1715.43 | 12.23 |
| Axis_Npc_Modifier_Min_Rarity_Epic | 10000 | 0.3 | 18.86 | 517.26 | 847 | 447.9 | 1724.21 | 12.62 |
| Stack_1_mods | 10000 | 0.59 | 25.45 | 1591.9 | 2837 | 1660.57 | 2710.24 | 48.92 |
| Stack_3_mods | 10000 | 1.76 | 26.53 | 3108.12 | 5473 | 2882.78 | 1770.31 | 64.26 |
| Stack_5_mods | 10000 | 2.94 | 27.53 | 4897.92 | 8911 | 4122.07 | 1665.76 | 100.71 |
| Stack_7_mods | 10000 | 4.1 | 28.61 | 6858.47 | 11561 | 5334.63 | 1673.02 | 134.65 |
| Stack_11_mods | 10000 | 6.43 | 30.59 | 10086.31 | 12645 | 7777.83 | 1567.43 | 224.02 |
| Extreme_Archon | 10000 | 6.44 | 30.65 | 10870.13 | 12293 | 14763.01 | 1687.99 | 291.74 |
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
| 0 | 46753 | 20.26% |
| 1 | 2243 | 0.97% |
| 2 | 63653 | 27.59% |
| 3 | 118082 | 51.18% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 5927 | 2.57% |
| Epic | 24343 | 10.55% |
| Rare | 71653 | 31.05% |
| Uncommon | 107431 | 46.56% |
| Common | 4584 | 1.99% |
| Unique | 8359 | 3.62% |
| Mythic | 8434 | 3.66% |

Top drops:
- Gloves_Mysterious_Bastion: 4909
- Amulet_Recovery_Source: 4909
- Boots_Mysterious_Bastion: 4897
- Crafting_Resource_Essence_Barrier: 4894
- Upgrade_Resource_Weapon_Rune: 4882
- Helmet_Mysterious_Bastion: 4870
- Upgrade_Resource_Apprentice_Rune: 4866
- Crafting_Resource_Essence_Damage: 4866
- Upgrade_Resource_Weapon_Dust_Uncommon: 4842
- Upgrade_Resource_Blacksmith_Rune: 4832
- Body_Mysterious_Bastion: 4831
- Upgrade_Resource_Armor_Dust_Uncommon: 4810
- Upgrade_Resource_Simple_Flux: 4788
- Upgrade_Resource_Jeweler_Rune: 4785
- Upgrade_Resource_Jewellery_Dust_Uncommon: 4761

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Tier_Upgrade_By_One

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Upgrade_By_One]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 978 | 0.47% |
| 1 | 31284 | 15.15% |
| 2 | 104047 | 50.37% |
| 3 | 70240 | 34.01% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 1695 | 0.82% |
| Epic | 28248 | 13.68% |
| Rare | 83365 | 40.36% |
| Uncommon | 89967 | 43.56% |
| Common | 3025 | 1.46% |
| Unique | 145 | 0.07% |
| Mythic | 104 | 0.05% |

Top drops:
- Helmet_Feral_Instinct: 7537
- Body_Feral_Instinct: 7430
- Gloves_Feral_Instinct: 7414
- Ring_Dexterity: 5895
- Ring_Intelligence: 5887
- Upgrade_Resource_Weapon_Dust_Rare: 5877
- Amulet_Ruby: 5846
- Upgrade_Resource_Jewellery_Dust_Rare: 5842
- Recipe_Body_Aegis: 5753
- Upgrade_Resource_Armor_Dust_Rare: 5727
- Belt_Of_Life: 5671
- Upgrade_Resource_Fine_Flux: 5602
- Upgrade_Resource_Journeyman_Rune: 5503
- Crafting_Resource_Essence_Barrier: 4051
- Crafting_Resource_Gold_Ore: 3169

No orphan items — every table entry dropped at least once.

## Axis_Npc_Modifier_Guaranteed_Items_Crafting_Resource

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Guaranteed_Items_Crafting_Resource]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 287 | 0.15% |
| 1 | 1315 | 0.67% |
| 2 | 58125 | 29.43% |
| 3 | 137795 | 69.76% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 240 | 0.1% |
| Epic | 33184 | 13.97% |
| Rare | 67493 | 28.42% |
| Uncommon | 120442 | 50.71% |
| Common | 16130 | 6.79% |
| Unique | 33 | 0.01% |

Top drops:
- Crafting_Resource_Deer_Leather: 16180
- Crafting_Resource_Linen_Fabric: 16155
- Crafting_Resource_Copper_Ore: 16130
- Crafting_Resource_Diamond_Gem: 16084
- Crafting_Resource_Iron_Ore: 6231
- Crafting_Resource_Ruby_Gem: 6169
- Crafting_Resource_Gold_Ore: 6163
- Crafting_Resource_Wild_Boar_Leather: 6117
- Crafting_Resource_Silk_Fabric: 6086
- Crafting_Resource_Emerald_Gem: 6080
- Boots_Mysterious_Bastion: 5283
- Body_Mysterious_Bastion: 5282
- Amulet_Recovery_Source: 5280
- Gloves_Mysterious_Bastion: 5230
- Crafting_Resource_Essence_Barrier: 5197

Orphans (11): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Carapace, Boots_Aegis, Cloak_Creators_Cassock, Helmet_Aegis, Ring_of_Creators, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Tier_Multiplier_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Tier_Multiplier_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 323 | 0.17% |
| 1 | 1472 | 0.79% |
| 2 | 53140 | 28.6% |
| 3 | 130845 | 70.43% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 212 | 0.11% |
| Epic | 21833 | 11.75% |
| Rare | 63457 | 34.16% |
| Uncommon | 94289 | 50.75% |
| Common | 5960 | 3.21% |
| Unique | 29 | 0.02% |

Top drops:
- Crafting_Resource_Gold_Ore: 6066
- Crafting_Resource_Wild_Boar_Leather: 6010
- Crafting_Resource_Emerald_Gem: 6005
- Crafting_Resource_Silk_Fabric: 5970
- Crafting_Resource_Copper_Ore: 5960
- Crafting_Resource_Deer_Leather: 5944
- Crafting_Resource_Ruby_Gem: 5898
- Crafting_Resource_Iron_Ore: 5888
- Crafting_Resource_Diamond_Gem: 5831
- Crafting_Resource_Linen_Fabric: 5807
- Boots_Mysterious_Bastion: 4977
- Body_Mysterious_Bastion: 4924
- Gloves_Mysterious_Bastion: 4897
- Amulet_Recovery_Source: 4868
- Crafting_Resource_Essence_Barrier: 4825

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Item_Effect_Vampire

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Item_Effect_Vampire]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 238 | 0.13% |
| 1 | 1274 | 0.68% |
| 2 | 53866 | 28.73% |
| 3 | 132112 | 70.46% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 219 | 0.12% |
| Epic | 22040 | 11.76% |
| Rare | 64188 | 34.24% |
| Uncommon | 95017 | 50.68% |
| Common | 6005 | 3.2% |
| Unique | 21 | 0.01% |

Top drops:
- Crafting_Resource_Gold_Ore: 6108
- Crafting_Resource_Wild_Boar_Leather: 6106
- Crafting_Resource_Iron_Ore: 6086
- Crafting_Resource_Ruby_Gem: 6046
- Crafting_Resource_Copper_Ore: 6005
- Crafting_Resource_Diamond_Gem: 5997
- Crafting_Resource_Silk_Fabric: 5965
- Crafting_Resource_Emerald_Gem: 5958
- Crafting_Resource_Linen_Fabric: 5958
- Crafting_Resource_Deer_Leather: 5950
- Amulet_Recovery_Source: 4999
- Crafting_Resource_Essence_Barrier: 4970
- Gloves_Mysterious_Bastion: 4954
- Body_Mysterious_Bastion: 4946
- Upgrade_Resource_Jeweler_Rune: 4881

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Rarity_Upgrade_Huge

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Rarity_Upgrade_Huge]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 218 | 0.12% |
| 1 | 1223 | 0.65% |
| 2 | 53926 | 28.56% |
| 3 | 133423 | 70.67% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 207 | 0.11% |
| Epic | 26449 | 14.01% |
| Rare | 63062 | 33.4% |
| Uncommon | 92953 | 49.24% |
| Common | 6106 | 3.23% |
| Unique | 13 | 0.01% |

Top drops:
- Crafting_Resource_Deer_Leather: 6141
- Crafting_Resource_Iron_Ore: 6128
- Crafting_Resource_Copper_Ore: 6106
- Crafting_Resource_Emerald_Gem: 6071
- Crafting_Resource_Linen_Fabric: 6065
- Crafting_Resource_Gold_Ore: 6038
- Crafting_Resource_Wild_Boar_Leather: 6036
- Crafting_Resource_Ruby_Gem: 6010
- Crafting_Resource_Diamond_Gem: 5974
- Crafting_Resource_Silk_Fabric: 5959
- Boots_Mysterious_Bastion: 5064
- Body_Mysterious_Bastion: 5037
- Gloves_Mysterious_Bastion: 5035
- Amulet_Recovery_Source: 4937
- Crafting_Resource_Essence_Damage: 4920

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Axis_Npc_Modifier_Min_Rarity_Epic

Boss/Legendary/lvl 50, modifiers: [Npc_Modifier_Min_Rarity_Epic]

| Tier | Rolls | Share |
|---|---|---|
| 0 | 213 | 0.11% |
| 1 | 1290 | 0.68% |
| 2 | 54048 | 28.66% |
| 3 | 133024 | 70.54% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 203 | 0.11% |
| Epic | 63238 | 33.53% |
| Rare | 50590 | 26.83% |
| Uncommon | 68478 | 36.31% |
| Common | 6051 | 3.21% |
| Unique | 15 | 0.01% |

Top drops:
- Crafting_Resource_Iron_Ore: 6197
- Crafting_Resource_Ruby_Gem: 6119
- Crafting_Resource_Wild_Boar_Leather: 6111
- Crafting_Resource_Deer_Leather: 6073
- Crafting_Resource_Copper_Ore: 6051
- Crafting_Resource_Emerald_Gem: 6045
- Crafting_Resource_Linen_Fabric: 6031
- Crafting_Resource_Gold_Ore: 5995
- Crafting_Resource_Diamond_Gem: 5973
- Crafting_Resource_Silk_Fabric: 5922
- Body_Mysterious_Bastion: 5056
- Boots_Mysterious_Bastion: 5032
- Crafting_Resource_Essence_Barrier: 4903
- Gloves_Mysterious_Bastion: 4902
- Upgrade_Resource_Armor_Dust_Uncommon: 4886

Orphans (20): Amulet_Creators_Nature, Amulet_Goliath_Seal, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Hunters_Dream, Boots_Aegis, Boots_Archmage_Sandals, Boots_Vital_Core, Cloak_Creators_Cassock, Gloves_Archmage, Helmet_Aegis, Helmet_Hunters_Dream, Ring_of_Creators, Ring_Of_Fire_Demon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Weaponsmith_Mark

## Stack_1_mods

Boss/Mythic/lvl 100, modifiers: [] + 1 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 12278 | 4.92% |
| 1 | 19798 | 7.94% |
| 2 | 111109 | 44.57% |
| 3 | 106121 | 42.57% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 3204 | 1.26% |
| Epic | 30652 | 12.04% |
| Rare | 92282 | 36.25% |
| Uncommon | 121793 | 47.85% |
| Common | 2685 | 1.05% |
| Unique | 2106 | 0.83% |
| Mythic | 1815 | 0.71% |

Top drops:
- Helmet_Feral_Instinct: 8189
- Gloves_Feral_Instinct: 8054
- Body_Feral_Instinct: 8008
- Amulet_Ruby: 7013
- Ring_Intelligence: 6851
- Ring_Dexterity: 6802
- Upgrade_Resource_Armor_Dust_Rare: 6711
- Upgrade_Resource_Jewellery_Dust_Rare: 6701
- Upgrade_Resource_Weapon_Dust_Rare: 6668
- Amulet_Trinity: 6298
- Upgrade_Resource_Armor_Dust_Uncommon: 6194
- Helmet_Mysterious_Bastion: 6048
- Upgrade_Resource_Weapon_Dust_Uncommon: 6048
- Upgrade_Resource_Apprentice_Rune: 6040
- Upgrade_Resource_Jewellery_Dust_Uncommon: 6015

No orphan items — every table entry dropped at least once.

## Stack_3_mods

Boss/Mythic/lvl 100, modifiers: [] + 3 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 37418 | 14.97% |
| 1 | 42458 | 16.99% |
| 2 | 120622 | 48.26% |
| 3 | 49442 | 19.78% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 9249 | 3.49% |
| Epic | 41783 | 15.75% |
| Rare | 102269 | 38.54% |
| Uncommon | 96088 | 36.21% |
| Common | 2820 | 1.06% |
| Unique | 6800 | 2.56% |
| Mythic | 6324 | 2.38% |

Top drops:
- Body_Feral_Instinct: 9204
- Helmet_Feral_Instinct: 9197
- Gloves_Feral_Instinct: 8687
- Upgrade_Resource_Armor_Dust_Rare: 7248
- Upgrade_Resource_Jewellery_Dust_Rare: 7230
- Ring_Dexterity: 7216
- Ring_Intelligence: 7195
- Upgrade_Resource_Weapon_Dust_Rare: 7184
- Amulet_Ruby: 7088
- Amulet_Trinity: 7069
- Upgrade_Resource_Journeyman_Rune: 6886
- Upgrade_Resource_Fine_Flux: 6885
- Helmet_Mysterious_Bastion: 4203
- Upgrade_Resource_Blacksmith_Rune: 3987
- Upgrade_Resource_Jeweler_Rune: 3932

No orphan items — every table entry dropped at least once.

## Stack_5_mods

Boss/Mythic/lvl 100, modifiers: [] + 5 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 72302 | 28.92% |
| 1 | 65372 | 26.15% |
| 2 | 95470 | 38.19% |
| 3 | 16856 | 6.74% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 16776 | 6.09% |
| Epic | 55417 | 20.13% |
| Rare | 104365 | 37.91% |
| Uncommon | 69351 | 25.19% |
| Common | 3852 | 1.4% |
| Unique | 13120 | 4.77% |
| Mythic | 12435 | 4.52% |

Top drops:
- Body_Feral_Instinct: 7407
- Helmet_Feral_Instinct: 7294
- Gloves_Feral_Instinct: 6758
- Upgrade_Resource_Journeyman_Rune: 6656
- Upgrade_Resource_Fine_Flux: 6634
- Amulet_Trinity: 6346
- Upgrade_Resource_Jewellery_Dust_Rare: 5735
- Upgrade_Resource_Armor_Dust_Rare: 5706
- Upgrade_Resource_Weapon_Dust_Rare: 5552
- Ring_Intelligence: 5071
- Ring_Dexterity: 5071
- Boots_Stone_Tread: 3948
- Amulet_Ruby: 3922
- Gloves_Stoneheart: 3863
- Crafting_Resource_Copper_Ore: 3852

No orphan items — every table entry dropped at least once.

## Stack_7_mods

Boss/Mythic/lvl 100, modifiers: [] + 7 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 116877 | 46.75% |
| 1 | 72239 | 28.9% |
| 2 | 55635 | 22.25% |
| 3 | 5249 | 2.1% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 24910 | 8.71% |
| Epic | 66274 | 23.16% |
| Rare | 94632 | 33.07% |
| Uncommon | 53375 | 18.66% |
| Common | 5403 | 1.89% |
| Unique | 21100 | 7.37% |
| Mythic | 20420 | 7.14% |

Top drops:
- Crafting_Resource_Copper_Ore: 5403
- Crafting_Resource_Linen_Fabric: 5401
- Crafting_Resource_Deer_Leather: 5400
- Crafting_Resource_Diamond_Gem: 5396
- Body_Feral_Instinct: 4928
- Helmet_Feral_Instinct: 4917
- Upgrade_Resource_Journeyman_Rune: 4900
- Upgrade_Resource_Fine_Flux: 4775
- Gloves_Stoneheart: 4724
- Boots_Stone_Tread: 4537
- Gloves_Feral_Instinct: 4236
- Body_Stoneheart: 4183
- Amulet_Trinity: 4148
- Helmet_Archmage_Cap: 3869
- Helmet_Vital_Core: 3868

No orphan items — every table entry dropped at least once.

## Stack_11_mods

Boss/Mythic/lvl 100, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 202089 | 80.84% |
| 1 | 36666 | 14.67% |
| 2 | 10932 | 4.37% |
| 3 | 313 | 0.13% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 37648 | 12.31% |
| Epic | 76308 | 24.94% |
| Rare | 69473 | 22.71% |
| Uncommon | 41482 | 13.56% |
| Common | 8387 | 2.74% |
| Unique | 36495 | 11.93% |
| Mythic | 36153 | 11.82% |

Top drops:
- Crafting_Resource_Copper_Ore: 8387
- Crafting_Resource_Deer_Leather: 8387
- Crafting_Resource_Diamond_Gem: 8387
- Crafting_Resource_Linen_Fabric: 8387
- Helmet_Archmage_Cap: 6491
- Amulet_Onyx_Medallion: 6481
- Helmet_Vital_Core: 6343
- Upgrade_Resource_Armorsmith_Mark: 6226
- Boots_Hunters_Dream: 6202
- Boots_Archmage_Sandals: 6195
- Body_Porcupine: 6169
- Amulet_Goliath_Seal: 6157
- Body_Hunters_Dream: 6156
- Gloves_Vital_Core: 6155
- Body_Archmage_Mantle: 6154

Orphans (3): Crafting_Resource_Gold_Ore, Crafting_Resource_Iron_Ore, Crafting_Resource_Silk_Fabric

## Extreme_Archon

Archon/Mythic/lvl 150, modifiers: [] + 11 random

| Tier | Rolls | Share |
|---|---|---|
| 0 | 238545 | 95.42% |
| 1 | 10412 | 4.16% |
| 2 | 1041 | 0.42% |
| 3 | 2 | 0% |

| Rarity | Items | Share |
|---|---|---|
| Legendary | 41027 | 13.39% |
| Epic | 80062 | 26.12% |
| Rare | 63383 | 20.68% |
| Uncommon | 41324 | 13.48% |
| Common | 8442 | 2.75% |
| Unique | 36207 | 11.81% |
| Mythic | 36042 | 11.76% |

Top drops:
- Crafting_Resource_Copper_Ore: 8442
- Crafting_Resource_Deer_Leather: 8442
- Crafting_Resource_Diamond_Gem: 8442
- Crafting_Resource_Linen_Fabric: 8442
- Boots_Hunters_Dream: 7331
- Boots_Archmage_Sandals: 7310
- Amulet_Lazurite: 7299
- Upgrade_Resource_Perfect_Flux: 7295
- Body_Carapace: 7285
- Helmet_Vital_Core: 7282
- Upgrade_Resource_Armorsmith_Mark: 7276
- Body_Vital_Core: 7272
- Helmet_Hunters_Dream: 7272
- Ring_Of_Fire_Demon: 7271
- Upgrade_Resource_Grandmaster_Rune: 7269

Orphans (16): Amulet_Garnet, Amulet_Recovery_Source, Body_Mysterious_Bastion, Boots_Mysterious_Bastion, Crafting_Resource_Emerald_Gem, Crafting_Resource_Gold_Ore, Crafting_Resource_Iron_Ore, Crafting_Resource_Ruby_Gem, Crafting_Resource_Silk_Fabric, Crafting_Resource_Wild_Boar_Leather, Gloves_Mysterious_Bastion, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Uncommon

## Extreme_Lvl1

Regular/Uncommon/lvl 1, modifiers: []

| Tier | Rolls | Share |
|---|---|---|
| 0 | 0 | 0% |
| 1 | 0 | 0% |
| 2 | 0 | 0% |
| 3 | 19141 | 100% |

| Rarity | Items | Share |
|---|---|---|
| Epic | 3669 | 19.17% |
| Rare | 6261 | 32.71% |
| Uncommon | 7437 | 38.85% |
| Common | 1774 | 9.27% |

Top drops:
- Crafting_Resource_Iron_Ore: 1917
- Crafting_Resource_Deer_Leather: 1874
- Crafting_Resource_Linen_Fabric: 1869
- Crafting_Resource_Diamond_Gem: 1851
- Crafting_Resource_Silk_Fabric: 1818
- Crafting_Resource_Emerald_Gem: 1809
- Crafting_Resource_Ruby_Gem: 1806
- Crafting_Resource_Gold_Ore: 1787
- Crafting_Resource_Wild_Boar_Leather: 1777
- Crafting_Resource_Copper_Ore: 1774
- Crafting_Resource_Essence_Health: 859

Orphans (77): Amulet_Goliath_Seal, Amulet_Onyx_Medallion, Amulet_Pathfinder_Sign, Amulet_Recovery_Source, Amulet_Ruby, Amulet_Trapper_Talisman, Body_Aegis, Body_Archmage_Mantle, Body_Carapace, Body_Feral_Instinct, Body_Hunter_Chestplate, Body_Hunters_Dream, Body_Mysterious_Bastion, Body_Porcupine, Body_Steel_Bastion, Body_Stoneheart, Body_Vital_Core, Boots_Aegis, Boots_Archmage_Sandals, Boots_Feral_Instinct, Boots_Hunter, Boots_Hunters_Dream, Boots_Mysterious_Bastion, Boots_Steel_Greaves, Boots_Stone_Tread, Boots_Vital_Core, Crafting_Resource_Essence_Critical_Chance, Gloves_Aegis, Gloves_Archmage, Gloves_Feral_Instinct, Gloves_Hunter, Gloves_Hunters_Dream, Gloves_Mysterious_Bastion, Gloves_Steel, Gloves_Stoneheart, Gloves_Vital_Core, Helmet_Aegis, Helmet_Archmage_Cap, Helmet_Feral_Instinct, Helmet_Hunter, Helmet_Hunters_Dream, Helmet_Mysterious_Bastion, Helmet_Steel, Helmet_Stoneheart, Helmet_Vital_Core, Ring_Dexterity, Ring_Hell_Servant, Ring_Intelligence, Ring_of_Creators, Ring_Of_Fire_Demon, Ring_of_Hunter, Ring_Of_Legionnaire, Upgrade_Resource_Apprentice_Rune, Upgrade_Resource_Armor_Dust_Rare, Upgrade_Resource_Armor_Dust_Uncommon, Upgrade_Resource_Armorsmith_Mark, Upgrade_Resource_Blacksmith_Rune, Upgrade_Resource_Fine_Flux, Upgrade_Resource_Grandmaster_Rune, Upgrade_Resource_Jeweler_Mark, Upgrade_Resource_Jeweler_Rune, Upgrade_Resource_Jewellery_Dust_Rare, Upgrade_Resource_Jewellery_Dust_Uncommon, Upgrade_Resource_Journeyman_Rune, Upgrade_Resource_Master_Rune, Upgrade_Resource_Perfect_Flux, Upgrade_Resource_Quality_Flux, Upgrade_Resource_Simple_Flux, Upgrade_Resource_Weapon_Dust_Rare, Upgrade_Resource_Weapon_Dust_Uncommon, Upgrade_Resource_Weapon_Rune, Upgrade_Resource_Weaponsmith_Mark, Weapon_Bloodthirsty, Weapon_Silent_Fury, Weapon_Simple_Axe, Weapon_Simple_Dagger, Weapon_Simple_Sword
