namespace Core.Save.Participants
{
    using Ai.World.Raids;
    using Newtonsoft.Json.Linq;

    /// <summary>Only the cooldown persists: an active raid blocks saving, so there is never
    /// raid state to capture beyond "when may the next one roll".</summary>
    public class RaidsSaveParticipant(IRaidService raids) : ISaveParticipant
    {
        public string SectionId => "raids";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.World;

        public JToken Capture() => new JObject { ["cooldownSeconds"] = raids.CooldownRemaining };

        public void Restore(JToken data, int savedVersion)
        {
            if (data["cooldownSeconds"]?.Value<float>() is { } cooldown)
                raids.CooldownRemaining = cooldown;
        }
    }
}
