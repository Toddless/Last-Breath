namespace Playground
{
    	using System.ComponentModel;
    	using System.Linq;
    	using Godot;
    	using Playground.Script.Enemy;
    	using Playground.Script.Scenes;
    	using System;
    	using System.Collections.Generic;

    	public partial class MainScene : BaseSpawnableScene
    	{
    		private PackedScene? _battleScene = ResourceLoader.Load<PackedScene>("res://Scenes/BattleScene.tscn");
    		private BattleScene? _currentBattleScene;
    		private Queue<Action> _actionQueue = new();
    		private Button? _reduceAttribute;
    		private Button? _addAttribute;
    		private Button? _dealDamage;
    		private Player? _player;

    		public override void _Ready()
    		{
    			EnemySpawner = GetNode<EnemySpawner>(nameof(EnemySpawner));
    			_player = GetNode<Player>(nameof(CharacterBody2D));
    			_reduceAttribute = GetNode<Button>("/root/MainScene/UI/Buttons/ReduceEnemiesAttribute");
    			_addAttribute = GetNode<Button>("/root/MainScene/UI/Buttons/AddEnemiesAttribute");
    			_dealDamage = GetNode<Button>("/root/MainScene/UI/Buttons/DealDamage");
    			_dealDamage.Pressed += DealDamage;
    			_addAttribute.Pressed += AddEnemiesAttribute;
    			_reduceAttribute.Pressed += ReduceEnemiesAttributePressed;

    			EnemiesRespawnPosition = new List<Vector2>()
    			{
    				new(259, 566),
    				new(728, 624),
    				new(878, 306),
    				new(515, 172),
    				new(290, 210),
    			};
    			EnemySpawner.InitializeEnemiesPositions(EnemiesRespawnPosition);

    			for (int i = 0; i < EnemiesRespawnPosition.Count; i++)
    			{
    				EnemySpawner.SpawnNewEnemy();
    			}

    			GD.Print($"Scene initialized: {this.Name}");
    		}


    		private void DealDamage()
    		{
    			var enemy = Enemies?[0];
    			enemy.EnemyHealth.MaxHealth *= 0.9f;
    			GD.Print($"Current health: {enemy.EnemyHealth.CurrentHealth}\n" +
    				$"Max health: {enemy.EnemyHealth.MaxHealth}");
    		}

    		private void AddEnemiesAttribute()
    		{
    			Enemies[0].EnemyAttribute.Dexterity.Total += 5;
    			Enemies[0].EnemyAttribute.Strength.Total += 5;
    		}

    		private void ReduceEnemiesAttributePressed()
    		{
    			Enemies[0].EnemyAttribute.Dexterity.Total -= 5;
    			Enemies[0].EnemyAttribute.Strength.Total -= 5;
    		}

    		public void PlayerInteracted(EnemyAI? enemy)
    		{
    			if (_currentBattleScene != null || enemy == null)
    			{
    				return;
    			}

    			Node battleInstance = _battleScene!.Instantiate();
    			Node mainScene = GetParent().GetNode<MainScene>("MainScene");
    			_currentBattleScene = (BattleScene)battleInstance;
    			mainScene.CallDeferred("add_child", battleInstance);
    			_player!.PlayerLastPosition = _player.Position;
    			_currentBattleScene.Init(_player!, enemy);
    			this.CallDeferred("remove_child", _player);
    			_currentBattleScene.CallDeferred("add_child", _player);
    			this.CallDeferred("remove_child", enemy);
    			_currentBattleScene.CallDeferred("add_child", enemy);
    			_currentBattleScene.BattleSceneFinished += OnBattleFinished;
    		}

    		private void OnBattleFinished(EnemyAI enemyToDelete)
    		{
    			CallDeferred("remove_child", _currentBattleScene!);
    			_currentBattleScene = null;
    			enemyToDelete.PropertyChanged -= EnemiePropertyChanged;
    			enemyToDelete.GetNode<Area2D>("Area2D").BodyEntered -= enemyToDelete.PlayerEntered;
    			enemyToDelete.GetNode<Area2D>("Area2D").BodyExited -= enemyToDelete.PlayerExited;
    			EnemySpawner!.DeleteEnemy(enemyToDelete);
    			Enemies!.Remove(enemyToDelete);
    		}

    		public override void EnemiePropertyChanged(object? sender, PropertyChangedEventArgs e)
    		{
    			PlayerInteracted(Enemies!.FirstOrDefault(x => x.PlayerEncounted == true));
    		}
    	}
}
