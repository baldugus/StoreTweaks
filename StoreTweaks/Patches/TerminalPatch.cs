using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalNetworkAPI;
using Steamworks.ServerList;
using Unity.Netcode;
using UnityEngine.ProBuilder;

namespace StoreTweaks.Patches;

[HarmonyPatch(typeof(Terminal))]
public class TerminalPatch
{
    public static LethalNetworkVariable<Dictionary<string, (bool, int)>> _itemList = new LethalNetworkVariable<Dictionary<string, (bool, int)>>(identifier: "buyableItemsList");
    public static Dictionary<string, (bool, int)> items = new Dictionary<string, (bool, int)>();

    [HarmonyPatch(nameof(Terminal.Awake))]
    [HarmonyPostfix]
    private static void AwakePatch(Terminal __instance)
    {
       var cfg = new TerminalConfig(StoreTweaks.Instance.Config, __instance.buyableItemsList.ToList());
       items = cfg.items;
    }

    [HarmonyPatch(nameof(Terminal.RotateShipDecorSelection))]
    [HarmonyPrefix]
    private static void RotateShipDecorSelectionPatch(Terminal __instance)
    {
        StoreTweaks.Logger.LogDebug("Tweaking the terminal...");

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            _itemList.Value = items;
        }

        var newItemList = new List<Item> { };
        for (int i = __instance.buyableItemsList.Length - 1; i >= 0; i--)
        {
            var item = __instance.buyableItemsList[i];

            if (!_itemList.Value.TryGetValue(item.itemName, out var itemInfo) || !itemInfo.Item1)
            {
                continue;
            }

            item.creditsWorth = itemInfo.Item2;
            newItemList.Add(item);
        }

        newItemList.Reverse();
        __instance.buyableItemsList = newItemList.ToArray();
    }
}
