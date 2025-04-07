using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

namespace StoreTweaks;

public class TerminalConfig
{
    public readonly Dictionary<string, ItemConfig> Items = new Dictionary<string, ItemConfig>();
    
    public TerminalConfig(ConfigFile cfg, List<Item> itemList)
    {
        cfg.SaveOnConfigSet = false;

        foreach (var item in itemList)
        {
            var enabled = cfg.Bind($"Item: {item.itemName}",
                "Enabled",
                true,
                "Enables/Disable the item from the store");
            
            var price = cfg.Bind($"Item: {item.itemName}",
                "Price",
                item.creditsWorth,
                "Price of the item in credits");
            
            Items.Add(item.itemName, new ItemConfig(enabled.Value, price.Value)); 
        }
        
        ClearOrphanedEntries(cfg);
        
        cfg.Save();
        cfg.SaveOnConfigSet = true;
    }

    private static void ClearOrphanedEntries(ConfigFile cfg)
    {
        var orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        orphanedEntries.Clear();
    }
}

public class ItemConfig(bool enabled, int price)
{
    public readonly bool Enabled = enabled;
    public readonly int Price = price;
}
