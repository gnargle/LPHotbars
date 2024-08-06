using Dalamud.IoC;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using LaunchpadNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static LaunchpadNET.Interface;

namespace LaunchpadHotbars
{
    public unsafe class LaunchpadHandler
    {
        [PluginService] internal static IFramework framework { get; private set; } = null!;
        public List<String> LaunchpadIDs { get; set; }       

        private Interface.LaunchpadDevice[] lps;
        private Interface.LaunchpadDevice? launchpad;
        private Interface lpIface;
        private RaptureHotbarModule* hotbarModule;
        private Action<string> logAction;
        private LaunchpadHotbarsPlugin plugin;
        private Configuration config;
        public LaunchpadHandler(LaunchpadHotbarsPlugin plugin, Configuration config) {
            this.plugin = plugin;
            this.config = config;

            logAction = delegate (string name)
            {
                this.plugin.ChatError(name);
            };

            lpIface = new Interface(logAction, logAction, logAction);
            ListLaunchpads();
            lpIface.OnLaunchpadKeyDown += KeyPressed;
            lpIface.OnLaunchpadCCKeyDown += CCKeyPressed;
            hotbarModule = RaptureHotbarModule.Instance();
        }

        public bool LaunchpadReady
        {
            get =>
                launchpad != null;
        }

        public bool LaunchpadConnected
        {
            get => lpIface.Connected;
        }

        public List<string> ListLaunchpads()
        {
            lps = Interface.getConnectedLaunchpads(logAction);
            LaunchpadIDs = new List<string>() { "" }.Union(lps.Where(l => l._isLegacy).Select(l => l._midiName)).Union(lps.Where(p => !p._isLegacy).Select(p => p._midiIn)).ToList();
            return LaunchpadIDs;
        }

        public bool SelectLaunchpad()
        {
            var found = false;
            try
            {
                if (lps == null || !lps.Any())
                    ListLaunchpads();
                foreach (var lp in lps)
                {
                    if (lp._isLegacy && lp._midiName == config.SelectedLaunchpadId)
                    {
                        launchpad = lp;
                        found = true;
                        break;

                    }
                    else if (lp._midiIn == config.SelectedLaunchpadId)
                    {
                        launchpad = lp;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    logAction("Selected launchpad could not be found in the devices list.");
                }
            }
            catch (Exception ex)
            {
                logAction("Error finding launchpad: " + ex.Message);
            }
            return found;
        }

        public bool ConnectLaunchpad()
        {
            if (config.SelectedLaunchpadId != null)
            {
                if (!lps.Any())
                    ListLaunchpads();
                if (launchpad == null)
                {
                    if (!SelectLaunchpad())
                    {
                        return false;
                    }
                }
                if (LaunchpadIDs.Contains(config.SelectedLaunchpadId))
                {
                    try
                    {
                        var connected = lpIface.connect(launchpad);
                        if (!connected)
                        {
                            logAction("Launchpad could not be connected to.");
                            return false;
                        }
                        else
                        {
                            lpIface.SetMode(LaunchpadMode.Programmer);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        logAction("Launchpad could nto be connected to with error: " + ex.Message);
                    }

                }
            }
            else
            {
                logAction("No launchpad selected.");
            }
            return false;
        }

        public void Disconnect()
        {
            if (LaunchpadReady)
            {
                lpIface.disconnect(launchpad);
                lpIface.Connected = false;
            }
        }

        public void TestConnection()
        {
            try
            {
                if (lpIface.IsLegacy)
                {
                    lpIface.createTextScroll("FINAL FANTASY XIV", 10, true, 125);
                }
                else
                {
                    lpIface.createTextScrollMiniMk3RGB("FINAL FANTASY XIV", 10, true, 0xFF, 0x00, 0xFB);
                }
            }
            catch (Exception ex)
            {
                logAction(ex.Message);
            }
        }

        private Action<uint, uint> executeAction = delegate (uint hotbarId, uint slotId)
        {
            RaptureHotbarModule.Instance()->ExecuteSlotById(hotbarId, slotId);
        };

        private void HandleAnyKeyPress(LaunchpadButton lpButton)
        {
            if (lpButton == null || !lpButton.Hotbar.HasValue || !lpButton.Slot.HasValue)
            {
                logAction("launchpad button not configured.");
                return;
            }
            
            if (hotbarModule != null)
            {
                if (hotbarModule->Hotbars != null)
                {
                    var hotbar = hotbarModule->Hotbars[1];
                    if (hotbar.Slots != null)
                    {
                        var slot = hotbar.GetHotbarSlot(0);
                        if (slot != null)
                        {
                            logAction("slot is not null");
                            //this doesn't work from here, for whatever reason. So we're gonna have to be hacky instead.
                            /*framework.RunOnTick(() =>
                            {
                                RaptureHotbarModule.Instance()->ExecuteSlotById(1, 0);
                            }); */

                            plugin.HotbarToExecute = lpButton.Hotbar;
                            plugin.SlotToExecute = lpButton.Slot;
                            logAction("hotbar and slot set");

                        }
                        else
                            logAction("slot is null");
                    }
                    else
                        logAction("hotbar Slots is null");
                }
                else
                    logAction("hotbars is null.");
            }
            else
                logAction("hotbar module is null.");
        }

        public void KeyPressed(object source, LaunchpadKeyEventArgs e)
        {
            logAction($"launchpad button x:{e.GetX()}, y:{e.GetY()} fired");
            var lpButton = config.LaunchpadGrid.FirstOrDefault(b => b.XCoord == e.GetX() && b.YCoord == e.GetY());
            HandleAnyKeyPress(lpButton);
        }

        public void CCKeyPressed(object source, LaunchpadCCKeyEventArgs e)
        {
            var lpButton = config.LaunchpadGrid.FirstOrDefault(b => b.CCVal == e.GetVal());
            HandleAnyKeyPress(lpButton);
            logAction($"CC key pressed {e.GetVal()}");
        }

        ~LaunchpadHandler()
        {
            if (lpIface.Connected)
            {
                Disconnect();
            }
        }
    }  
}
