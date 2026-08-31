namespace LastBreath.UI
{
    using System;
    using System.Globalization;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Localization;
    using Core.Services;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The character sheet: every entity parameter grouped into sections (vitals, attributes,
    /// offense, defense, misc) plus the faction reputation tab. Rows rebuild on any parameter
    /// change while the window is open — the sheet is small enough to be rebuilt whole.
    /// </summary>
    public partial class CharacterWindow : Control, IWindow
    {
        private const string UID = "uid://b7ndt5b1q2dif";

        private static readonly (string TitleKey, EntityParameter[] Parameters)[] s_sections =
        [
            ("UI_Char_Attributes", [EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Intelligence]),
            ("UI_Char_Offense",
            [
                EntityParameter.PhysicalDamage, EntityParameter.ColdDamage, EntityParameter.FireDamage, EntityParameter.LightningDamage,
                EntityParameter.SpellDamage, EntityParameter.Accuracy, EntityParameter.CriticalChance, EntityParameter.CriticalDamage,
                EntityParameter.AdditionalHitChance, EntityParameter.MulticastChance, EntityParameter.ArmorPenetration,
            ]),
            ("UI_Char_Defense",
            [
                EntityParameter.Armor, EntityParameter.Evade, EntityParameter.BlockChance, EntityParameter.Barrier,
                EntityParameter.Suppress, EntityParameter.CriticalDamageMitigation, EntityParameter.FireResistance,
                EntityParameter.ColdResistance, EntityParameter.LightningResistance, EntityParameter.PoisonResistance,
            ]),
            ("UI_Char_Misc",
            [
                EntityParameter.MoveSpeed, EntityParameter.HealthRecovery, EntityParameter.ManaRecovery,
                EntityParameter.BarrierRecovery,
            ]),
        ];

        [Export] private VBoxContainer? _stats;
        [Export] private VBoxContainer? _ranks;
        private IFactionRelationService? _relations;
        private IParameterFormatProvider? _formats;
        private IPlayerAccessor? _playerAccessor;


        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void InjectServices(IGameServiceProvider provider)
        {
            _relations = provider.GetService<IFactionRelationService>();
            _formats = provider.GetService<IParameterFormatProvider>();
            _playerAccessor = provider.GetService<IPlayerAccessor>();

            _relations.PlayerReputationChanged += OnReputationChanged;
            _playerAccessor.Player?.Parameters.ParameterChanged += OnParameterChanged;

            RenderStats();
            RenderReputation();
        }

        public void Close() => QueueFree();

        public override void _ExitTree()
        {
            _relations?.PlayerReputationChanged -= OnReputationChanged;
            _playerAccessor?.Player?.Parameters.ParameterChanged -= OnParameterChanged;
        }

        private void OnReputationChanged(ReputationChangedArgs change) => RenderReputation();

        private void OnParameterChanged(EntityParameter parameter, float value) => RenderStats();

        private void RenderStats()
        {
            if (_stats == null || _playerAccessor?.Player is not { } player) return;

            _stats.QueueFreeChildren();

            RenderVitals(player);
            foreach ((string titleKey, EntityParameter[] parameters) in s_sections)
            {
                var rows = BeginSection(titleKey);
                foreach (var parameter in parameters)
                    AddRow(rows, Localization.Localize(parameter.ToString()), FormatValue(parameter, player));
            }
        }

        private void RenderVitals(IPlayer player)
        {
            var rows = BeginSection("UI_Char_Vitals");
            AddRow(rows, Localization.Localize("Health"), $"{Mathf.CeilToInt(player.CurrentHealth)} / {Mathf.CeilToInt(player.Parameters.MaxHealth)}");
            AddRow(rows, Localization.Localize("Mana"), $"{Mathf.CeilToInt(player.CurrentMana)} / {Mathf.CeilToInt(player.Parameters.MaxMana)}");
        }

        private VBoxContainer BeginSection(string titleKey)
        {
            var header = new Label { Text = Localization.Localize(titleKey), ThemeTypeVariation = "HeaderLabel" };
            _stats!.AddChild(header);

            var rows = new VBoxContainer();
            rows.AddThemeConstantOverride("separation", 4);
            _stats.AddChild(rows);
            return rows;
        }

        private static void AddRow(VBoxContainer rows, string name, string value)
        {
            var row = SharedUi.KeyValueRow.Initialize().Instantiate<SharedUi.KeyValueRow>();
            row.Set(name, value);
            row.UsePlainValue(); // the sheet's numbers keep the regular label tone, not the gold accent
            rows.AddChild(row);
        }

        private string FormatValue(EntityParameter parameter, IPlayer player) =>
            ParameterValueText.Format(_formats, parameter, player.Parameters);

        private void RenderReputation()
        {
            if (_ranks == null || _relations == null) return;

            _ranks.QueueFreeChildren();

            foreach (Fractions faction in Enum.GetValues<Fractions>())
            {
                if (!_relations.HasReputation(faction)) continue;

                var row = new Label
                {
                    Text = $"{Localization.Localize($"Fraction_{faction}")}: " +
                           $"{Localization.Localize($"RelationLevel_{_relations.GetPlayerRelation(faction)}")} " +
                           $"({_relations.GetReputation(faction)})",
                };
                _ranks.AddChild(row);
            }
        }
    }
}
