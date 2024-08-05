using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using LaunchpadNET;

namespace LaunchpadHotbars.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    private Interface.LaunchpadDevice[] lps;
    private List<String> lpIDs;
    private Interface.LaunchpadDevice? launchpad;
    private Interface lpIface;

    private LaunchpadHotbarsPlugin Plugin;

    private string failed = String.Empty;

    public ConfigWindow(LaunchpadHotbarsPlugin plugin) : base("Launchpad Config###Launchpad Config")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(500, 400);
        SizeCondition = ImGuiCond.Appearing;

        Configuration = plugin.Configuration;
        this.Plugin = plugin;

        var logAction = delegate (string name)
        {
            Plugin.ChatError(name);
        };
        lps = Interface.getConnectedLaunchpads(logAction);
        lpIDs = new List<string>() { "" }.Union(lps.Where(l => l._isLegacy).Select(l => l._midiName)).Union(lps.Where(p => !p._isLegacy).Select(p => p._midiIn)).ToList();
        lpIface = new Interface(logAction, logAction, logAction);
        try
        {
            if (!String.IsNullOrWhiteSpace(Configuration.SelectedLaunchpadId) && lpIDs.Contains(Configuration.SelectedLaunchpadId))
                SelectLaunchpad(Configuration.SelectedLaunchpadId);
        }catch (Exception ex)
        {
            failed = ex.Message;
        }
    }    

    public void Dispose() { }

    public override void PreDraw()
    {
        
        // Flags must be added or removed before Draw() is being called, or they won't apply        
    }

    private void SelectLaunchpad(string name)
    {
        foreach (var lp in lps)
        {
            if (lp._isLegacy && lp._midiName == name)
            {
                launchpad = lp;
                break;

            } else if (lp._midiIn == name) {
                launchpad = lp;
                break;
            }
        }        
    }

    public override void Draw()
    {
        var selectedLP = Configuration.SelectedLaunchpadId;
        var selectedLPint = lpIDs.IndexOf(selectedLP);
        if (ImGui.Combo("Launchpad to connect to", ref selectedLPint, lpIDs.ToArray(), lpIDs.Count()))
        {
            Configuration.SelectedLaunchpadId = lpIDs[selectedLPint];
            SelectLaunchpad(Configuration.SelectedLaunchpadId);
            Configuration.Save();
        }

        if (!String.IsNullOrWhiteSpace(Configuration.SelectedLaunchpadId) && lpIDs.Contains(Configuration.SelectedLaunchpadId) && launchpad != null)
        {
            if (ImGui.Button("Test Connection"))
            {
                try
                {
                    lpIface.connect(launchpad);
                    lpIface.SetMode(LaunchpadMode.Programmer);
                    if (lpIface.IsLegacy)
                    {
                        lpIface.createTextScroll("FINAL FANTASY XIV", 10, true, 125);
                    }
                    else
                    {
                        lpIface.createTextScrollMiniMk3RGB("FINAL FANTASY XIV", 10, true, 0xFF, 0x00, 0xFB);
                    }
                    lpIface.disconnect(launchpad);
                }
                catch (Exception ex)
                {
                    Plugin.ChatError(ex.Message);
                }
            }
            if (ImGui.Button("Disconnect"))
            {
                lpIface.disconnect(launchpad);
            }
        }
        if (!String.IsNullOrWhiteSpace(failed))
        {
            ImGui.TextUnformatted(failed);
        }
    }
}
