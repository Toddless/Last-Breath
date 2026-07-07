namespace Core.Save.Participants
{
    using System;
    using Data.SaveData;
    using Interfaces;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>Strictly last (<see cref="RestoreOrder.Vitals"/>): the setters clamp against
    /// Max values that must have settled from equipment/mastery restoration first.</summary>
    public class PlayerVitalsSaveParticipant(IPlayerAccessor playerAccessor) : ISaveParticipant
    {
        public string SectionId => "playerVitals";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Vitals;

        public JToken Capture()
        {
            var player = playerAccessor.Player;
            if (player == null) return new JObject();
            return JToken.FromObject(new PlayerVitalsSaveData
            {
                Health = player.CurrentHealth,
                Barrier = player.CurrentBarrier,
                Mana = player.CurrentMana
            });
        }

        public void Restore(JToken data, int savedVersion)
        {
            var player = playerAccessor.Player;
            var saved = data.ToObject<PlayerVitalsSaveData>();
            if (player == null || saved == null) return;

            // Checkpoint saves are always alive; a corrupted 0 must not insta-kill on load.
            player.CurrentHealth = Math.Max(1f, saved.Health);
            player.CurrentBarrier = saved.Barrier;
            player.CurrentMana = saved.Mana;
        }
    }
}
