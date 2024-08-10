using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Lua;
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
    uint rgbint; 

    private LaunchpadHotbarsPlugin plugin;

    private bool testInProgress = false;

    private string[] hotbarNumbers = new string[] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
    private string[] slotNumbers = new string[] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };

    public ConfigWindow(LaunchpadHotbarsPlugin plugin, LaunchpadHandler launchpadHandler) : base("Launchpad Config###Launchpad Config")
    {
        Flags = ImGuiWindowFlags.NoCollapse;

        Size = new Vector2(550, 850);
        SizeCondition = ImGuiCond.Appearing;

        config = plugin.Configuration;
        this.plugin = plugin;
        this.launchpadHandler = launchpadHandler;
        lpGrid = config.LaunchpadGrid;
    }    

    public void Dispose() {
        launchpadHandler.Disconnect();
    }

    public override void PreDraw()
    {
        
        // Flags must be added or removed before Draw() is being called, or they won't apply        
    }

    public override void Draw()
    {
        var selectedLP = config.SelectedLaunchpadId;
        var selectedLPint = launchpadHandler.LaunchpadIDs.IndexOf(selectedLP);
        if (ImGui.Combo("Launchpad to connect to", ref selectedLPint, launchpadHandler.LaunchpadIDs.ToArray(), launchpadHandler.LaunchpadIDs.Count()))
        {
            config.SelectedLaunchpadId = launchpadHandler.LaunchpadIDs[selectedLPint];            
            config.Save();
            launchpadHandler.SelectLaunchpad();
        }

        if (!launchpadHandler.LaunchpadConnected)
            ImGui.TextUnformatted("If your launchpad is not listed, ensure it is plugged in then disable and enable this plugin.");

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
                testInProgress = true;
            }
            if (testInProgress && ImGui.Button("Stop Test (dyanmic lighting won't work while the test is running!"))
            {
                launchpadHandler.StopTest();
                testInProgress = false;
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
                            ImGui.TextUnformatted("Logo");
                            continue;
                        }
                        LaunchpadButton? lpButton = null;

                        lpGrid.FirstOrDefault(b => b.XCoord == x && b.YCoord == y);
                        if (lpButton == null)
                        {
                            lpButton = config.LaunchpadGrid.FirstOrDefault(b => b.XCoord == x && b.YCoord == y);
                        }
                        if (lpButton == null)
                        {
                            lpButton = new LaunchpadButton { XCoord = x, YCoord = y };
                            lpGrid.Add(lpButton);
                        }

                        int hotbar = 0;
                        if (lpButton.Hotbar.HasValue)
                            hotbar = lpButton.Hotbar.Value + 1;
                        ImGui.SetNextItemWidth(50);
                        if (ImGui.Combo($"###{x}{y}hotbar", ref hotbar, hotbarNumbers, hotbarNumbers.Count()))
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
                        if (ImGui.Combo($"###{x}{y}slot", ref slot, slotNumbers, slotNumbers.Count()))
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

                        if (!config.LaunchpadGrid.Any())
                        {  //first run
                            config.LaunchpadGrid = lpGrid;
                            config.Save();
                        }
                    }
                }
                ImGui.EndTable();

                var backgroundColour = config.BackgroundColour;
                if (ImGui.ColorEdit4("Unused Buttons Colour", ref backgroundColour))
                {
                    rgbint = ImGui.ColorConvertFloat4ToU32(backgroundColour);
                    //launchpad maxes out at 127 for its rgb values.
                    config.BackgroundColourRed = (int)((rgbint & 0x000000FF)/2);
                    config.BackgroundColourGreen = (int)(((rgbint & 0x0000FF00) >> 8)/2);
                    config.BackgroundColourBlue = (int)(((rgbint & 0x00FF0000) >> 48) / 2);
                    backgroundColour = ImGui.ColorConvertU32ToFloat4(rgbint);
                    config.BackgroundColour = backgroundColour;
                    config.Save();
                }

                if (ImGui.Button("Save Launchpad Mapping"))
                {
                    config.LaunchpadGrid = lpGrid;
                    config.Save();
                    launchpadHandler.InitialLightUp();
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
