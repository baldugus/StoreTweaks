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
    private static readonly LethalNetworkVariable<Dictionary<string, ItemConfig>> _sharedItemsConfig = new LethalNetworkVariable<Dictionary<string, ItemConfig>>(identifier: "items");
    private static Dictionary<string, ItemConfig> _itemsConfig = new Dictionary<string, ItemConfig>();
    private static readonly TerminalStoreHandler TerminalStoreHandler = new TerminalStoreHandler();
    private static bool _tweaked;

    [HarmonyPatch(nameof(Terminal.Awake))]
    [HarmonyPostfix]
    private static void AwakePatch(Terminal __instance)
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            StoreTweaks.Logger.LogInfo("I'm hosting, initializing configs.");
            var cfg = new TerminalConfig(StoreTweaks.Instance.Config, __instance.buyableItemsList.ToList());
            _itemsConfig = cfg.Items;
        }

        var storeNodes = FindStoreNodes();
        BuildStoreHandler(storeNodes, __instance.buyableItemsList);
    }

    [HarmonyPatch(nameof(Terminal.RotateShipDecorSelection))]
    [HarmonyPrefix]
    private static void RotateShipDecorSelectionPatch(Terminal __instance)
    {
        if (_tweaked) { return; }
        StoreTweaks.Logger.LogInfo("Tweaking the terminal.");

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            _sharedItemsConfig.Value = _itemsConfig;
        }
        
        ApplyConfig(__instance.buyableItemsList);

        __instance.buyableItemsList = TerminalStoreHandler.Render();
        _tweaked = true;
    }

    [HarmonyPatch(nameof(Terminal.LoadNewNode))]
    [HarmonyPrefix]
    private static void LoadNewNodePatch(Terminal __instance, ref TerminalNode node)
    {
        // I'm not proud of this workaround
        if (node.itemCost != -1) return;
        StoreTweaks.Logger.LogDebug($"{node.name} is disabled, returning a parse error to the terminal.");
        
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
            // Some mods fill the buyItemIndex even if it's not a store item for some reason.
            if (node.buyItemIndex >= buyableItems.Length)
            {
                StoreTweaks.Logger.LogWarning($"Item {node.name} has a buyItemIndex ({node.buyItemIndex}) which is out of range ({buyableItems.Length}), skipping.");
                continue;
            }
            TerminalStoreHandler.Add(node.buyItemIndex, buyableItems[node.buyItemIndex], node);
        }
    }

    private static void ApplyConfig(Item[] buyableItems)
    {
        var cfg = _sharedItemsConfig.Value;
        for (var i = 0; i < buyableItems.Length; i++)
        {
            var item = buyableItems[i];

            if (!cfg.TryGetValue(item.itemName, out var itemConfig) || !itemConfig.Enabled)
            {
                StoreTweaks.Logger.LogDebug($"Removing item {item.name} from store.");
                TerminalStoreHandler.Remove(i);
                continue;
            }

            StoreTweaks.Logger.LogDebug($"Setting item {item.name} price to {itemConfig.Price}");
            TerminalStoreHandler.UpdatePrice(i, itemConfig.Price);
        }
    }
}
