using Godot;

namespace Gravebound.Items;

public partial class WorldItem : Area3D
{
    [Signal]
    public delegate void ItemPickedUpEventHandler(ItemData item);

    [Export] public ItemData? Item { get; set; }

    private Node3D _visualContainer = null!;
    private MeshInstance3D _defaultMesh = null!;
    private Label3D _label = null!;
    private OmniLight3D? _glowLight;
    private double _idleTime;
    private float _baseVisualY;

    public override void _Ready()
    {
        AddToGroup("WorldItems");
        EnsureNodes();

        if (Item != null)
        {
            Initialize(Item);
        }
    }

    private void EnsureNodes()
    {
        _visualContainer ??= GetNodeOrNull<Node3D>("VisualContainer")!;
        _defaultMesh ??= GetNodeOrNull<MeshInstance3D>("VisualContainer/DefaultMesh")!;
        _label ??= GetNodeOrNull<Label3D>("Label3D")!;
        _glowLight ??= GetNodeOrNull<OmniLight3D>("GlowLight");

        if (_visualContainer != null && _baseVisualY == 0.0f)
        {
            _baseVisualY = _visualContainer.Position.Y;
        }
    }

    public void Initialize(ItemData data)
    {
        Item = data;
        EnsureNodes();

        Color rarityColor = data.Rarity.GetColor();

        // Update Billboard Label
        if (_label != null)
        {
            _label.Text = data.ItemName;
            _label.Modulate = rarityColor;
        }

        // Update Visual Mesh
        if (_visualContainer != null)
        {
            if (data.WorldVisual != null)
            {
                _defaultMesh.Visible = false;
                var customVisual = data.WorldVisual.Instantiate<Node3D>();
                _visualContainer.AddChild(customVisual);
            }
            else if (_defaultMesh != null)
            {
                _defaultMesh.Visible = true;
                var mat = new StandardMaterial3D
                {
                    AlbedoColor = rarityColor,
                    EmissionEnabled = true,
                    Emission = rarityColor * 0.4f,
                    Roughness = 0.3f,
                    Metallic = 0.5f
                };
                _defaultMesh.MaterialOverride = mat;
            }
        }

        // Rarity glow light
        if (_glowLight != null)
        {
            _glowLight.LightColor = rarityColor;
            _glowLight.LightEnergy = data.Rarity switch
            {
                ItemRarity.Unique => 1.8f,
                ItemRarity.Rare => 1.2f,
                ItemRarity.Magic => 0.8f,
                _ => 0.4f
            };
        }
    }

    public override void _Process(double delta)
    {
        _idleTime += delta;

        if (_visualContainer != null)
        {
            // Gentle floating rotation and bobbing
            _visualContainer.RotateY((float)(1.5 * delta));
            float bobOffset = Mathf.Sin((float)(_idleTime * 2.5)) * 0.05f;
            _visualContainer.Position = new Vector3(_visualContainer.Position.X, _baseVisualY + bobOffset, _visualContainer.Position.Z);
        }
    }

    public ItemData? Collect()
    {
        if (Item == null)
        {
            return null;
        }

        var collectedItem = Item;
        Item = null;
        EmitSignal(SignalName.ItemPickedUp, collectedItem);
        GD.Print($"[WorldItem] Picked up: [{collectedItem.Rarity}] {collectedItem.ItemName}");

        // Small collection pop tween
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "scale", new Vector3(0.01f, 0.01f, 0.01f), 0.15f);
        tween.TweenProperty(_label, "modulate:a", 0.0f, 0.15f);
        tween.Chain().TweenCallback(Callable.From(QueueFree));

        return collectedItem;
    }
}
