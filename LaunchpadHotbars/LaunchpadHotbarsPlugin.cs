using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using LaunchpadHotbars.Windows;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Linq;

namespace LaunchpadHotbars;

public unsafe sealed class LaunchpadHotbarsPlugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    private const string CommandName = "/lphb";

    private ActionManager* actionManager;
    private RaptureHotbarModule* hotbarModule;

    private ActionManager* ActionMgr
    {
        get
        {
            if (actionManager == null)
            {
                if (ActionManager.Instance() != null)
                {
                    actionManager = ActionManager.Instance();
                }
            }
            return actionManager;
        }
    }

    private RaptureHotbarModule* HotbarModule
    {
        get
        {
            if (hotbarModule == null)
            {
                if (RaptureHotbarModule.Instance() != null)
                {
                    hotbarModule = RaptureHotbarModule.Instance();
                }
            }
            return hotbarModule;
        }
    }

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("LaunchpadHotbars");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private LaunchpadHandler launchpadHandler { get; init; }

    public int? HotbarToExecute { get; set; } = null;
    public uint? SlotToExecute { get; set; } = null;

    public LaunchpadHotbarsPlugin(IDalamudPluginInterface pi)
    {
        DalamudApi.Initialize(pi);
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        launchpadHandler = new LaunchpadHandler(this, Configuration);

        ConfigWindow = new ConfigWindow(this, launchpadHandler);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the configuration window for LaunchpadHotbars."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void ChatError(string text)
    {
        if (Configuration.ShowDebugMessages)
        {
            DalamudApi.PluginLog.Error(text);
            DalamudApi.ChatGui.Print(new XivChatEntry
            {
                Message = text,
                Type = XivChatType.Say,
            });
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleConfigUI();
    }

    private void CastAction()
    {
        if (HotbarToExecute != null && SlotToExecute != null)
        {
            ChatError("hotbar id: " + HotbarToExecute + " slot id: " + SlotToExecute);
            if (HotbarModule != null)
            {
                if (HotbarModule->Hotbars != null)
                {
                    var hotbar = HotbarModule->Hotbars[HotbarToExecute.Value];
                    if (hotbar.Slots != null)
                    {
                        var slot = hotbar.GetHotbarSlot(SlotToExecute.Value);
                        if (slot != null)
                        {
                            ChatError("firing slot");
                            HotbarModule->ExecuteSlot(slot);
                        }
                    }
                }
            }            
            HotbarToExecute = null;
            SlotToExecute = null;
        }
    }

    private void CheckRecasts()
    {
        var cooldowns = Configuration.LaunchpadGrid.Where(b => b.OnCooldown && b.Hotbar.HasValue && b.Slot.HasValue);
        foreach (var cd in cooldowns) {
            var slot = HotbarModule->GetSlotById((uint)cd.Hotbar.Value, cd.Slot.Value);
            if (slot != null)
            {
                var aid = slot->ApparentActionId;

                var recastTime = ActionMgr->GetRecastTime(ActionType.Action, aid);
                var recastElapsed = ActionMgr->GetRecastTimeElapsed(ActionType.Action, aid);
                var pct = recastElapsed / recastTime;
                launchpadHandler.ManageCooldown(cd, pct);
            }
        }        
    }

    private void DrawUI() { 
        WindowSystem.Draw();
        CastAction();
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
