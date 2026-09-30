# LastBreath Documentation

Each folder groups the documents for one system. Descriptions of existing implementations are kept separate from architectures for new features, concepts, and decisions. System descriptions are written in Russian; existing project documents were not translated as part of this reorganization.

## Existing Systems and Related Projects

Files named `CurrentSystem.md` describe existing implementations. A title containing Architecture, Concept, or Decisions does not by itself confirm that something has been implemented; check the status inside the document. Combat also has a separate presentation document, which remains in the Combat folder.

| System | Existing implementation | Design and extensions |
| --- | --- | --- |
| Abilities | [Active Abilities](Abilities/CurrentSystem.md) | — |
| AI | [AI, NPC Creation, and Lifecycle](AI/CurrentSystem.md) | [NPC Groups, Alerts, and Reinforcements — Architecture Proposal](AI/EntityGroupsArchitecture.md) |
| Augments | [Augments, Sockets, and Ornaments](Augments/CurrentSystem.md) | — |
| Combat | [Combat Damage and Events](Combat/CurrentSystem.md); [Combat Playback, Animations, and VFX](Combat/Presentation.md) | [Combat System Expansion — Architecture Proposal](Combat/CombatExpansionArchitecture.md); [Combat System Expansion](Combat/CombatExpansionConcept.md); [Combat System Expansion — Decision Discussion](Combat/CombatExpansionDecisions.md) |
| Crafting | [Existing Crafting System](Crafting/CurrentSystem.md) | [Crafting Expansion — Modifier Tags](Crafting/CraftingTagsConcept.md); [Crafting Expansion — Decision Discussion](Crafting/CraftingTagsDecisions.md) |
| Effects | [Existing Effects System](Effects/CurrentSystem.md) | [Timed Effects: Expanding the Buff and Debuff System](Effects/TimedEffectsArchitecture.md) |
| GameData | [Loading Game Data](GameData/CurrentSystem.md) | — |
| Interaction | [General Interaction, Chests, and NPCs](Interaction/CurrentSystem.md) | [Shared Interaction Architecture](Interaction/InteractionArchitecture.md); [Interactive Environment](Interaction/InteractiveEnvironmentConcept.md); [Interactive Environment — Decision Discussion](Interaction/InteractiveEnvironmentDecisions.md); [NPCs in the Shared Interaction System — #266](Interaction/NpcInteractions.md) |
| Items | [Equipment and Item Grants](Items/CurrentSystem.md) | [Usable Items — Architecture Proposal](Items/UsableItemsArchitecture.md) |
| Localization | [Localization and Text Formatting](Localization/CurrentSystem.md) | — |
| Locations | [Locations, Battle Spaces, and World Snapshots](Locations/CurrentSystem.md) | [Location and Battle Spaces](Locations/LocationSpaces.md) |
| Loot | [Loot Generation](Loot/CurrentSystem.md) | — |
| Parameters | [Entity Parameters and Modifiers](Parameters/CurrentSystem.md) | — |
| PassiveTree | [Passive Tree and Passive Abilities](PassiveTree/CurrentSystem.md) | — |
| Reputation | [Reputation and Raids](Reputation/CurrentSystem.md) | — |
| Services | [Service Registration and Cross-System Messages](Services/CurrentSystem.md) | — |
| Trade | [Trading and Wallet](Trade/CurrentSystem.md) | — |
| UI | [Windows, HUD, and Popups](UI/CurrentSystem.md) | — |

## Other Project Documents

- [Godot/C# Development: Lifecycle, State, and Integration](Guidelines/GodotDevelopment.md).
- [Change Verification: Tests, Godot, and Acceptance](Guidelines/Verification.md).
- [Quest Automation](Narrative/QuestAutomationArchitecture.md).
- [DualGrid Tiles](DualGrid/DualGridTileset.md).
- [System Architecture Request Template](Guidelines/SystemArchitecturePrompt.md).
- [Tool Exports](Guidelines/ToolExports.md).
- [Game Asset Style Guide](Guidelines/AssetStyleGuide.md).
