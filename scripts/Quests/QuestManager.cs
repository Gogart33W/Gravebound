using Godot;
using System;
using System.Collections.Generic;
using Gravebound.Audio;
using Gravebound.Inventory;
using Gravebound.Player;
using Gravebound.UI;

namespace Gravebound.Quests;

public class QuestData
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Giver { get; set; } = "";
    public string Description { get; set; } = "";
    public string ObjectiveShort { get; set; } = "";
    public string TargetLocation { get; set; } = "";
    public string TargetScene { get; set; } = "";
    public int TargetAmount { get; set; } = 1;
    public int CurrentAmount { get; set; } = 0;
    public bool IsAccepted { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    public int RewardGold { get; set; } = 50;
    public int RewardXP { get; set; } = 100;
    public ItemData? RewardItem { get; set; }
    public string LoreTitle { get; set; } = "";
    public string LoreSnippet { get; set; } = "";

    public bool IsReadyToTurnIn => IsAccepted && !IsCompleted && CurrentAmount >= TargetAmount;
}

public partial class QuestManager : Node
{
    public static QuestManager? Instance { get; private set; }

    public static string TrackedQuestId { get; set; } = "quest_crypt";

    private static readonly Dictionary<string, QuestData> _quests = new();
    private static readonly HashSet<string> _unlockedLoreIds = new();
    private static bool _initialized = false;

    [Signal] public delegate void QuestUpdatedEventHandler(string questId);
    [Signal] public delegate void QuestCompletedEventHandler(string questId, string title);
    [Signal] public delegate void TrackedQuestChangedEventHandler(string questId);

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
            InitQuests();
        }
    }

    private void InitQuests()
    {
        _quests.Clear();

        // 1. Tutorial / Crypt Quest
        _quests["quest_crypt"] = new QuestData
        {
            Id = "quest_crypt",
            Title = "Пробудження в Попелі",
            Giver = "Старійшина Ольгерд",
            Description = "Прадавній склеп Валґарду охоплений мороком. Знищіть 3 скелетів-вартових та розбийте печать на вівтарі, щоб відкрити портал у підземелля Катакомб.",
            ObjectiveShort = "Знищіть вартових (3) та дослідіть вівтар",
            TargetLocation = "Забута Крипта",
            TargetScene = "res://scenes/Main.tscn",
            TargetAmount = 3,
            CurrentAmount = 0,
            IsAccepted = true, // Main initial quest is active by default
            RewardGold = 60,
            RewardXP = 120,
            LoreTitle = "Хроніки Валґарду I: Перша Скверна Мор-Гула",
            LoreSnippet = "«Коли попіл впав із небес, королівство Валґард поринуло у сон смерті. Лише клятва Могилонароджених втримала душу світу від повного розпаду...»"
        };

        // 2. Blacksmith Brann Quest
        _quests["quest_steel"] = new QuestData
        {
            Id = "quest_steel",
            Title = "Сталь для Варти",
            Giver = "Коваль Бранн",
            Description = "Ковалю Бранну потрібна зачарована сталь із Катакомб для відновлення зброярні вартових. Знайдіть Зачарований Залізний Меч у підземеллі та принесіть його до кузні.",
            ObjectiveShort = "Знайдіть зачарований меч у катакомбах",
            TargetLocation = "Долина Згарища / Катакомби",
            TargetScene = "res://scenes/ValleyOfAshes.tscn",
            TargetAmount = 1,
            CurrentAmount = 0,
            IsAccepted = false,
            RewardGold = 100,
            RewardXP = 200,
            RewardItem = new ItemData
            {
                Id = "rare_guard_plate",
                ItemName = "Оковані Лати Захисника Валґарду",
                Description = "Загартований панцир, викуваний Бранном у нагороду за відновлення ковадла.",
                Rarity = ItemRarity.Rare,
                SlotType = ItemSlotType.Chest,
                BonusArmor = 16,
                BonusHealth = 45,
                ValueGold = 160
            },
            LoreTitle = "Хроніки Валґарду II: Забута Зброярня Вартових",
            LoreSnippet = "«Сталь першої імперії гартувалася у попелі священних вулканів. Жоден меч темряви не здатен був пробити ці оковані щити.»"
        };

        // 3. Alchemist Selena Quest
        _quests["quest_morgul"] = new QuestData
        {
            Id = "quest_morgul",
            Title = "Багряний Мор",
            Giver = "Алхімік Селена",
            Description = "Скверна попелу отруює землю. Джерело зарази — сам Архімаг Мор-Гул у глибинах катакомб. Здолайте його та принесіть есенцію для зцілення землі.",
            ObjectiveShort = "Здолайте Архімага Мор-Гула у Катакомбах",
            TargetLocation = "Катакомби Мор-Гула",
            TargetScene = "res://scenes/Catacombs.tscn",
            TargetAmount = 1,
            CurrentAmount = 0,
            IsAccepted = false,
            RewardGold = 250,
            RewardXP = 450,
            RewardItem = new ItemData
            {
                Id = "unique_fire_ring",
                ItemName = "Перстень Незгасного Полум'я",
                Description = "Древній обруч із темного золота, що пульсує життєдайною магією вогню.",
                Rarity = ItemRarity.Unique,
                SlotType = ItemSlotType.Ring,
                BonusDamage = 9,
                BonusHealth = 30,
                ValueGold = 220
            },
            LoreTitle = "Хроніки Валґарду III: Таємниця Могилонароджених",
            LoreSnippet = "«Ті, хто повстали з попелу гробниць, несуть у собі іскру незгасного полум'я — єдину силу, здатну спопелити скверну Мор-Гула.»"
        };

        // 4. Waypoints / Exploration Quest
        _quests["quest_waypoints"] = new QuestData
        {
            Id = "quest_waypoints",
            Title = "Обеліски Предків",
            Giver = "Старійшина Ольгерд",
            Description = "Прадавні обеліски ефіру зв'язують безпечний табір, склеп та катакомби. Дослідіть усі три регіони та активуйте швидке переміщення.",
            ObjectiveShort = "Активуйте 3 обеліски переміщення",
            TargetLocation = "Мережа Обелісків Акту I",
            TargetScene = "res://scenes/HubTown.tscn",
            TargetAmount = 3,
            CurrentAmount = 1, // Hub is unlocked by default
            IsAccepted = false,
            RewardGold = 150,
            RewardXP = 300,
            RewardItem = new ItemData
            {
                Id = "elixir_life_reward",
                ItemName = "Великий Еліксир Життя",
                Description = "Потужний концентрат, що відновлює 75 HP.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Potion,
                BonusHealth = 75,
                ValueGold = 60
            },
            LoreTitle = "Хроніки Валґарду IV: Мережа Обелісків Ефіру",
            LoreSnippet = "«Шляхи ефіру не підвладні часу. Доки горять руни на обелісках, воїни Варти можуть долати ліги за один подих.»"
        };

        // 5. Wilderness Patrol Quest
        _quests["quest_wilderness"] = new QuestData
        {
            Id = "quest_wilderness",
            Title = "Патрулі Згарища",
            Giver = "Старійшина Ольгерд",
            Description = "Долина Згарища за межами фортеці наповнена патрулями нежиті. Знайдіть древнє Святилище Попелу та знищіть 4 скелетів у відкритому світі.",
            ObjectiveShort = "Знищіть 4 скелетів у Долині Згарища",
            TargetLocation = "Долина Згарища (Відкритий Світ)",
            TargetScene = "res://scenes/ValleyOfAshes.tscn",
            TargetAmount = 4,
            CurrentAmount = 0,
            IsAccepted = false,
            RewardGold = 120,
            RewardXP = 240,
            RewardItem = new ItemData
            {
                Id = "magic_iron_sword_reward",
                ItemName = "Зачарований Залізний Меч",
                Description = "Гострий клинок вартових, натертий оливою могильного срібла.",
                Rarity = ItemRarity.Magic,
                SlotType = ItemSlotType.Weapon,
                BonusDamage = 8,
                BonusHealth = 10,
                ValueGold = 45
            },
            LoreTitle = "Хроніки Валґарду V: Долина Вічного Згарища",
            LoreSnippet = "«Колись тут шуміли золоті лани королівських наділів. Нині лише чорна земля та попіл, що скрипить на зубах живих...»"
        };
    }

    public QuestData? GetQuest(string id)
    {
        return _quests.TryGetValue(id, out var q) ? q : null;
    }

    public List<QuestData> GetAllQuests()
    {
        return new List<QuestData>(_quests.Values);
    }

    public List<QuestData> GetActiveQuests()
    {
        var list = new List<QuestData>();
        foreach (var q in _quests.Values)
        {
            if (q.IsAccepted && !q.IsCompleted) list.Add(q);
        }
        return list;
    }

    public List<QuestData> GetCompletedQuests()
    {
        var list = new List<QuestData>();
        foreach (var q in _quests.Values)
        {
            if (q.IsCompleted) list.Add(q);
        }
        return list;
    }

    public bool AcceptQuest(string id)
    {
        if (_quests.TryGetValue(id, out var q) && !q.IsAccepted)
        {
            q.IsAccepted = true;
            SoundManager.Instance?.PlayAltarChime();
            GraveboundHUD.Instance?.ShowToast($"📜 Прийнято завдання: «{q.Title}»", new Color(0.95f, 0.82f, 0.45f));
            EmitSignal(SignalName.QuestUpdated, id);
            return true;
        }
        return false;
    }

    public void AdvanceQuest(string id, int amount = 1)
    {
        if (_quests.TryGetValue(id, out var q) && q.IsAccepted && !q.IsCompleted)
        {
            q.CurrentAmount = Mathf.Min(q.TargetAmount, q.CurrentAmount + amount);
            EmitSignal(SignalName.QuestUpdated, id);

            if (q.CurrentAmount >= q.TargetAmount)
            {
                GraveboundHUD.Instance?.ShowToast($"✦ «{q.Title}» — Готово до здачі! (Зверніться до: {q.Giver})", new Color(0.4f, 1.0f, 0.7f));
            }
            else
            {
                GraveboundHUD.Instance?.ShowToast($"📜 «{q.Title}»: {q.CurrentAmount} / {q.TargetAmount}", new Color(0.85f, 0.85f, 0.85f));
            }
        }
    }

    public bool CompleteQuest(string id)
    {
        if (_quests.TryGetValue(id, out var q) && q.IsReadyToTurnIn)
        {
            q.IsCompleted = true;
            _unlockedLoreIds.Add(id);

            // Grant rewards
            InventoryManager.Instance?.AddGold(q.RewardGold);
            PlayerProgression.Instance?.AddXP(q.RewardXP);

            if (q.RewardItem != null)
            {
                InventoryManager.Instance?.AddItem(q.RewardItem);
            }

            SoundManager.Instance?.PlayAltarChime();
            GraveboundHUD.Instance?.ShowToast(
                $"✦ ЗАВДАННЯ ВИКОНАНО! «{q.Title}»\n+ {q.RewardGold} 💰 Золота | + {q.RewardXP} XP Досвіду",
                new Color(1.0f, 0.85f, 0.2f)
            );

            EmitSignal(SignalName.QuestCompleted, id, q.Title);
            EmitSignal(SignalName.QuestUpdated, id);
            return true;
        }
        return false;
    }

    public bool IsLoreUnlocked(string questId) => _unlockedLoreIds.Contains(questId);

    public void SetTrackedQuest(string questId)
    {
        TrackedQuestId = questId;
        EmitSignal(SignalName.TrackedQuestChanged, questId);
        GraveboundHUD.Instance?.RefreshQuestBox();
    }

    public QuestData? GetTrackedQuest()
    {
        if (!string.IsNullOrEmpty(TrackedQuestId) && _quests.TryGetValue(TrackedQuestId, out var q))
        {
            return q;
        }

        // Fallback: first active quest, or first accepted quest
        foreach (var quest in _quests.Values)
        {
            if (quest.IsAccepted && !quest.IsCompleted) return quest;
        }

        return _quests.TryGetValue("quest_crypt", out var first) ? first : null;
    }
}
