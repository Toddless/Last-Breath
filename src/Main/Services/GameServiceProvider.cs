namespace LastBreath.Services
{
    using Battle.Source;
    using Battle.Source.RequestHandlers;
    using Core.Ai.World;
    using Core.Ai.World.Raids;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Interfaces;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
    using Core.Modifiers.Conditions;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.Rules;
    using Core.Reputation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Session;
    using Core.Trade;
    using Core.Views;
    using Core.Views.UI;
    using Crafting.Source;
    using Inventory;
    using LootGeneration.Source;
    using Microsoft.Extensions.DependencyInjection;
    using Npc;
    using SharedUi;
    using Trade;
    using UI;
    using World;

    /// <summary>Project bootstrap: the shared Core provider + Main registrations. The only place touching the static root.</summary>
    public static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            // The streams a cast rolls on are chosen here, next to the container that already binds the
            // engine RNG for everything else: the game rolls its casts and their delivery on Godot's
            // generator, and the domain defaults stay free of the engine so hosts without one survive
            // a cast.
            BattleSystemModuleDependencies.UseEngineCastRandom();
            BattleSystemModuleDependencies.UseEngineCombatRandom();
            var provider = Core.Services.GameServiceProvider.Initialize(RegisterProjectServices);
            provider.GetService<IGameDataService>().LoadAll();
            provider.GetService<ReputationBroadcaster>(); // eager: nobody injects it, it lives on bus subscriptions
            provider.GetService<KillFactTracker>(); // eager: same, bus subscriptions only
            provider.GetService<LocationFactTracker>();
            provider.GetService<NpcFinalDeathFactTracker>();
            provider.GetService<IQuestLogService>(); // eager: lives on facts/inventory/clock subscriptions
            provider.GetService<QuestNotificationBroadcaster>();
            RegisterUiFactories(provider);
            RegisterNarrativeSaveSections(provider);
            return provider;
        }

        private static void RegisterProjectServices(IServiceCollection services)
        {
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddGameDataParticipant<IItemDataProvider, ItemDataProvider>();
            // One reading of "one of a kind" for everyone who hands an item over.
            services.AddSingleton<IUniqueItemQuery, UniqueItemQuery>();
            services.AddGameDataParticipant<IFactionRelationService, FactionRelationService>();
            services.AddGameDataParticipant<IReputationDeedProcessor, ReputationDeedProcessor>();
            services.AddGameDataParticipant<IPersonalReputationService, PersonalReputationService>();
            services.AddGameDataParticipant<IReputationPerkProvider, ReputationPerkProvider>();
            services.AddSingleton<ReputationBroadcaster>();
            services.AddSingleton<INpcWorldRegistry, NpcWorldRegistry>();
            services.AddSingleton<IWitnessQuery, WorldWitnessQuery>();

            // World NPC stack: providers, population, skirmishes, clock, spawner — raids sit on top of it.
            services.AddGameDataParticipant<INpcProvider, NpcProvider>();
            services.AddGameDataParticipant<INpcBuffProvider, NpcBuffProvider>();
            services.AddGameDataParticipant<INpcModifierProvider, NpcModifierProvider>();
            services.AddGameDataParticipant<IWorldClock, GameWorldClock>();
            services.AddGameDataParticipant<IPlayerLifecycleConfigProvider, PlayerLifecycleConfigProvider>();
            services.AddGameDataParticipant<IPlayerStatsProvider, PlayerStatsProvider>();
            services.AddConditionCatalog();
            services.AddGameDataParticipant<IPassiveTreeProvider, PassiveTreeProvider>();
            services.AddGameDataParticipant<IPassiveRespecPricing, PassiveTreeRulesProvider>();
            services.AddSingleton<IPassiveTreeService, PassiveTreeService>();

            // Undoing an allocation spends gold, and the tree and the wallet know nothing about each
            // other: the order lives in the handler, like every other operation that settles two systems.
            // Registered here rather than by the battle module because the wallet is this project's.
            services.AddTransient<IRequestHandler<RespecPassiveNodesRequest, RespecResult>, RespecPassiveNodesRequestHandler>();
            services.AddSingleton<INpcPopulationService, NpcPopulationService>();
            services.AddSingleton<INpcSkirmishService, NpcSkirmishService>();
            services.AddSingleton<INpcWorldSpawner, BattleNpcWorldSpawner>();
            services.AddSingleton<IBattleNpcSpawner, BattleSummonSpawner>();
            services.AddSingleton<IRaidSpawnRegistry, RaidSpawnRegistry>();
            services.AddGameDataParticipant<IRaidService, RaidService>();
            services.AddSingleton<ISaveGameService, SaveGameService>();
            // Project infrastructure (module discipline): the save stack and session reset are
            // composed by the PROJECT, not by the battle module.
            services.AddSaveSystem();
            services.AddSessionReset();
            services.AddGameData("res://Data/", "res://Data/Shared/");
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<ISettingsHandler, SettingsHandler>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            RegisterTradeServices(services);
            services.AddCraftingSystemModuleDependencies();
            services.AddBattleSystemModuleDependencies();
            services.AddLootGenerationServices();
            RegisterNarrativeServices(services);
        }

        /// <summary>Trade is Main-owned (module discipline): wallet, the single gold-valuation
        /// point, traders and the buy/sell gates live only in the world project. The shared
        /// session-reset list picks the wallet/traders up through its optional GetService pattern.</summary>
        private static void RegisterTradeServices(IServiceCollection services)
        {
            services.AddGameDataParticipant<ITradeConfigProvider, TradeConfigProvider>();
            services.AddSingleton<IWalletService, WalletService>();
            services.AddSingleton<IItemValuation>(sp => new ItemValuation(
                sp.GetRequiredService<ITradeConfigProvider>(),
                sp.GetService<IItemDataProvider>(),
                sp.GetService<IAbilityAugmentCatalog>()));
            services.AddGameDataParticipant<ITraderProvider, TraderProvider>();
            services.AddSingleton<ITraderService>(sp => new TraderService(
                sp.GetRequiredService<ITraderProvider>(),
                sp.GetRequiredService<ITradeConfigProvider>(),
                sp.GetService<IItemDataProvider>(),
                sp.GetService<IItemCreationService>(),
                sp.GetService<IWorldClock>()));
            services.AddSingleton(sp => new TradePricing(
                sp.GetRequiredService<IItemValuation>(),
                sp.GetRequiredService<ITradeConfigProvider>(),
                sp.GetService<IReputationPerkProvider>()));
            services.AddTransient<IRequestHandler<BuyItemRequest, int>, BuyItemRequestHandler>();
            services.AddTransient<IRequestHandler<SellItemRequest, int>, SellItemRequestHandler>();
            services.AddTransient<IMessageHandler<OpenTradeWindowMessage>, OpenTradeWindowMessageHandler>();
        }

        /// <summary>Narrative foundation: world facts + the condition/action vocabulary shared by
        /// the dialogue and quest systems, and the Influence mastery both feed on.</summary>
        private static void RegisterNarrativeServices(IServiceCollection services)
        {
            services.AddSingleton<IWorldFactsService, WorldFactsService>();
            services.AddSingleton<KillFactTracker>();
            services.AddSingleton<LocationFactTracker>();
            services.AddSingleton<NpcFinalDeathFactTracker>();
            services.AddGameDataParticipant<IInfluenceMastery, InfluenceMastery>();
            services.AddGameDataParticipant<IQuestProvider, QuestProvider>();
            services.AddSingleton<IQuestLogService, QuestLogService>();
            services.AddSingleton<QuestNotificationBroadcaster>();
            services.AddGameDataParticipant<IDialogueProvider, DialogueProvider>();
            services.AddSingleton<IDialogueService, DialogueService>();
            services.AddTransient<IMessageHandler<OpenDialogueMessage>, OpenDialogueMessageHandler>();

            services.AddSingleton<INarrativeConditionParser, NarrativeConditionParser>();
            services.AddSingleton<INarrativeConditionFactory, HasItemConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, FactConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, FactionStandingConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, NpcRelationConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, AttributeConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, InfluenceConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, AllOfConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, AnyOfConditionFactory>();
            services.AddSingleton<INarrativeConditionFactory, NotConditionFactory>();

            // Quest vocabulary: Func-injected — the data loaders own the parsers, a direct
            // IQuestLogService/IQuestProvider dependency here would close a DI cycle.
            services.AddSingleton<INarrativeConditionFactory>(sp => new QuestStatusConditionFactory(sp.GetRequiredService<IQuestLogService>));
            services.AddSingleton<INarrativeConditionFactory>(sp => new CanAcceptQuestConditionFactory(sp.GetRequiredService<IQuestLogService>));
            services.AddSingleton<INarrativeConditionFactory>(sp => new CanTurnInQuestConditionFactory(sp.GetRequiredService<IQuestLogService>));
            services.AddSingleton<INarrativeConditionFactory>(sp => new QuestOfferRollConditionFactory(
                sp.GetRequiredService<IWorldFactsService>(),
                sp.GetRequiredService<IInfluenceMastery>(),
                sp.GetRequiredService<IWorldClock>(),
                sp.GetRequiredService<Godot.RandomNumberGenerator>(),
                sp.GetRequiredService<IQuestProvider>));

            services.AddSingleton<INarrativeActionParser, NarrativeActionParser>();
            services.AddSingleton<INarrativeActionFactory, SetFactActionFactory>();
            services.AddSingleton<INarrativeActionFactory, GiveItemActionFactory>();
            services.AddSingleton<INarrativeActionFactory, TakeItemActionFactory>();
            services.AddSingleton<INarrativeActionFactory, PublishDeedActionFactory>();
            services.AddSingleton<INarrativeActionFactory, AddReputationActionFactory>();
            services.AddSingleton<INarrativeActionFactory, AddInfluenceExpActionFactory>();
            services.AddSingleton<INarrativeActionFactory, StartTradeActionFactory>();
            services.AddSingleton<INarrativeActionFactory, SpawnNpcActionFactory>();
            services.AddSingleton<INarrativeActionFactory, GrantTreePointsActionFactory>();
            foreach (var kind in System.Enum.GetValues<QuestActionKind>())
                services.AddSingleton<INarrativeActionFactory>(sp => new QuestActionFactory(sp.GetRequiredService<IQuestLogService>, kind));
        }

        /// <summary>The Battle module owns the ISaveManager factory; Main-only sections are
        /// registered on top of it here instead of editing the shared module.</summary>
        private static void RegisterNarrativeSaveSections(IGameServiceProvider provider)
        {
            var saveManager = provider.GetService<ISaveManager>();
            saveManager.Register(new WorldFactsSaveParticipant(provider.GetService<IWorldFactsService>()));
            saveManager.Register(new InfluenceMasterySaveParticipant(provider.GetService<IInfluenceMastery>()));
            saveManager.Register(new QuestLogSaveParticipant(provider.GetService<IQuestLogService>()));
            saveManager.Register(new WalletSaveParticipant(provider.GetService<IWalletService>())); // trade is Main-owned
        }

        private static void RegisterUiFactories(IGameServiceProvider provider)
        {
            var uiElements = provider.GetService<IUiElementsManager>();
            uiElements.RegisterHudFactory(typeof(PlayerHud), () => PlayerHud.Initialize().Instantiate<PlayerHud>());
            // The availability map (design, Todd 2026-07-11): battle allows only the read-only
            // CharacterWindow; a dialogue allows nothing else; a forbidden open is a silent no-op.
            uiElements.RegisterWindowFactory(typeof(InventoryWindow), () => InventoryWindow.Initialize().Instantiate<InventoryWindow>(), UiContext.World);
            uiElements.RegisterWindowFactory(typeof(DialogueWindow), () => DialogueWindow.Initialize().Instantiate<DialogueWindow>(), UiContext.World | UiContext.Dialogue);
            uiElements.RegisterWindowFactory(typeof(QuestJournalWindow), () => QuestJournalWindow.Initialize().Instantiate<QuestJournalWindow>(), UiContext.World);
            uiElements.RegisterWindowFactory(typeof(SaveLoadWindow), () => SaveLoadWindow.Initialize().Instantiate<SaveLoadWindow>(), UiContext.World | UiContext.GameOver);
            // Dialogue allowed by design EXCEPTION: trading starts from a dialogue node, and the
            // window must survive the Dialogue->World context flip of the closing conversation.
            uiElements.RegisterWindowFactory(typeof(TradeWindow), () => TradeWindow.Initialize().Instantiate<TradeWindow>(), UiContext.World | UiContext.Dialogue);
            uiElements.RegisterWindowFactory(typeof(CharacterWindow), () => CharacterWindow.Initialize().Instantiate<CharacterWindow>(), UiContext.World | UiContext.Battle);
            uiElements.RegisterWindowFactory(typeof(OptionsWindow), () => OptionsWindow.Initialize().Instantiate<OptionsWindow>());
            uiElements.RegisterWindowFactory(typeof(GameOverWindow), () => GameOverWindow.Initialize().Instantiate<GameOverWindow>());
            uiElements.RegisterPopupFactory(typeof(IKeywordTooltipPopup), () => KeywordTooltipPopup.Initialize().Instantiate<KeywordTooltipPopup>());
            uiElements.RegisterPopupFactory(typeof(ItemTooltipPopup), () => ItemTooltipPopup.Initialize().Instantiate<ItemTooltipPopup>());
            provider.AddCraftingWindowFactories();
            provider.AddBattleUiElementsFactory();
            provider.AddSharedUiFactories();
        }
    }
}
