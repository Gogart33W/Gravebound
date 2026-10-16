using Godot;
using System;
using Gravebound.Audio;
using Gravebound.Player;

namespace Gravebound.Inventory;

public partial class WorldItem3D : Area3D
{
    [Export] public string ItemName { get; set; } = "Старовинний Клинок";
    public ItemData? Item { get; set; }

    private OmniLight3D? _light;
    private Label3D? _label;
    private MeshInstance3D? _pillarMesh;
    private float _bobTimer;
    private Vector3 _basePos;

    public override void _Ready()
    {
        CollisionLayer = 8; // Layer 8 = Interactive
        CollisionMask = 2;  // Mask 2 = Player

        _basePos = GlobalPosition;

        CreateVisuals();

        BodyEntered += OnBodyEntered;
    }

    public void Setup(ItemData itemData)
    {
        Item = itemData;
        ItemName = itemData.ItemName;
        UpdateVisualColors();
    }

    private void CreateVisuals()
    {
        // 1. Collision shape (cylinder around item)
        var col = new CollisionShape3D();
        var cyl = new CylinderShape3D();
        cyl.Radius = 1.2f;
        cyl.Height = 1.8f;
        col.Shape = cyl;
        col.Position = new Vector3(0, 0.9f, 0);
        AddChild(col);

        // 2. Light beam pillar
        _pillarMesh = new MeshInstance3D();
        var cylinderMesh = new CylinderMesh();
        cylinderMesh.TopRadius = 0.12f;
        cylinderMesh.BottomRadius = 0.28f;
        cylinderMesh.Height = 2.4f;
        _pillarMesh.Mesh = cylinderMesh;
        _pillarMesh.Position = new Vector3(0, 1.2f, 0);

        var mat = new StandardMaterial3D();
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.AlbedoColor = new Color(1, 1, 1, 0.45f);
        _pillarMesh.MaterialOverride = mat;
        AddChild(_pillarMesh);

        // 3. OmniLight
        _light = new OmniLight3D();
        _light.Position = new Vector3(0, 0.8f, 0);
        _light.OmniRange = 4.0f;
        _light.LightEnergy = 1.8f;
        AddChild(_light);

        // 4. Billboard floating Label3D
        _label = new Label3D();
        _label.Position = new Vector3(0, 2.2f, 0);
        _label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _label.NoDepthTest = true;
        _label.FontSize = 34;
        _label.OutlineSize = 6;
        _label.OutlineModulate = new Color(0.05f, 0.05f, 0.05f, 1f);
        _label.Text = Item != null ? Item.ItemName : ItemName;
        AddChild(_label);

        UpdateVisualColors();
    }

    private void UpdateVisualColors()
    {
        if (Item == null) return;

        Color col = Item.GetRarityColor();

        if (_light != null) _light.LightColor = col;
        if (_label != null)
        {
            _label.Text = $"[{Item.ItemName}]\n{Item.GetFormattedStats()}";
            _label.Modulate = col;
        }
        if (_pillarMesh != null && _pillarMesh.MaterialOverride is StandardMaterial3D mat)
        {
            mat.AlbedoColor = new Color(col.R, col.G, col.B, 0.45f);
        }
    }

    public override void _Process(double delta)
    {
        _bobTimer += (float)delta * 3.0f;
        if (_pillarMesh != null)
        {
            _pillarMesh.RotateY(1.5f * (float)delta);
        }
        if (_label != null)
        {
            _label.Position = new Vector3(0, 2.2f + Mathf.Sin(_bobTimer) * 0.08f, 0);
        }
    }

    public void Collect()
    {
        if (Item == null) Item = ItemData.CreateRandomDrop(false);

        if (InventoryManager.Instance != null)
        {
            bool added = InventoryManager.Instance.AddItem(Item);
            if (added)
            {
                SoundManager.Instance?.PlayAltarChime();
                QueueFree();
            }
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController)
        {
            Collect();
        }
    }
}
