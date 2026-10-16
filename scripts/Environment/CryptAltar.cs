using Godot;
using System;
using Gravebound.Audio;
using Gravebound.Player;
using Gravebound.UI;

namespace Gravebound.Environment;

public partial class CryptAltar : Area3D
{
    public static CryptAltar? Instance { get; private set; }

    [Export] public string AltarTitle { get; set; } = "Хроніка Могилонародженого: Пакт Мор-Гула";
    [Export(PropertyHint.MultilineText)]
    public string AltarText { get; set; } =
        "«Коли небо впало, а перша імперія потонула в чорному попелі, Вартові склали нерозривну клятву крові. Тіла їхні були замуровані в граніт крипти, а душі зв'язані печатками Мор-Гула.\n\nТи — останній Могилонароджений (Gravebound). Пробуджений із праху, коли древня печать тріснула, ти ступаєш цим холодним камінням. Твій меч жадає спокою для тих, чиї кістки були підняті скверною...»";

    private bool _playerInside = false;
    private bool _hasBeenInspected = false;
    private OmniLight3D? _altarLight;
    private Label3D? _promptLabel;

    [Signal] public delegate void AltarInspectedEventHandler();

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

    [Export] public float InteractRange { get; set; } = 4.2f;
    [Export] public bool IsUnlocked { get; private set; } = false;

    public override void _Ready()
    {
        _altarLight = GetNodeOrNull<OmniLight3D>("AltarGlow");
        _promptLabel = GetNodeOrNull<Label3D>("PromptLabel");

        if (!InputMap.HasAction("interact"))
        {
            InputMap.AddAction("interact");
            var keyEvent = new InputEventKey();
            keyEvent.Keycode = Key.E;
            keyEvent.PhysicalKeycode = Key.E;
            InputMap.ActionAddEvent("interact", keyEvent);
        }

        UpdateVisuals();

        if (_promptLabel != null)
        {
            _promptLabel.Visible = false;
        }
    }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_promptLabel != null)
        {
            if (!IsUnlocked)
            {
                _promptLabel.Text = "Вівтар запечатано (знищіть вартових)";
                _promptLabel.Modulate = new Color(0.9f, 0.35f, 0.35f);
            }
            else if (_hasBeenInspected)
            {
                _promptLabel.Text = "Вівтар активовано [E]";
                _promptLabel.Modulate = new Color(0.3f, 1.0f, 0.7f);
            }
            else
            {
                _promptLabel.Text = "[E] Дослідити Вівтар";
                _promptLabel.Modulate = new Color(0.3f, 0.95f, 0.75f);
            }
        }

        if (_altarLight != null)
        {
            _altarLight.LightColor = IsUnlocked ? new Color(0.2f, 0.95f, 0.65f) : new Color(0.8f, 0.25f, 0.25f);
            _altarLight.LightEnergy = IsUnlocked ? (_hasBeenInspected ? 2.2f : 1.8f) : 0.8f;
        }
    }

    /// <summary>Horizontal distance check: the navmesh stops at the dais edge, below the altar itself.</summary>
    public bool IsInRange(Node3D who)
    {
        Vector3 d = who.GlobalPosition - GlobalPosition;
        d.Y = 0f;
        return d.Length() <= InteractRange;
    }

    public override void _Process(double delta)
    {
        var player = PlayerController.Instance;
        _playerInside = player != null && !player.IsDead && IsInRange(player);
        if (_promptLabel != null && _promptLabel.Visible != _playerInside)
        {
            _promptLabel.Visible = _playerInside;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInside) return;
        if (GraveboundHUD.Instance != null && GraveboundHUD.Instance.IsModalOpen) return;

        if (@event.IsActionPressed("interact"))
        {
            Inspect();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Inspect()
    {
        if (!IsUnlocked)
        {
            if (_promptLabel != null)
            {
                _promptLabel.Text = "Вівтар запечатано! Знищіть усіх вартових";
                _promptLabel.Modulate = new Color(1.0f, 0.25f, 0.25f);
            }
            SoundManager.Instance?.PlaySwordHit(GlobalPosition);
            return;
        }

        SoundManager.Instance?.PlayAltarChime();
        GraveboundHUD.Instance?.ShowLore(AltarTitle, AltarText);
        _hasBeenInspected = true;
        EmitSignal(SignalName.AltarInspected);

        UpdateVisuals();

        // Pulse rune lighting
        if (_altarLight != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_altarLight, "light_energy", 4.5f, 0.3f);
            tween.TweenProperty(_altarLight, "light_energy", 2.2f, 1.0f);
        }
    }
}
