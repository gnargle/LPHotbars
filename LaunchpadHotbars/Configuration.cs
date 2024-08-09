using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace LaunchpadHotbars;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public string SelectedLaunchpadId { get; set; } = String.Empty;
    public bool ShowDebugMessages { get; set; } = false;
    public List<LaunchpadButton> LaunchpadGrid { get; set; } = new List<LaunchpadButton>();
    public int BackgroundColourRed { get; set; }
    public int BackgroundColourGreen { get; set; }
    public int BackgroundColourBlue { get; set; }
    public Vector4 BackgroundColour { get; set; } = new Vector4();

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        LaunchpadHotbarsPlugin.PluginInterface.SavePluginConfig(this);
    }
}
