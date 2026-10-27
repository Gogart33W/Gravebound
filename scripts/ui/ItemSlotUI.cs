using Godot;
using Gravebound.Items;

namespace Gravebound.UI;

public partial class ItemSlotUI : PanelContainer
{
    [Signal]
    public delegate void SlotRightClickedEventHandler(int slotIndex);

    [Signal]
    public delegate void EquipSlotRightClickedEventHandler(int slotType);

    [Signal]
    public delegate void SlotHoveredEventHandler(ItemData item, Vector2 globalPos, bool isEquipSlot);

    [Signal]
    public delegate void SlotUnhoveredEventHandler();

    [Export] public bool IsEquipmentSlot { get; set; }
    [Export] public ItemType EquipmentSlotType { get; set; } = ItemType.MainHand;
    [Export] public int SlotIndex { get; set; } = -1;

    private TextureRect _iconRect = null!;
    private Panel _rarityBorder = null!;
    private Label _placeholderLabel = null!;
    private StyleBoxFlat _borderStyle = null!;

    public ItemData? CurrentItem { get; private set; }

    public override void _Ready()
    {
        _iconRect = GetNode<TextureRect>("%ItemIcon");
        _rarityBorder = GetNode<Panel>("%RarityBorder");
        _placeholderLabel = GetNode<Label>("%PlaceholderLabel");

        _borderStyle = new StyleBoxFlat
        {
            DrawCenter = false,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusBottomLeft = 4
        };
        _rarityBorder.AddThemeStyleboxOverride("panel", _borderStyle);

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        GuiInput += OnGuiInput;

        UpdateSlotDisplay();
    }

    public void SetItem(ItemData? item)
    {
        CurrentItem = item;
        UpdateSlotDisplay();
    }

    private void UpdateSlotDisplay()
    {
        if (_iconRect == null) return;

        if (CurrentItem != null)
        {
            _iconRect.Texture = CurrentItem.Icon;
            _iconRect.Visible = CurrentItem.Icon != null;
            _placeholderLabel.Visible = false;

            Color rarityColor = CurrentItem.Rarity.GetColor();
            _borderStyle.BorderColor = rarityColor;
            _rarityBorder.Visible = true;
        }
        else
        {
            _iconRect.Texture = null;
            _iconRect.Visible = false;
            _rarityBorder.Visible = false;

            if (IsEquipmentSlot)
            {
                _placeholderLabel.Text = EquipmentSlotType switch
                {
                    ItemType.Helmet => "HELM",
                    ItemType.MainHand => "WEAPON",
                    ItemType.OffHand => "SHIELD",
                    ItemType.BodyArmor => "ARMOR",
                    ItemType.Boots => "BOOTS",
                    _ => ""
                };
                _placeholderLabel.Visible = true;
            }
            else
            {
                _placeholderLabel.Visible = false;
            }
        }
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Right)
        {
            if (CurrentItem != null)
            {
                if (IsEquipmentSlot)
                {
                    EmitSignal(SignalName.EquipSlotRightClicked, (int)EquipmentSlotType);
                }
                else
                {
                    EmitSignal(SignalName.SlotRightClicked, SlotIndex);
                }
                // Hide tooltip after right-click action
                EmitSignal(SignalName.SlotUnhovered);
            }
        }
    }

    private void OnMouseEntered()
    {
        if (CurrentItem != null)
        {
            EmitSignal(SignalName.SlotHovered, CurrentItem, GlobalPosition, IsEquipmentSlot);
        }
    }

    private void OnMouseExited()
    {
        EmitSignal(SignalName.SlotUnhovered);
    }
}
