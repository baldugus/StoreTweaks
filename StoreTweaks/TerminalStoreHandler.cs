using System.Collections.Generic;
using System.Linq;

namespace StoreTweaks;

public class TerminalStoreHandler
{
    private readonly Dictionary<int, TerminalStoreItem> _items = new Dictionary<int, TerminalStoreItem>();

    public void Add(int index, Item item, TerminalNode node)
    {
        if (_items.TryGetValue(index, value: out var storeItem))
        {
            storeItem.Nodes.Add(node);
            return;
        }

        _items.Add(index, new TerminalStoreItem(item, [node]));
    }

    public void UpdatePrice(int index, int price)
    {
        _items[index].Item.creditsWorth = price;
        foreach (var node in _items[index].Nodes)
        {
            node.itemCost = price;
        }
    }

    public void Remove(int index)
    {
        foreach (var node in _items[index].Nodes)
        {
            node.buyItemIndex = -1;
            node.itemCost = -1;
        }
        
        
        _items.Remove(index);
    }

    public Item[] Render()
    {
        var storeItems = _items.Values.ToArray();
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
        
        return buyableItems.ToArray();
    }
}

public class TerminalStoreItem(Item item, List<TerminalNode> nodes)
{
    public Item Item { get; } = item;
    public List<TerminalNode> Nodes { get; } = nodes;
}
