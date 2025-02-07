using BepInEx.Configuration;
using HarmonyLib;
using LethalNetworkAPI;
using Unity.Netcode;
using UnityEngine.ProBuilder;

namespace StoreTweaks.Patches;

[HarmonyPatch(typeof(Terminal))]
public class StorePatch
{
    private static LethalNetworkVariable<Item[]> _itemList = new LethalNetworkVariable<Item[]>(identifier: "buyableItemsList");

    [HarmonyPatch(nameof(Terminal.Awake))]
    [HarmonyPostfix]
    [HarmonyPriority(500)]
    private static void AwakePostfix(Terminal __instance)
    {
        StoreTweaks.Logger.LogDebug("Tweaking the terminal...");

        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
        {
            __instance.buyableItemsList = _itemList.Value;
            return;
        }

        for (int i = __instance.buyableItemsList.Length-1; i >= 0; i--)
        {
            var item = __instance.buyableItemsList[i];
            ConfigEntry<bool> enabled = StoreTweaks.Instance.Config.Bind<bool>(item.itemName, "Enabled", true, "Enables/Disable the item from the store");
            ConfigEntry<int> price = StoreTweaks.Instance.Config.Bind<int>(item.itemName, "Price", item.creditsWorth, "Price of the item in credits");

            if (!enabled.Value)
            {
                __instance.buyableItemsList.RemoveAt(i);
                continue;
            }

            __instance.buyableItemsList[i].creditsWorth = price.Value;
        }

        _itemList.Value = __instance.buyableItemsList;
    }
}
