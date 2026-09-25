namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Interfaces;
    using Core.MessageBus;
    using Core.Services;
    using Godot;
    using static Tooling.Text.Format;

    /// <summary>
    /// The world beyond the conversation, as far as a tool can stand in for it: a clock that does not
    /// run, an empty scene, and a player nobody is holding. Every one of them is asked for by a factory
    /// of the vocabulary, and none of them is asked anything a dry run can answer truthfully — an entry
    /// that would reach past these is refused before it runs (<see cref="SandboxAction"/>).
    /// </summary>
    /// <remarks>What each refusal means is written on it. A stub that answered a plausible number
    /// instead would put an answer nobody authored on screen.</remarks>
    public sealed class SandboxWorld : IWorldClock, IPlayerAccessor, INpcWorldRegistry, INpcPopulationService,
        ISpawnPointRegistry, INpcProvider, INpcModifierProvider, INpcWorldSpawner, IMartialArtMastery
    {
        private const string NoWorldFormat = "a dry run has no world: {0} is only reached by an entry the run refuses to execute";

        private const string MasteryId = "Mastery_Martial_Art";

        private const int MinutesPerHour = 60;

        private const int MinutesPerDay = 1440;

        public int Day { get; private set; }

        public int Hour => MinuteOfDay / MinutesPerHour;

        public int Minute => MinuteOfDay % MinutesPerHour;

        public int MinuteOfDay { get; private set; }

        public float NormalizedTimeOfDay => (float)MinuteOfDay / MinutesPerDay;

        public DayPhase Phase => DayPhase.Day;

        public IPlayer? Player => null;

        public IReadOnlyList<ISkirmishParticipant> All { get; } = [];

        public int GlobalLimit { get; set; }

        public int CurrentCount => 0;

        public IReadOnlyCollection<string> KnownNpcIds { get; } = [];

        public int EarnedLevel => 0;

        public int BonusPoints => 0;

        public int TotalPoints => 0;

        public string Id => MasteryId;

        public string InstanceId => MasteryId;

        public Texture2D? Icon => null;

        public string Description => MasteryId;

        public string DisplayName => MasteryId;

        public int BonusLevel => 0;

        public int CurrentExperience => 0;

        public int CurrentLevel => 1;

        public int MaximumLevel => 1;

        IReadOnlyList<IPersistentSpawnPoint> ISpawnPointRegistry.All { get; } = [];

        public event Action<int>? HourPassed { add { } remove { } }

        public event Action<DayPhase>? PhaseChanged { add { } remove { } }

        public event Action<IPlayer>? PlayerChanged { add { } remove { } }

        public event Action<int>? BonusLevelChange { add { } remove { } }

        public event Action<int>? ExperienceChange { add { } remove { } }

        public event Action<int>? CurrentLevelChange { add { } remove { } }

        /// <summary>The clock of a dry run stands still: every entry that reads it — a decline cooldown,
        /// an offer window, a deadline — is read at the one moment the conversation is being looked at.</summary>
        public void Tick(float realDelta)
        {
        }

        public void RestoreState(int day, int minuteOfDay)
        {
            Day = day;
            MinuteOfDay = minuteOfDay;
        }

        public void Set(IPlayer player)
        {
        }

        public void Register(ISkirmishParticipant npc)
        {
        }

        public void Unregister(ISkirmishParticipant npc)
        {
        }

        public bool TryReserve() => false;

        public void ReserveOutsideLimit()
        {
        }

        public void Reset()
        {
        }

        public void Register(IPersistentSpawnPoint point)
        {
        }

        public void Unregister(IPersistentSpawnPoint point)
        {
        }

        public NpcDefinition CreateDefinition(string npcId) => throw Refused(nameof(CreateDefinition));

        public NpcDefinition CreateDefinition(string npcId, NpcDefinitionOverrides? overrides) => throw Refused(nameof(CreateDefinition));

        public INpcModifier GetModifier(string id) => throw Refused(nameof(GetModifier));

        public List<string> GetAllModifierIds() => [];

        public IReadOnlyList<INpcModifier> GetAllModifiers() => [];

        public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position) => null;

        public void Despawn(IFightableNpc npc)
        {
        }

        public void AddBonusPoints(int points)
        {
        }

        public void AddExperience(int experience)
        {
        }

        public void AddBonusLevel()
        {
        }

        public void RemoveBonusLevel()
        {
        }

        public int ExpToNextLevelRemain() => 0;

        public int ExpToNextLevelTotal() => 0;

        public void RestoreState(int baseLevel, int experience, int bonusPoints)
        {
        }

        public bool IsSame(string otherId) => string.Equals(InstanceId, otherId, StringComparison.Ordinal);

        private static NotSupportedException Refused(string member) => new(Text(NoWorldFormat, member));
    }

    /// <summary>Messages a dry run would send: written down under the name of the message, because
    /// nothing on this side of the tool opens a window or shows a notification.</summary>
    public sealed class SandboxMessages(SandboxLog log) : IGameMessageBus
    {
        private const string SentFormat = "message {0}";

        private const string NoAnswerFormat = "a dry run answers no requests: {0} was asked for";

        public Task PublishMessageAsync<TMessage>(TMessage message) where TMessage : IMessage
        {
            log.Write(Text(SentFormat, typeof(TMessage).Name));
            return Task.CompletedTask;
        }

        public Task<TResponce> SendRequest<TRequest, TResponce>(TRequest request) where TRequest : IRequest<TResponce> =>
            throw new NotSupportedException(Text(NoAnswerFormat, typeof(TRequest).Name));
    }
}
