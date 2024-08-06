using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace LaunchpadHotbars.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration config;
    private LaunchpadHandler launchpadHandler;
    private List<LaunchpadButton> lpGrid = new List<LaunchpadButton>();

    private LaunchpadHotbarsPlugin plugin;

    private string failed = String.Empty;

    private string[] numbers = new string[] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
    private int[] CCTopRow = new int[] { 91, 92, 93, 94, 95, 96, 97, 98, 0 }; //last is the logo
    private int[] CCRightColumn = new int[] { 0, 89, 79, 69, 59, 49, 39, 29, 19 }; //first is the logo

    public ConfigWindow(LaunchpadHotbarsPlugin plugin, LaunchpadHandler launchpadHandler) : base("Launchpad Config###Launchpad Config")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(550, 850);
        SizeCondition = ImGuiCond.Appearing;

        config = plugin.Configuration;
        this.plugin = plugin;
        this.launchpadHandler = launchpadHandler;
        lpGrid = new List<LaunchpadButton>();
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

        if (!String.IsNullOrWhiteSpace(config.SelectedLaunchpadId) && launchpadHandler.LaunchpadIDs.Contains(config.SelectedLaunchpadId) && !launchpadHandler.LaunchpadConnected && ImGui.Button("Connect Launchpad"))
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

            if (launchpadHandler.LaunchpadConnected)
            {
                ImGui.TextUnformatted("The following table maps to the grid of buttons on your launchpad.\nIn each cell, the top dropdown is the hotbar number, and the bottom is the slot.");
                ImGui.BeginTable("launchpadtable", 9, ImGuiTableFlags.Borders, new Vector2(500, 500));
                //ok, now draw the grid and put a hotbar number and slot number in each.
                for (int x = 0; x < 9; x++)
                {
                    ImGui.TableSetupColumn("Column " + x, ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableNextRow();
                    for (int y = 0; y < 9; y++)
                    {
                        ImGui.TableNextColumn();
                        if (x == 0 && y == 8)
                        {
                            ImGui.TextUnformatted("logo");
                            continue;
                        }
                        ImGui.TextUnformatted($"x:{x},y:{y}");
                        LaunchpadButton? lpButton = null;
                        if (x == 0)
                        {
                            lpButton = lpGrid.FirstOrDefault(b => b.CCVal == CCTopRow[y]);
                            if (lpButton != null)
                                lpButton = config.LaunchpadGrid.FirstOrDefault(b => b.CCVal == CCTopRow[y]);
                            if (lpButton == null)
                                lpButton = new LaunchpadButton { CCVal = CCTopRow[y] };
                        }
                        else if (y == 8)
                        {
                            lpButton = lpGrid.FirstOrDefault(b => b.CCVal == CCRightColumn[x]);
                            if (lpButton != null)
                                lpButton = config.LaunchpadGrid.FirstOrDefault(b => b.CCVal == CCRightColumn[x]);
                            if (lpButton == null)
                                lpButton = new LaunchpadButton { CCVal = CCRightColumn[x] };
                        }
                        else
                        {
                            lpGrid.FirstOrDefault(b => b.XCoord == x && b.YCoord == y);
                            if (lpButton == null)
                            {
                                lpButton = config.LaunchpadGrid.FirstOrDefault(b => b.XCoord == x - 1 && b.YCoord == y);
                            }
                            if (lpButton == null)
                                lpButton = new LaunchpadButton { XCoord = x - 1, YCoord = y };
                        }
                        lpGrid.Add(lpButton);

                        int hotbar = 0;
                        if (lpButton.Hotbar.HasValue)
                            hotbar = lpButton.Hotbar.Value + 1;
                        ImGui.SetNextItemWidth(50);
                        if (ImGui.Combo($"###{x}{y}hotbar", ref hotbar, numbers, numbers.Count()))
                        {
                            if (hotbar == 0)
                            {
                                lpButton.Hotbar = null;
                            }
                            else
                            {
                                lpButton.Hotbar = hotbar - 1;
                            }
                        }

                        int slot = 0;
                        if (lpButton.Slot.HasValue)
                            slot = (int)lpButton.Slot.Value + 1;
                        ImGui.SetNextItemWidth(50);
                        if (ImGui.Combo($"###{x}{y}slot", ref slot, numbers, numbers.Count()))
                        {
                            if (slot == 0)
                            {
                                lpButton.Slot = null;
                            }
                            else
                            {
                                lpButton.Slot = (uint)slot - 1;
                            }
                        }

                    }
                }
                ImGui.EndTable();

                if (ImGui.Button("Save Launchpad Mapping"))
                {
                    config.LaunchpadGrid = lpGrid;
                    foreach (var btn in config.LaunchpadGrid)
                    {
                        plugin.ChatError($"button {btn.XCoord},{btn.YCoord} mapped to hotbar {btn.Hotbar}, slot {btn.Slot}");
                    }
                    config.Save();
                }
            }
        }

        var debug = config.ShowDebugMessages;
        if (ImGui.Checkbox("Show Debug Messages in chatlog", ref debug))
        {
            config.ShowDebugMessages = debug;
            config.Save();
        }
    }
}
