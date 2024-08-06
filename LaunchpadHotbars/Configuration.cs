using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace LaunchpadHotbars;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public string SelectedLaunchpadId { get; set; } = String.Empty;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        LaunchpadHotbarsPlugin.PluginInterface.SavePluginConfig(this);
    }
}
