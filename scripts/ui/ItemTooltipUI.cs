using Godot;
using Gravebound.Items;

namespace Gravebound.UI;

public partial class ItemTooltipUI : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _typeLabel = null!;
    private VBoxContainer _statsContainer = null!;
    private Label _descriptionLabel = null!;
    private Label _actionHintLabel = null!;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("%ItemName");
        _typeLabel = GetNode<Label>("%ItemType");
        _statsContainer = GetNode<VBoxContainer>("%StatsContainer");
        _descriptionLabel = GetNode<Label>("%ItemDescription");
        _actionHintLabel = GetNode<Label>("%ActionHint");
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void ShowItem(ItemData item, Vector2 globalPos, bool isEquippedSlot = false)
    {
        _nameLabel.Text = item.ItemName.ToUpperInvariant();
        Color rarityColor = item.Rarity.GetColor();
        _nameLabel.Modulate = rarityColor;

        _typeLabel.Text = $"{item.Rarity} {item.Type}".ToUpperInvariant();
        _typeLabel.Modulate = rarityColor.Lerp(Colors.White, 0.35f);

        // Clear previous stats
        foreach (var child in _statsContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Add stat modifiers
        if (item.StatModifiers != null && item.StatModifiers.Count > 0)
        {
            foreach (var kvp in item.StatModifiers)
            {
                string statName = kvp.Key.Replace("_", " ").Capitalize();
                string prefix = kvp.Value > 0 ? "+" : "";
                var statLabel = new Label
                {
                    Text = $"{prefix}{kvp.Value} {statName}",
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                statLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.95f, 0.55f));
                statLabel.AddThemeFontSizeOverride("font_size", 13);
                _statsContainer.AddChild(statLabel);
            }
        }

        // Description
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            _descriptionLabel.Text = $"\"{item.Description}\"";
            _descriptionLabel.Visible = true;
        }
        else
        {
            _descriptionLabel.Visible = false;
        }

        // Action hint
        _actionHintLabel.Text = isEquippedSlot ? "[Right-Click] Unequip to Bag" : (item.Type == ItemType.Consumable ? "[Right-Click] Use" : "[Right-Click] Equip");

        // Position tooltip clamped within viewport
        Vector2 vpSize = GetViewportRect().Size;
        Vector2 pos = globalPos + new Vector2(16, 16);
        if (pos.X + 220 > vpSize.X - 10)
        {
            pos.X = globalPos.X - 230;
        }
        if (pos.Y + 160 > vpSize.Y - 10)
        {
            pos.Y = vpSize.Y - 170;
        }
        GlobalPosition = pos;
        Visible = true;
    }

    public void HideTooltip()
    {
        Visible = false;
    }
}
