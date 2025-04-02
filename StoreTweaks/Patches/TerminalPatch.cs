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
    public static LethalNetworkVariable<Dictionary<string, (bool, int)>> _itemList = new LethalNetworkVariable<Dictionary<string, (bool, int)>>(identifier: "items");
    private static Dictionary<string, (bool, int)> _configItems = new Dictionary<string, (bool, int)>();
    private static readonly TerminalStoreHandler TerminalStoreHandler = new TerminalStoreHandler();

    [HarmonyPatch(nameof(Terminal.Awake))]
    [HarmonyPostfix]
    private static void AwakePatch(Terminal __instance)
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var cfg = new TerminalConfig(StoreTweaks.Instance.Config, __instance.buyableItemsList.ToList());
            _configItems = cfg.items;
        }
        
        var allItems = Resources.FindObjectsOfTypeAll<TerminalNode>().ToList();
        var storeNodes = allItems.FindAll(n => n.buyItemIndex >= 0);

        foreach (var storeNode in storeNodes)
        {
            TerminalStoreHandler.Add(storeNode.buyItemIndex, __instance.buyableItemsList[storeNode.buyItemIndex], storeNode);
        }
    }

    [HarmonyPatch(nameof(Terminal.RotateShipDecorSelection))]
    [HarmonyPrefix]
    private static void RotateShipDecorSelectionPatch(Terminal __instance)
    {
        StoreTweaks.Logger.LogDebug("Tweaking the terminal...");

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            _itemList.Value = _configItems;
        }

        for (int i = __instance.buyableItemsList.Length - 1; i >= 0; i--)
        {
            var item = __instance.buyableItemsList[i];

            if (!_itemList.Value.TryGetValue(item.itemName, out var itemInfo) || !itemInfo.Item1)
            {
                TerminalStoreHandler.Remove(i);
                continue;
            }

            TerminalStoreHandler.UpdatePrice(i, itemInfo.Item2);
        }

        __instance.buyableItemsList = TerminalStoreHandler.Render();
    }

    [HarmonyPatch(nameof(Terminal.LoadNewNode))]
    [HarmonyPrefix]
    private static void LoadNewNodePatch(Terminal __instance, ref TerminalNode node)
    {
        // i'm not proud of this workaround
        if (node.itemCost == -1)
        {
            StoreTweaks.Logger.LogDebug("Found a disabled item, returning error to screen");
            // ParseError1
            node = __instance.terminalNodes.specialNodes[10];
        }
    }
}
