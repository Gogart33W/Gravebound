using Godot;
using System;
using Gravebound.Enemies;
using Gravebound.Environment;
using Gravebound.Player;
using Gravebound.UI;

namespace Gravebound.Core;

public partial class GameManager : Node3D
{
    [Export] public NodePath? NavigationRegionPath { get; set; }
    [Export] public NodePath? HUDPath { get; set; }

    private NavigationRegion3D? _navRegion;
    private GraveboundHUD? _hud;
    private int _skeletonsDefeated = 0;
    private const int TotalSkeletonsGoal = 3;
    private bool _altarInspected = false;

    public override void _Ready()
    {
        _navRegion = NavigationRegionPath != null && !NavigationRegionPath.IsEmpty
            ? GetNodeOrNull<NavigationRegion3D>(NavigationRegionPath)
            : GetNodeOrNull<NavigationRegion3D>("NavigationRegion3D");

        _hud = HUDPath != null && !HUDPath.IsEmpty
            ? GetNodeOrNull<GraveboundHUD>(HUDPath)
            : GetNodeOrNull<GraveboundHUD>("HUD");

        // Hook enemy death signals
        var enemiesNode = GetNodeOrNull<Node>("Enemies");
        if (enemiesNode != null)
        {
            foreach (Node child in enemiesNode.GetChildren())
            {
                if (child is SkeletonEnemy skeleton)
                {
                    skeleton.EnemyDied += OnSkeletonDied;
                }
            }
        }

        // Hook altar inspection
        var altar = GetNodeOrNull<CryptAltar>("AltarArea");
        if (altar != null)
        {
            altar.AltarInspected += OnAltarInspected;
        }

        UpdateQuestState();
    }

    private void UpdateQuestState()
    {
        if (_hud == null) return;

        if (_skeletonsDefeated >= TotalSkeletonsGoal && _altarInspected)
        {
            _hud.UpdateQuest(
                "Розділ I: Очищення Завершено!",
                "• Вартові склепу знищені (3 / 3)\n• Портал у Катакомби відкрито на півночі арени!"
            );

            // Open the physical portal in the level!
            CatacombsPortal.Instance?.OpenPortal();
        }
        else
        {
            string altarStatus = _altarInspected ? "Вівтар Мор-Гула досліджено" : "Дослідіть вівтар у центрі";
            _hud.UpdateQuest(
                "Розділ I: Пробудження в Попелі",
                $"• Знищіть скелетів-вартових: {_skeletonsDefeated} / {TotalSkeletonsGoal}\n• {altarStatus}"
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
        UpdateQuestState();
    }
}
