using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace StoreTweaks;

public class TerminalConfig
{
    public readonly Dictionary<string, (bool, int)> items = new Dictionary<string, (bool, int)>();
    
    public TerminalConfig(ConfigFile cfg, List<Item> itemList)
    {
        cfg.SaveOnConfigSet = false;

        foreach (Item item in itemList)
        {
            ConfigEntry<bool> enabled = cfg.Bind<bool>(item.itemName, "Enabled", true, "Enables/Disable the item from the store");
            ConfigEntry<int> price = cfg.Bind<int>(item.itemName, "Price", item.creditsWorth, "Price of the item in credits");
            
            items.Add(item.itemName, (enabled.Value, price.Value)); 
        }
        
        ClearOrphanedEntries(cfg);
        
        cfg.Save();
        cfg.SaveOnConfigSet = true;
    }

    static void ClearOrphanedEntries(ConfigFile cfg)
    {
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        orphanedEntries.Clear();
    }
}
