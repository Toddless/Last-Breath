namespace Playground.Resource
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.QuestSystem;

    public partial class RewardTable : Table<Reward>
    {
        private static RewardTable? s_instance;

        public static RewardTable Instance => s_instance ??= new();

        protected override void LoadData()
        {
            var data = ResourceLoader.Load<RewardData>(ResourcePath.RewardData);
            foreach (var reward in data.Rewards)
            {
                if (Elements.ContainsKey(reward.Id)) continue;

                Elements[reward.Id] = reward;
            }
        }

        public override void AddNewElement(Reward element) => Elements.TryAdd(element.Id, element);

        public override void _EnterTree()
        {
            s_instance ??= this;
            LoadData();
        }
    }
}
