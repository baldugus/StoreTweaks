using System.Collections.Generic;
using System.Linq;

namespace StoreTweaks;

public class TerminalStoreHandler
{
    private readonly Dictionary<int, TerminalStoreItem> _items = new Dictionary<int, TerminalStoreItem>();

    public void Add(int index, Item item, TerminalNode node)
    {
        if (_items.TryGetValue(index, value: out var item1))
        {
            item1.Nodes.Add(node);
            return;
        }

        var terminalStoreItem = new TerminalStoreItem(index, item, [node]);
        _items.Add(index, terminalStoreItem);
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
        var terminalStoreItems = _items.Values.ToArray();
        var itemList = new List<Item>();

        for (var i = 0; i < terminalStoreItems.Length; i++)
        {
            var item = terminalStoreItems[i];
            foreach (var node in item.Nodes)
            {
                node.buyItemIndex = i;
            }
            itemList.Add(item.Item);
        }
        
        return itemList.ToArray();
    }
}

public class TerminalStoreItem(int index, Item item, List<TerminalNode> nodes)
{
    public int Index { get; set; } = index;
    public Item Item { get; set; } = item;
    public List<TerminalNode> Nodes { get; set; } = nodes;
}
