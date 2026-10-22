using Godot;
using System;
using System.Collections.Generic;
using Gravebound.Audio;
using Gravebound.Inventory;
using Gravebound.Player;
using Gravebound.UI;

namespace Gravebound.NPC;

public partial class MerchantNPC : Area3D
{
    [Export] public string NPCName { get; set; } = "Коваль Бранн";
    [Export] public string NPCRole { get; set; } = "Зброяр та Коваль Вартових";
    [Export(PropertyHint.MultilineText)]
    public string Greeting { get; set; } = "«Вітаю тебе, Могилонароджений. Мій молот ще пам'ятає, як кувати сталь проти скверни Мор-Гула. Поглянь на мої клинки та броню...»";
    [Export] public string QuestId { get; set; } = "";
    [Export] public bool HasShop { get; set; } = true;

    public List<ItemData> Wares { get; private set; } = new();

    private Label3D? _nameLabel;
    private OmniLight3D? _glow;
    private AnimationPlayer? _animPlayer;
    private bool _playerInRange;

    public override void _Ready()
    {
        CollisionLayer = 8; // Layer 8 = Interactive
        CollisionMask = 2;  // Mask 2 = Player

        _nameLabel = GetNodeOrNull<Label3D>("NameLabel");
        _glow = GetNodeOrNull<OmniLight3D>("Glow");
        _animPlayer = FindAnimationPlayer(this);

        UpdatePromptLabel();

        if (Quests.QuestManager.Instance != null)
        {
            Quests.QuestManager.Instance.QuestUpdated += (id) => UpdatePromptLabel();
        }

        if (_animPlayer != null && _animPlayer.HasAnimation("Idle"))
        {
            _animPlayer.GetAnimation("Idle").LoopMode = Animation.LoopModeEnum.Linear;
            _animPlayer.Play("Idle");
        }

        InitWares();

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private AnimationPlayer? FindAnimationPlayer(Node root)
    {
        if (root is AnimationPlayer ap) return ap;
        foreach (Node child in root.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found != null) return found;
        }
        return null;
    }

    private void InitWares()
    {
        Wares.Clear();

        if (NPCName.Contains("Селена") || NPCRole.Contains("Алхімік"))
        {
            // Alchemist wares
            Wares.Add(new ItemData
            {
                Id = "potion_hp_shop",
                ItemName = "Цілюще Зілля Попелу",
                Description = "Миттєво зцілює 35 одиниць здоров'я.",
                Rarity = ItemRarity.Common,
                SlotType = ItemSlotType.Potion,
                BonusHealth = 35,
                ValueGold = 25
            });
            Wares.Add(new ItemData
            {
                Id = "potion_large_shop",
                ItemName = "Великий Еліксир Життя",
                Description = "Потужний концентрат, що відновлює 75 HP.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Potion,
                BonusHealth = 75,
                ValueGold = 60
            });
            Wares.Add(new ItemData
            {
                Id = "magic_ring_shop",
                ItemName = "Перстень Опору Темряві",
                Description = "Срібний перстень, що відштовхує прокляття попелу.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Ring,
                BonusHealth = 25,
                BonusDamage = 4,
                ValueGold = 110
            });
        }
        else
        {
            // Blacksmith wares
            Wares.Add(new ItemData
            {
                Id = "heavy_iron_sword",
                ItemName = "Важкий Сталевий Дворучник",
                Description = "Масивний клинок коваля, що завдає нищівних ударів.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Weapon,
                BonusDamage = 14,
                BonusHealth = 15,
                ValueGold = 85
            });
            Wares.Add(new ItemData
            {
                Id = "steel_tower_shield",
                ItemName = "Баштовий Щит Цитаделі",
                Description = "Важкий захист, що блокує найсильніші випади ворогів.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Shield,
                BonusArmor = 10,
                BonusHealth = 25,
                ValueGold = 90
            });
            Wares.Add(new ItemData
            {
                Id = "reinforced_plate",
                ItemName = "Оковані Лати Захисника Валґарду",
                Description = "Загартована пластинчаста броня для тривалих боїв.",
                Rarity = ItemRarity.Rare,
                SlotType = ItemSlotType.Chest,
                BonusArmor = 16,
                BonusHealth = 45,
                ValueGold = 160
            });
            Wares.Add(new ItemData
            {
                Id = "horned_helm",
                ItemName = "Бойовий Шолом Воїна Попелу",
                Description = "Шолом із забралом, прикрашений гербом першої імперії.",
                Rarity = ItemRarity.Rare,
                SlotType = ItemSlotType.Head,
                BonusArmor = 11,
                BonusDamage = 4,
                BonusHealth = 30,
                ValueGold = 135
            });
        }
    }

    public void UpdatePromptLabel()
    {
        if (_nameLabel == null) return;

        if (!string.IsNullOrEmpty(QuestId) && Quests.QuestManager.Instance != null)
        {
            var q = Quests.QuestManager.Instance.GetQuest(QuestId);
            if (q != null)
            {
                if (q.IsReadyToTurnIn)
                {
                    _nameLabel.Text = $"[?] [{NPCRole}]\n{NPCName}\n[E: Здати завдання!]";
                    _nameLabel.Modulate = new Color(0.35f, 1.0f, 0.6f);
                    return;
                }
                if (!q.IsAccepted)
                {
                    _nameLabel.Text = $"[!] [{NPCRole}]\n{NPCName}\n[E: Нове завдання!]";
                    _nameLabel.Modulate = new Color(1.0f, 0.85f, 0.2f);
                    return;
                }
                if (q.IsAccepted && !q.IsCompleted)
                {
                    _nameLabel.Text = $"[{NPCRole}]\n{NPCName}\n«{q.Title}»: {q.CurrentAmount}/{q.TargetAmount}";
                    _nameLabel.Modulate = new Color(0.85f, 0.85f, 0.85f);
                    return;
                }
            }
        }

        string actionText = HasShop ? "Торгівля / Розмова" : "Говорити";
        _nameLabel.Text = $"[{NPCRole}]\n{NPCName}\n[E: {actionText}]";
        _nameLabel.Modulate = new Color(1.0f, 0.85f, 0.35f);
    }

    public void Interact()
    {
        SoundManager.Instance?.PlayAltarChime();
        GraveboundHUD.Instance?.OpenShop(NPCName, NPCRole, Greeting, Wares, QuestId, HasShop);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInRange = true;
            if (_nameLabel != null) _nameLabel.Modulate = new Color(0.3f, 1.0f, 0.7f);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInRange = false;
            if (_nameLabel != null) _nameLabel.Modulate = new Color(1.0f, 0.85f, 0.35f);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playerInRange) return;
        if (GraveboundHUD.Instance != null && GraveboundHUD.Instance.IsModalOpen) return;

        if (@event.IsActionPressed("interact"))
        {
            Interact();
            GetViewport().SetInputAsHandled();
        }
    }
}
