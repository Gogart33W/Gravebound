using Godot;
using System;

namespace Gravebound.Audio;

public partial class SoundManager : Node
{
    public static SoundManager? Instance { get; private set; }

    private AudioStream? _swordSwing;
    private AudioStream? _swordHit;
    private AudioStream? _skeletonHit;
    private AudioStream? _skeletonDeath;
    private AudioStream? _playerHit;
    private AudioStream? _altarChime;

    public override void _EnterTree()
    {
        Instance = this;
        LoadStreams();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LoadStreams()
    {
        _swordSwing = GD.Load<AudioStream>("res://assets/audio/sword_swing.wav");
        _swordHit = GD.Load<AudioStream>("res://assets/audio/sword_hit.wav");
        _skeletonHit = GD.Load<AudioStream>("res://assets/audio/skeleton_hit.wav");
        _skeletonDeath = GD.Load<AudioStream>("res://assets/audio/skeleton_death.wav");
        _playerHit = GD.Load<AudioStream>("res://assets/audio/player_hit.wav");
        _altarChime = GD.Load<AudioStream>("res://assets/audio/altar_chime.wav");
    }

    public void PlaySwordSwing()
    {
        Play2D(_swordSwing, -4.0f, (float)GD.RandRange(0.95, 1.05));
    }

    public void PlaySwordHit(Vector3 position)
    {
        Play3D(_swordHit, position, 2.0f, (float)GD.RandRange(0.92, 1.08));
    }

    public void PlaySkeletonHit(Vector3 position)
    {
        Play3D(_skeletonHit, position, 1.0f, (float)GD.RandRange(0.9, 1.1));
    }

    public void PlaySkeletonDeath(Vector3 position)
    {
        Play3D(_skeletonDeath, position, 3.0f, (float)GD.RandRange(0.95, 1.05));
    }

    public void PlayPlayerHit()
    {
        Play2D(_playerHit, 1.0f, (float)GD.RandRange(0.95, 1.05));
    }

    public void PlayAltarChime()
    {
        Play2D(_altarChime, 3.0f, 1.0f);
    }

    private void Play2D(AudioStream? stream, float volumeDb = 0f, float pitchScale = 1.0f)
    {
        if (stream == null) return;
        var player = new AudioStreamPlayer();
        player.Stream = stream;
        player.VolumeDb = volumeDb;
        player.PitchScale = pitchScale;
        AddChild(player);
        player.Finished += () => player.QueueFree();
        player.Play();
    }

    private void Play3D(AudioStream? stream, Vector3 position, float volumeDb = 0f, float pitchScale = 1.0f)
    {
        if (stream == null) return;
        var player = new AudioStreamPlayer3D();
        player.Stream = stream;
        player.VolumeDb = volumeDb;
        player.PitchScale = pitchScale;
        player.MaxDistance = 40f;
        AddChild(player);
        player.GlobalPosition = position;
        player.Finished += () => player.QueueFree();
        player.Play();
    }
}
