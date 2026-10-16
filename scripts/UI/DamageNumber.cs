using Godot;
using System;

namespace Gravebound.UI;

public partial class DamageNumber : Label3D
{
    public static void Spawn(Node context, Vector3 position, int damage, bool isCritical = false)
    {
        var label = new DamageNumber();
        label.Text = damage.ToString();
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        label.NoDepthTest = true;
        label.FontSize = isCritical ? 56 : 42;
        label.Modulate = isCritical ? new Color(1.0f, 0.3f, 0.1f) : new Color(1.0f, 0.85f, 0.25f);
        label.OutlineModulate = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        label.OutlineSize = 6;
        label.GlobalPosition = position + new Vector3(
            (float)GD.RandRange(-0.2, 0.2),
            (float)GD.RandRange(0.2, 0.4),
            (float)GD.RandRange(-0.2, 0.2)
        );

        context.GetTree().Root.AddChild(label);

        var tween = label.CreateTween();
        Vector3 endPos = label.GlobalPosition + new Vector3(0, 1.2f, 0);
        tween.SetParallel(true);
        tween.TweenProperty(label, "global_position", endPos, 0.65f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "modulate:a", 0.0f, 0.65f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(() => label.QueueFree()));
    }
}
