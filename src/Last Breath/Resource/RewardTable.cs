namespace Playground.Resource
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.QuestSystem;

    public partial class RewardTable : Table<Reward>
    {
        public static RewardTable Instance { get; private set; } = new();

        public override void _EnterTree()
        {
            Instance ??= this;
            LoadData();
        }

        protected override void LoadData()
        {
            var data = ResourceLoader.Load<RewardData>(ResourcePath.RewardData);
            foreach (var reward in data.Rewards)
            {
                if (Elements.ContainsKey(reward.Id)) continue;

                Elements[reward.Id] = reward;
            }
        }
    }
}
