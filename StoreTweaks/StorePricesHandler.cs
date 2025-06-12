using System.Collections.Generic;
using System.Linq;

namespace StoreTweaks;

public static class StorePricesHandler
{
    private static readonly Dictionary<int, TerminalStoreItem> Items = new Dictionary<int, TerminalStoreItem>();
    private static Dictionary<int, TerminalStoreItem>? _backup;

    public static void AddItem(int index, Item item, TerminalNode node)
    {
        if (Items.TryGetValue(index, value: out var storeItem))
        {
            storeItem.Nodes.Add(node);
            return;
        }

        Items.Add(index, new TerminalStoreItem(item, [node], item.creditsWorth));
    }

    public static void Backup()
    {
        _backup = Items.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Item[] Restore()
    {
        if (_backup == null) return [];
        
        foreach (var (_, item) in _backup)
        {
            item.Item.creditsWorth = item.OriginalPrice;
            foreach (var node in item.Nodes)
            {
                node.itemCost = item.OriginalPrice;
            }
        }

        return RenderInternal(_backup);
    }

    public static void UpdatePrice(int index, int price)
    {
        Items[index].Item.creditsWorth = price;
        foreach (var node in Items[index].Nodes)
        {
            node.itemCost = price;
        }
    }

    public static void Remove(int index)
    {
        foreach (var node in Items[index].Nodes)
        {
            node.buyItemIndex = -1;
            node.itemCost = -1;
        }
        
        
        Items.Remove(index);
    }

    public static Item[] Render()
    {
        return RenderInternal(Items);
    }

    private static Item[] RenderInternal(Dictionary<int, TerminalStoreItem> items)
    {
        var storeItems = items.Values.ToArray();
        var buyableItems = new List<Item>();

        // Recreate the buyableItemsList and reindex all nodes to match the new array.
        for (var i = 0; i < storeItems.Length; i++)
        {
            var item = storeItems[i];
            foreach (var node in item.Nodes)
            {
                node.buyItemIndex = i;
            }
            buyableItems.Add(item.Item);
        }
        
        return [.. buyableItems];
    }
}

public class TerminalStoreItem(Item item, List<TerminalNode> nodes, int price)
{
    public Item Item { get; } = item;
    public int OriginalPrice { get; } = price;
    public List<TerminalNode> Nodes { get; } = nodes;
}
