using Godot;
using System;
using Gravebound.Audio;
using Gravebound.Player;
using Gravebound.UI;

namespace Gravebound.Environment;

public partial class CatacombsPortal : Area3D
{
    public static CatacombsPortal? Instance { get; private set; }

    [Export] public bool IsOpen { get; private set; } = false;

    private Node3D? _portalVfx;
    private OmniLight3D? _portalLight;
    private MeshInstance3D? _sealSlab;
    private Label3D? _portalLabel;
    private bool _playerInside = false;

    [Signal] public delegate void PortalEnteredEventHandler();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Ready()
    {
        _portalVfx = GetNodeOrNull<Node3D>("PortalVfx");
        _portalLight = GetNodeOrNull<OmniLight3D>("PortalLight");
        _sealSlab = GetNodeOrNull<MeshInstance3D>("SealSlab");
        _portalLabel = GetNodeOrNull<Label3D>("PortalLabel");

        if (_portalVfx != null) _portalVfx.Visible = false;
        if (_portalLight != null) _portalLight.Visible = false;
        if (_portalLabel != null) _portalLabel.Visible = false;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public void OpenPortal()
    {
        if (IsOpen) return;
        IsOpen = true;

        SoundManager.Instance?.PlayAltarChime();

        // Lower the seal slab into the floor
        if (_sealSlab != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_sealSlab, "position:y", -1.2f, 1.8f)
                 .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
        }

        // Ignite the portal VFX and lighting
        if (_portalVfx != null)
        {
            _portalVfx.Visible = true;
            _portalVfx.Scale = Vector3.Zero;
            var tweenVfx = CreateTween();
            tweenVfx.TweenProperty(_portalVfx, "scale", Vector3.One, 1.5f)
                    .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        }

        if (_portalLight != null)
        {
            _portalLight.Visible = true;
            _portalLight.LightEnergy = 0.0f;
            var tweenLight = CreateTween();
            tweenLight.TweenProperty(_portalLight, "light_energy", 3.5f, 1.2f);
        }

        if (_portalLabel != null)
        {
            _portalLabel.Visible = true;
        }

        // Camera rumble if camera exists
        var camera = GetViewport().GetCamera3D();
        if (camera != null)
        {
            var tweenCam = camera.CreateTween();
            Vector3 origPos = camera.Position;
            tweenCam.TweenProperty(camera, "position", origPos + new Vector3(0.15f, -0.1f, 0.1f), 0.08f);
            tweenCam.TweenProperty(camera, "position", origPos - new Vector3(0.12f, 0.08f, -0.1f), 0.08f);
            tweenCam.TweenProperty(camera, "position", origPos, 0.15f);
        }
    }

    public override void _Process(double delta)
    {
        if (!IsOpen || _portalVfx == null) return;

        // Swirl portal rings
        _portalVfx.RotateY(1.8f * (float)delta);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController && IsOpen)
        {
            _playerInside = true;
            GraveboundHUD.Instance?.ShowVictoryModal();
            EmitSignal(SignalName.PortalEntered);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInside = false;
        }
    }
}
