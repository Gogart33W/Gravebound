using Godot;
using System;
using Gravebound.Enemies;
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

        InitializeQuest();
    }

    private void InitializeQuest()
    {
        if (_hud != null)
        {
            _hud.UpdateQuest(
                "Розділ I: Пробудження в Попелі",
                $"Знищіть скелетів-вартових склепу: {_skeletonsDefeated} / {TotalSkeletonsGoal}\nДослідіть давній вівтар Мор-Гула (центр арени)"
            );
        }
    }

    private void OnSkeletonDied(SkeletonEnemy enemy)
    {
        _skeletonsDefeated++;
        if (_hud != null)
        {
            if (_skeletonsDefeated >= TotalSkeletonsGoal)
            {
                _hud.UpdateQuest(
                    "Розділ I: Очищення Завершено",
                    "Вартові подолані! Підійдіть до вівтаря та натисніть [L], щоб прочитати Хроніку."
                );
            }
            else
            {
                _hud.UpdateQuest(
                    "Розділ I: Пробудження в Попелі",
                    $"Знищіть скелетів-вартових склепу: {_skeletonsDefeated} / {TotalSkeletonsGoal}"
                );
            }
        }
    }
}
