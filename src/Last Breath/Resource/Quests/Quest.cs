namespace Playground.Resource.Quests
{
    using System.Collections.Generic;

    public class Quest
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? DescriptionId { get; set; }
        public string? RewardId { get; set; }
        public List<bool>? Conditions { get; set; }
    }
}
