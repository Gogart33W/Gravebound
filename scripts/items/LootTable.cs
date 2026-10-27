using Godot;

namespace Gravebound.Items;

[GlobalClass]
public partial class LootTable : Resource
{
    [Export] public Godot.Collections.Array<LootEntry> Entries { get; set; } = new();
    [Export] public int MinDrops { get; set; } = 1;
    [Export] public int MaxDrops { get; set; } = 2;
    [Export(PropertyHint.Range, "0.0,1.0")] public float DropChance { get; set; } = 1.0f;

    public Godot.Collections.Array<ItemData> RollDrops()
    {
        var drops = new Godot.Collections.Array<ItemData>();

        if (Entries.Count == 0 || GD.Randf() > DropChance)
        {
            return drops;
        }

        // Calculate total weight of valid entries
        float totalWeight = 0.0f;
        foreach (var entry in Entries)
        {
            if (entry.Item != null && entry.Weight > 0.0f)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0.0f)
        {
            return drops;
        }

        int dropCount = (int)GD.RandRange(MinDrops, MaxDrops);
        for (int i = 0; i < dropCount; i++)
        {
            float roll = (float)GD.RandRange(0.0f, totalWeight);
            float currentSum = 0.0f;

            foreach (var entry in Entries)
            {
                if (entry.Item == null || entry.Weight <= 0.0f)
                {
                    continue;
                }

                currentSum += entry.Weight;
                if (roll <= currentSum)
                {
                    // Check individual item drop chance
                    if (GD.Randf() <= entry.DropChance)
                    {
                        drops.Add(entry.Item);
                    }
                    break;
                }
            }
        }

        return drops;
    }
}
