using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

namespace vsrap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[HarmonyPatch]
public class VSRAP : BaseUnityPlugin {
    internal static ManualLogSource logger;

    private void Awake() {
        logger = base.Logger;
        logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Vars.debugScreen = true;
    }

    [HarmonyPatch(typeof(Vars), nameof(Vars.loadData))]
    [HarmonyPostfix]
    static void patchFileLoad(bool __result) {
        if (!__result) {
            return;
        }

        logger.LogInfo("Loaded a file!");
        logger.LogInfo("Decryptors:");
        foreach (Decryptor.ID dec in Vars.decryptors) {
            logger.LogInfo($"  {dec} ({Decryptor.getCode(dec)})");
        }
        logger.LogInfo("Cards:");
        foreach (int card in Vars.creatureCardsFound) {
            logger.LogInfo($"  {CreatureCard.getCardNameFromID(card)} ({card})");
        }
    }

    [HarmonyPatch(typeof(Vars), nameof(Vars.collectDecryptor))]
    [HarmonyPrefix]
    static void patchCollectDecryptor(Decryptor.ID decryptor) {
        logger.LogInfo($"Collect decryptor {decryptor}");
    }

    [HarmonyPatch(typeof(Vars), nameof(Vars.creatureCardFind), new Type[] { typeof(int) })]
    [HarmonyPrefix]
    static void patchCollectCard(int creatureID) {
        logger.LogInfo($"Collect card {CreatureCard.getCardNameFromID(creatureID)} ({creatureID})");
    }
}
