using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LethalNetworkAPI;
using Unity.Netcode;
using UnityEngine;

namespace StoreTweaks.Patches;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatch
{
    private static readonly LethalNetworkVariable<Dictionary<string, ItemConfig>> SharedItemsConfig = new LethalNetworkVariable<Dictionary<string, ItemConfig>>(identifier: "items");
    private static Dictionary<string, ItemConfig> _itemsConfig = new Dictionary<string, ItemConfig>();

    [HarmonyPatch(nameof(Terminal.Awake))]
    [HarmonyPostfix]
    private static void Setup(Terminal __instance)
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            StoreTweaks.Logger.LogInfo("I'm hosting, initializing configs.");
            var cfg = new TerminalConfig(StoreTweaks.Instance.Config, __instance.buyableItemsList.ToList());
            _itemsConfig = cfg.Items;
        }

        var terminal = __instance;
        void OnConfigChanged(Dictionary<string, ItemConfig> _)
        {
            StoreTweaks.Logger.LogDebug("SharedItemsConfig changed, applying config");
            ApplyConfig(terminal.buyableItemsList);
            
            terminal.buyableItemsList = StorePricesHandler.Render();
            terminal.InitializeItemSalesPercentages();
            terminal.SetItemSales();
            SharedItemsConfig.OnValueChanged -= OnConfigChanged;
        }
        SharedItemsConfig.OnValueChanged += OnConfigChanged;

        var storeNodes = FindStoreNodes();
        BuildStoreHandler(storeNodes, __instance.buyableItemsList);

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            SharedItemsConfig.Value = _itemsConfig;
        }
    }

    [HarmonyPatch(nameof(Terminal.LoadNewNode))]
    [HarmonyPrefix]
    private static void CheckNodeAvailability(Terminal __instance, ref TerminalNode node)
    {
        // I'm not proud of this workaround
        if (node.itemCost != -1) return;
        StoreTweaks.Logger.LogDebug($"{node.name} is disabled in configs, returning a parse error to the terminal.");
        
        // ParseError1
        node = __instance.terminalNodes.specialNodes[10];
    }

    private static List<TerminalNode> FindStoreNodes()
    {
        var allNodes = Resources.FindObjectsOfTypeAll<TerminalNode>().ToList();
        var storeNodes = allNodes.FindAll(n => n.buyItemIndex >= 0);
        
        return storeNodes;
    }

    private static void BuildStoreHandler(List<TerminalNode> nodes, Item[] buyableItems)
    {
        foreach (var node in nodes)
        {
            // Some mods fill the buyItemIndex even if it's not a store item for some reason
            if (node.buyItemIndex >= buyableItems.Length)
            {
                StoreTweaks.Logger.LogWarning($"Item {node.name} has a buyItemIndex ({node.buyItemIndex}) which is out of range ({buyableItems.Length}), skipping.");
                continue;
            }
            StorePricesHandler.AddItem(node.buyItemIndex, buyableItems[node.buyItemIndex], node);
        }
        
        // If we want to support clients with different configs, we have to be able to revert changes so they can apply
        // their own configs after leaving a server and hosting their own.
        StorePricesHandler.Backup();
    }

    private static void ApplyConfig(Item[] buyableItems)
    {
        var cfg = SharedItemsConfig.Value;
        for (var i = 0; i < buyableItems.Length; i++)
        {
            var item = buyableItems[i];

            if (!cfg.TryGetValue(item.itemName, out var itemConfig) || !itemConfig.Enabled)
            {
                StoreTweaks.Logger.LogDebug($"Removing item {item.name} from store.");
                StorePricesHandler.Remove(i);
                continue;
            }

            StoreTweaks.Logger.LogDebug($"Setting item {item.name} price to {itemConfig.Price}");
            StorePricesHandler.UpdatePrice(i, itemConfig.Price);
        }
    }
}
