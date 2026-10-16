using Godot;
using System;
using Gravebound.Enemies;
using Gravebound.Environment;
using Gravebound.UI;

namespace Gravebound.Core;

public partial class GameManager : Node3D
{
    [Export] public NodePath? HUDPath { get; set; }

    [Export] public string ChapterTitle { get; set; } = "Розділ I: Пробудження в Попелі";
    [Export] public string CompletedTitle { get; set; } = "Розділ I: Очищення Завершено!";
    [Export] public string VictoryTitle { get; set; } = "«ТАЄМНИЦЮ СКЛЕПУ РОЗГАДАНО!»";
    [Export(PropertyHint.MultilineText)]
    public string VictoryBody { get; set; } =
        "Вартові склепу знайшли спокій. Древній портал у Катакомби Мор-Гула активовано!\n\nТвоя подорож тільки розпочинається, Могилонароджений...";
    [Export] public string VictoryButton { get; set; } = "ВИРУШИТИ В КАТАКОМБИ";
    /// <summary>Scene loaded when the player confirms the victory window. Empty = just close the window.</summary>
    [Export(PropertyHint.File, "*.tscn")] public string NextScenePath { get; set; } = "";

    private GraveboundHUD? _hud;
    private CryptAltar? _altar;
    private int _skeletonsDefeated = 0;
    private int _skeletonGoal = 0;
    private bool _altarInspected = false;
    private bool _changingScene = false;

    public override void _Ready()
    {
        _hud = HUDPath != null && !HUDPath.IsEmpty
            ? GetNodeOrNull<GraveboundHUD>(HUDPath)
            : GetNodeOrNull<GraveboundHUD>("HUD");

        // Hook enemy death signals; the goal is however many skeletons the level contains
        var enemiesNode = GetNodeOrNull<Node>("Enemies");
        if (enemiesNode != null)
        {
            foreach (Node child in enemiesNode.GetChildren())
            {
                if (child is SkeletonEnemy skeleton)
                {
                    _skeletonGoal++;
                    skeleton.EnemyDied += OnSkeletonDied;
                }
            }
        }

        // The altar stays sealed until every guardian is dead
        _altar = GetNodeOrNull<CryptAltar>("AltarArea");
        if (_altar != null)
        {
            _altar.AltarInspected += OnAltarInspected;
            _altar.SetUnlocked(_skeletonsDefeated >= _skeletonGoal);
        }

        if (_hud != null)
        {
            _hud.ConfigureVictory(VictoryTitle, VictoryBody, VictoryButton);
            _hud.ContinuePressed += OnContinuePressed;
        }

        UpdateQuestState();
    }

    public override void _ExitTree()
    {
        if (_hud != null && IsInstanceValid(_hud))
        {
            _hud.ContinuePressed -= OnContinuePressed;
        }
    }

    private void UpdateQuestState()
    {
        if (_hud == null) return;

        if (_skeletonsDefeated >= _skeletonGoal && _altarInspected)
        {
            _hud.UpdateQuest(
                CompletedTitle,
                $"• Вартові склепу знищені ({_skeletonsDefeated} / {_skeletonGoal})\n• Портал відкрито на півночі арени!"
            );

            // Open the physical portal in the level!
            CatacombsPortal.Instance?.OpenPortal();
        }
        else
        {
            string altarStatus = _altarInspected
                ? "Вівтар Мор-Гула досліджено"
                : (_skeletonsDefeated >= _skeletonGoal ? "Дослідіть вівтар у центрі" : "Вівтар запечатано, доки живі вартові");
            _hud.UpdateQuest(
                ChapterTitle,
                $"• Знищіть скелетів-вартових: {_skeletonsDefeated} / {_skeletonGoal}\n• {altarStatus}"
            );
        }
    }

    private void OnAltarInspected()
    {
        _altarInspected = true;
        UpdateQuestState();
    }

    private void OnSkeletonDied(SkeletonEnemy enemy)
    {
        _skeletonsDefeated++;
        if (_altar != null && _skeletonsDefeated >= _skeletonGoal)
        {
            _altar.SetUnlocked(true);
        }
        UpdateQuestState();
    }

    private void OnContinuePressed()
    {
        if (_changingScene) return;
        _changingScene = true;

        if (!string.IsNullOrEmpty(NextScenePath))
        {
            GetTree().ChangeSceneToFile(NextScenePath);
        }
        else
        {
            GetTree().ReloadCurrentScene();
        }
    }
}
