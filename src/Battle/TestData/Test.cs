namespace Battle.TestData
{
    using System.Linq;
    using Godot;

    [GlobalClass]
    [Tool]
    public partial class Test : Control
    {
        private readonly RandomNumberGenerator _rnd = new();
        private int total, stage2, stage3, stage4;
        [Export] private float _baseChance;
        [Export] private Button? _generateButton;
        [Export] private Label? _resultLabel, _currentChances, _total, _stage2, _stage3, _stage4;
        [Export] private float[] _baseChances = [0.55f, 0.30f, 0.15f];

        public override void _Ready()
        {
            _generateButton?.Pressed += OnButtonPressed;
            _rnd.Randomize();
        }

        private void OnButtonPressed()
        {
            for (int i = 0; i < 100; i++)
            {
                total++;
                _currentChances?.Text = string.Empty;
                float m = _baseChance + 1;
                int stage = 1;

                stage = GetStage();

                int GetStage()
                {
                    if (_rnd.Randf() <= 0.55f * m) stage++;
                    else return stage;

                    if (_rnd.Randf() <= 0.30f * m) stage++;
                    else return stage;

                    if (_rnd.Randf() <= 0.15f * m) stage++;

                    return stage;
                }


                switch (stage)
                {
                    case 2:
                        stage2++;
                        _stage2?.Text = $"Stage 2 count: {stage2}";
                        break;
                    case 3:
                        stage3++;
                        _stage3?.Text = $"Stage 3 count: {stage3}";
                        break;
                    case 4:
                        stage4++;
                        _stage4?.Text = $"Stage 4 count: {stage4}";
                        break;
                }

                _total?.Text = $"Total count: {total}";

                _resultLabel?.Text = $"For base chance: {_baseChance} ability stage will be: {stage}";
            }
        }
    }
}
