using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        var newItems = new Dictionary<int, TerminalStoreItem>();

        // Recreate the buyableItemsList and reindex all nodes to match the new array.
        for (var i = 0; i < storeItems.Length; i++)
        {
            var item = storeItems[i];
            StoreTweaks.Logger.LogDebug($"Rendering item {i}: {item.Item.itemName}");
            foreach (var node in item.Nodes)
            {
                StoreTweaks.Logger.LogDebug($"  Before: Node {node.name} buyItemIndex: {node.buyItemIndex}");
                node.buyItemIndex = i;
                StoreTweaks.Logger.LogDebug($"  After: Node {node.name} buyItemIndex: {node.buyItemIndex}");
            }
            buyableItems.Add(item.Item);
            newItems[i] = item;
        }
        
        Items.Clear();
        foreach (var (key, value) in newItems)
        {
            Items[key] = value;
        }
        
        return [.. buyableItems];
    }

    public static object? GetRelatedNodesForMrovLib(int index, Type relatedNodesType)
    {
        StoreTweaks.Logger.LogDebug($"Getting nodes for index {index}, Items count: {Items.Count}");
        StoreTweaks.Logger.LogDebug($"Items keys: {string.Join(", ", Items.Keys)}");
        
        var nodes = Items[index].Nodes;
        var relatedNodes = Activator.CreateInstance(relatedNodesType);
        
        var node = nodes.FirstOrDefault(node => !node.isConfirmationNode);
        var nodeConfirm = nodes.FirstOrDefault(node => node.isConfirmationNode);
        
        StoreTweaks.Logger.LogDebug($"Node for {index}: {node?.name}, buyItemIndex: {node?.buyItemIndex}");
        StoreTweaks.Logger.LogDebug($"NodeConfirm for {index}: {nodeConfirm?.name}, buyItemIndex: {nodeConfirm?.buyItemIndex}");
        
        if (node == null || nodeConfirm == null)
        {
            StoreTweaks.Logger.LogWarning($"Missing required nodes for item {index}");
            return null;
        }
        
        // Use the original nodes
        relatedNodesType.GetProperty("Node")?.SetValue(relatedNodes, node);
        relatedNodesType.GetProperty("NodeConfirm")?.SetValue(relatedNodes, nodeConfirm);
        
        StoreTweaks.Logger.LogDebug($"After setting: Node buyItemIndex: {node.buyItemIndex}, NodeConfirm buyItemIndex: {nodeConfirm.buyItemIndex}");
        
        return relatedNodes;
    }
}

public class TerminalStoreItem(Item item, List<TerminalNode> nodes, int price)
{
    public Item Item { get; } = item;
    public int OriginalPrice { get; } = price;
    public List<TerminalNode> Nodes { get; } = nodes;
}
