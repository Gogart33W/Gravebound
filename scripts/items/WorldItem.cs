using Godot;

namespace Gravebound.Items;

public partial class WorldItem : Area3D
{
    [Signal]
    public delegate void ItemPickedUpEventHandler(ItemData item);

    public static bool LabelsVisible { get; private set; } = true;

    [Export] public ItemData? Item { get; set; }

    private Node3D _visualContainer = null!;
    private Node3D _defaultMesh = null!;
    private Label3D _label = null!;
    private OmniLight3D? _glowLight;

    public override void _EnterTree()
    {
        AddToGroup("WorldItems");
    }

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
        _defaultMesh ??= GetNodeOrNull<Node3D>("VisualContainer/DefaultMesh")!;
        _label ??= GetNodeOrNull<Label3D>("Label3D")!;
        _glowLight ??= GetNodeOrNull<OmniLight3D>("GlowLight");
    }

    public static void ToggleLabels(SceneTree? tree = null)
    {
        tree ??= Engine.GetMainLoop() as SceneTree;
        if (tree == null)
        {
            return;
        }

        LabelsVisible = !LabelsVisible;
        foreach (var node in tree.GetNodesInGroup("WorldItems"))
        {
            if (node is WorldItem worldItem)
            {
                worldItem.SetLabelVisible(LabelsVisible);
            }
        }
        GD.Print($"[WorldItem] Loot labels: {(LabelsVisible ? "ON" : "OFF")}");
    }

    public void SetLabelVisible(bool visible)
    {
        if (_label != null)
        {
            _label.Visible = visible;
        }
    }

    public void Initialize(ItemData data)
    {
        Item = data;
        EnsureNodes();

        Color rarityColor = data.Rarity.GetColor();

        // Update Billboard Label (Titan Quest style)
        if (_label != null)
        {
            _label.Text = $"[ {data.ItemName} ]";
            _label.Modulate = rarityColor;
            SetLabelVisible(LabelsVisible);

            if (LabelsVisible)
            {
                _label.Scale = new Vector3(0.5f, 0.5f, 0.5f);
                var labelTween = CreateTween();
                labelTween.SetParallel(true);
                labelTween.TweenProperty(_label, "scale", Vector3.One, 0.25f)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
        }

        // Update Visual Mesh (lying on the ground)
        if (_visualContainer != null)
        {
            float randomYaw = (float)GD.RandRange(0.0f, Mathf.Tau);
            _visualContainer.Rotation = new Vector3(0.0f, randomYaw, 0.0f);
            _visualContainer.Position = Vector3.Zero;

            if (data.WorldVisual != null)
            {
                if (_defaultMesh != null)
                {
                    _defaultMesh.Visible = false;
                }
                var customVisual = data.WorldVisual.Instantiate<Node3D>();
                _visualContainer.AddChild(customVisual);
            }
            else if (_defaultMesh != null)
            {
                _defaultMesh.Visible = true;
            }
        }

        // Soft ground illumination
        if (_glowLight != null)
        {
            _glowLight.LightColor = rarityColor;
            _glowLight.LightEnergy = data.Rarity switch
            {
                ItemRarity.Unique => 1.2f,
                ItemRarity.Rare => 0.8f,
                ItemRarity.Magic => 0.5f,
                _ => 0.25f
            };
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

        // Collection pop tween
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "scale", new Vector3(0.01f, 0.01f, 0.01f), 0.15f);
        if (_label != null)
        {
            tween.TweenProperty(_label, "modulate:a", 0.0f, 0.15f);
        }
        tween.Chain().TweenCallback(Callable.From(QueueFree));

        return collectedItem;
    }
}
