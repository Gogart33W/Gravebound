using Godot;

namespace Gravebound.Core;

/// <summary>
/// Shared grounded movement for player and enemies: gravity, floor snapping
/// and automatic step-up so characters can walk over stairs and the altar dais.
/// </summary>
public static class CharacterMotion
{
    public const float Gravity = 28.0f;
    public const float MaxFallSpeed = 40.0f;
    public const float StepHeight = 0.5f;
    public const float FloorSnap = 0.6f;

    public static void Configure(CharacterBody3D body)
    {
        body.FloorSnapLength = FloorSnap;
        body.FloorMaxAngle = Mathf.DegToRad(50.0f);
        body.FloorStopOnSlope = true;
        body.UpDirection = Vector3.Up;
    }

    /// <summary>
    /// Uses the horizontal part of body.Velocity, adds gravity, climbs small steps, then moves.
    /// </summary>
    public static void Move(CharacterBody3D body, ref float fallSpeed, float dt)
    {
        Vector3 vel = body.Velocity;
        Vector3 horizontal = new Vector3(vel.X, 0f, vel.Z);

        if (body.IsOnFloor())
        {
            fallSpeed = 0f;
        }
        else
        {
            fallSpeed = Mathf.Max(fallSpeed - Gravity * dt, -MaxFallSpeed);
        }

        TryStepUp(body, horizontal * dt);

        body.Velocity = new Vector3(horizontal.X, fallSpeed, horizontal.Z);
        body.MoveAndSlide();

        if (body.IsOnFloor())
        {
            fallSpeed = 0f;
        }
    }

    private static void TryStepUp(CharacterBody3D body, Vector3 motion)
    {
        if (motion.LengthSquared() < 0.000001f) return;
        if (!body.IsOnFloor()) return;

        Transform3D from = body.GlobalTransform;
        var hit = new KinematicCollision3D();
        if (!body.TestMove(from, motion, hit)) return;

        // Only treat near-vertical surfaces as steps (not slopes or other floors)
        if (Mathf.Abs(hit.GetNormal().Y) > 0.5f) return;

        Vector3 up = Vector3.Up * StepHeight;
        if (body.TestMove(from, up)) return; // ceiling in the way

        Transform3D raised = from;
        raised.Origin += up;

        Vector3 probe = motion.Normalized() * Mathf.Max(motion.Length(), 0.2f);
        if (body.TestMove(raised, probe)) return; // too tall to climb

        body.GlobalPosition += up;
    }
}
