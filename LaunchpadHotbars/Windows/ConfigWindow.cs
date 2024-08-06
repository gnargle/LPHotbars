using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace LaunchpadHotbars.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration config;
    private LaunchpadHandler launchpadHandler;
    

    private LaunchpadHotbarsPlugin plugin;

    private string failed = String.Empty;

    public ConfigWindow(LaunchpadHotbarsPlugin plugin, LaunchpadHandler launchpadHandler) : base("Launchpad Config###Launchpad Config")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(500, 400);
        SizeCondition = ImGuiCond.Appearing;

        config = plugin.Configuration;
        this.plugin = plugin;
        this.launchpadHandler = launchpadHandler;      
    }    

    public void Dispose() { }

    public override void PreDraw()
    {
        
        // Flags must be added or removed before Draw() is being called, or they won't apply        
    }

    public override void Draw()
    {
        var selectedLP = config.SelectedLaunchpadId;
        var selectedLPint = launchpadHandler.LaunchpadIDs.IndexOf(selectedLP);
        if (ImGui.Button("Refresh Launchpad List"))
        {
            launchpadHandler.ListLaunchpads();
        }

        if (ImGui.Combo("Launchpad to connect to", ref selectedLPint, launchpadHandler.LaunchpadIDs.ToArray(), launchpadHandler.LaunchpadIDs.Count()))
        {
            config.SelectedLaunchpadId = launchpadHandler.LaunchpadIDs[selectedLPint];            
            config.Save();
            launchpadHandler.SelectLaunchpad();
        }

        if (!String.IsNullOrWhiteSpace(config.SelectedLaunchpadId) && launchpadHandler.LaunchpadIDs.Contains(config.SelectedLaunchpadId) && ImGui.Button("Connect Launchpad"))
        {
            launchpadHandler.SelectLaunchpad();
            launchpadHandler.ConnectLaunchpad();
        }
        
        if (!String.IsNullOrWhiteSpace(config.SelectedLaunchpadId) && launchpadHandler.LaunchpadIDs.Contains(config.SelectedLaunchpadId) && launchpadHandler.LaunchpadReady)
        {
            if (launchpadHandler.LaunchpadConnected && ImGui.Button("Test Connection"))
            {
                launchpadHandler.TestConnection();
            }
            if (launchpadHandler.LaunchpadConnected && ImGui.Button("Disconnect"))
            {
                launchpadHandler.Disconnect();
            }
        }
        if (!String.IsNullOrWhiteSpace(failed))
        {
            plugin.ChatError(failed);
        }
        if (ImGui.Button("action test"))
        {
            unsafe
            {
                if (RaptureHotbarModule.Instance() != null)
                {
                    if (RaptureHotbarModule.Instance()->Hotbars != null)
                    {
                        var hotbar = RaptureHotbarModule.Instance()->Hotbars[1];
                        if (hotbar.Slots != null)
                        {
                            var slot = hotbar.GetHotbarSlot(0);
                            if (slot != null)
                            {
                                RaptureHotbarModule.Instance()->ExecuteSlot(slot);
                            }
                        }
                    }
                }
            }
        }
    }
}
