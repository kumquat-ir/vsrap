using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

namespace vsrap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[HarmonyPatch]
public class VSRAP : BaseUnityPlugin {
    internal static ManualLogSource logger;
    public static ConfigEntry<bool> enableDebugScreen;

    private void Awake() {
        logger = base.Logger;
        logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        enableDebugScreen = Config.Bind("General",
                                        "EnableDebugScreen",
                                        false,
                                        "Enable the in-game debug menu. When enabled, End toggles the menu, Page Up/Down navigate, and Numpad * performs actions.");
        Vars.debugScreen = enableDebugScreen.Value;
        enableDebugScreen.SettingChanged += setDebugScreen;
    }

    private static void setDebugScreen(object sender, EventArgs args) {
        Vars.debugScreen = ((ConfigEntry<bool>) ((SettingChangedEventArgs) args).ChangedSetting).Value;
    }
}
