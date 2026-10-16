using Godot;
using System;
using Gravebound.Audio;

namespace Gravebound.Player;

public partial class PlayerProgression : Node
{
    public static PlayerProgression? Instance { get; private set; }

    // Persistent attributes across scene transitions
    private static int _level = 1;
    private static int _currentXP = 0;
    private static int _unspentPoints = 0;
    private static int _strength = 10;
    private static int _agility = 10;
    private static int _vitality = 10;
    private static int _willpower = 10;
    private static bool _initialized = false;

    public int Level => _level;
    public int CurrentXP => _currentXP;
    public int UnspentPoints => _unspentPoints;
    public int Strength => _strength;
    public int Agility => _agility;
    public int Vitality => _vitality;
    public int Willpower => _willpower;

    public int XPRequiredForNextLevel => (int)(80 * Mathf.Pow(_level, 1.4f));

    [Signal] public delegate void XPChangedEventHandler(int current, int max, int level);
    [Signal] public delegate void LevelUpEventHandler(int newLevel);
    [Signal] public delegate void StatsChangedEventHandler();

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
        if (!_initialized)
        {
            _initialized = true;
            _level = 1;
            _currentXP = 0;
            _unspentPoints = 3; // Starting bonus points to try allocation immediately
            _strength = 10;
            _agility = 10;
            _vitality = 10;
            _willpower = 10;
        }

        EmitSignal(SignalName.XPChanged, _currentXP, XPRequiredForNextLevel, _level);
        EmitSignal(SignalName.StatsChanged);
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        _currentXP += amount;

        while (_currentXP >= XPRequiredForNextLevel)
        {
            _currentXP -= XPRequiredForNextLevel;
            _level++;
            _unspentPoints += 5;

            // Full heal on level up
            var player = PlayerController.Instance;
            if (player != null && !player.IsDead)
            {
                player.CurrentHealth = player.GetTotalMaxHealth();
                player.EmitSignal(PlayerController.SignalName.HealthChanged, player.CurrentHealth, player.GetTotalMaxHealth());
            }

            SoundManager.Instance?.PlayAltarChime();
            EmitSignal(SignalName.LevelUp, _level);
        }

        EmitSignal(SignalName.XPChanged, _currentXP, XPRequiredForNextLevel, _level);
        EmitSignal(SignalName.StatsChanged);
    }

    public bool AllocatePoint(string attribute)
    {
        if (_unspentPoints <= 0) return false;

        switch (attribute.ToLower())
        {
            case "strength":
            case "сила":
                _strength++;
                break;
            case "agility":
            case "спритність":
                _agility++;
                break;
            case "vitality":
            case "живучість":
                _vitality++;
                break;
            case "willpower":
            case "воля":
                _willpower++;
                break;
            default:
                return false;
        }

        _unspentPoints--;

        // Notify player stats recalculated
        var player = PlayerController.Instance;
        if (player != null && !player.IsDead)
        {
            player.EmitSignal(PlayerController.SignalName.HealthChanged, player.CurrentHealth, player.GetTotalMaxHealth());
        }

        EmitSignal(SignalName.StatsChanged);
        return true;
    }

    public int GetBonusDamage()
    {
        // Every point above 10 grants +1.5 physical damage
        return Mathf.RoundToInt((_strength - 10) * 1.5f + (_agility - 10) * 0.5f);
    }

    public int GetBonusHealth()
    {
        // Every point above 10 vitality grants +8 Max HP
        return (_vitality - 10) * 8;
    }
}
