namespace StoreTweaks.Patches;

using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(QuickMenuManager))]
public class QuickMenuManagerPatch
{
    [HarmonyPatch(nameof(QuickMenuManager.LeaveGameConfirm))]
    [HarmonyPrefix]
    // Are we sure there's no Terminal method called on disconnect? I hate FindObjectOfType
    private static void LeaveGameConfirmPatch()
    {
        StoreTweaks.Logger.LogDebug("Leaving game, reverting patch.");
        var terminal = Object.FindObjectOfType<Terminal>();
        terminal.buyableItemsList = StorePricesHandler.Restore();
        TerminalPatch.Unlock();
    }
}
