using System;
using System.Reflection;
using BepInEx.Bootstrap;
using System.Collections.Generic;

namespace StoreTweaks;

public static class Compatibility {
    public static bool ModLoaded(string guid) {
        return Chainloader.PluginInfos.ContainsKey(guid);
    }

    public static void ApplyMrovLibPatch(Terminal terminal)
    {
        if (!ModLoaded("MrovLib")) return;
        
        StoreTweaks.Logger.LogInfo("MrovLib is loaded, rebuilding buyables.");
        var mrovLibPlugin = Chainloader.PluginInfos["MrovLib"];
        StoreTweaks.Logger.LogDebug($"Found MrovLib plugin: {mrovLibPlugin.Metadata.Name} v{mrovLibPlugin.Metadata.Version}");
        
        var mrovLibAssembly = mrovLibPlugin.Instance.GetType().Assembly;
        StoreTweaks.Logger.LogDebug("Got MrovLib assembly");
        
        var contentManagerType = mrovLibAssembly.GetType("MrovLib.ContentManager");
        StoreTweaks.Logger.LogDebug($"Found ContentManager type: {contentManagerType != null}");
        
        var buyablesField = contentManagerType?.GetField("Buyables", BindingFlags.Public | BindingFlags.Static);
        StoreTweaks.Logger.LogDebug($"Found Buyables field: {buyablesField != null}");
        
        var buyables = buyablesField?.GetValue(null) as System.Collections.IList;
        StoreTweaks.Logger.LogDebug($"Got buyables list: {buyables != null}");
        
        if (buyables != null)
        {
            var buyableItemType = mrovLibAssembly.GetType("MrovLib.ContentType.BuyableItem");
            var buyableThingType = buyableItemType?.BaseType;
            
            for (var i = buyables.Count - 1; i >= 0; i--)
            {
                var buyable = buyables[i];
                var buyableType = buyable.GetType();
                if (buyableType != buyableItemType)
                {
                    continue;
                }
                
                var baseType = buyableType.BaseType;
                var nodesField = baseType.GetField("Nodes", BindingFlags.Public | BindingFlags.Instance);
                
                if (nodesField != null)
                {
                    var nodesType = nodesField.FieldType;
                    var nodeField = nodesType.GetField("Node", BindingFlags.Public | BindingFlags.Instance);
                    
                    if (nodeField != null)
                    {
                        var nodeType = nodeField.FieldType;
                        var buyItemIndexField = nodeType.GetField("buyItemIndex", BindingFlags.Public | BindingFlags.Instance);
                        
                        var nodes = nodesField.GetValue(buyable);
                        var node = nodeField.GetValue(nodes);
                        var buyItemIndex = (int)(buyItemIndexField?.GetValue(node) ?? -1);
                        
                        if (buyItemIndex == -1)
                        {
                            buyables.RemoveAt(i);
                            continue;
                        }
                        
                        var priceProperty = baseType.GetProperty("Price", BindingFlags.Public | BindingFlags.Instance);
                        if (priceProperty != null && buyItemIndex >= 0 && buyItemIndex < terminal.buyableItemsList.Length)
                        {
                            var creditsWorth = terminal.buyableItemsList[buyItemIndex].creditsWorth;
                            priceProperty.SetValue(buyable, creditsWorth);
                        }
                    }
                }
            }
        }
        
        StoreTweaks.Logger.LogInfo($"Finished cleaning up buyables, count: {buyables?.Count ?? 0}");
    }
}