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

    [Signal] public delegate void AltarInspectedEventHandler();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        _altarLight = GetNodeOrNull<OmniLight3D>("AltarGlow");
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInside = true;
            GraveboundHUD.Instance?.ShowInteractPrompt("[E] або [L] або Клік: Дослідити Вівтар Мор-Гула");
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInside = false;
            GraveboundHUD.Instance?.HideInteractPrompt();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInside) return;

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.E || key.Keycode == Key.L)
            {
                Inspect();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void Inspect()
    {
        SoundManager.Instance?.PlayAltarChime();
        GraveboundHUD.Instance?.ShowLore(AltarTitle, AltarText);
        _hasBeenInspected = true;
        EmitSignal(SignalName.AltarInspected);

        // Flash rune light
        if (_altarLight != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_altarLight, "light_energy", 4.5f, 0.3f);
            tween.TweenProperty(_altarLight, "light_energy", 2.0f, 1.2f);
        }
    }
}
