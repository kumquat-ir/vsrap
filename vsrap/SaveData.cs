using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;

namespace vsrap;

public class APConnectionData {
    public string address = "";
    public int port;
    public string slot = "";
    public string password = "";
}

public class APSaveData {
    public APConnectionData connection = new();
    public ISet<long> checkedLocations = new HashSet<long>();
    public ISet<long> receivedItems = new HashSet<long>();
}

[HarmonyPatch]
public class SaveDataPatches {
    private static string sentinel = "-- Archipelago Data --";
    public static string loadError = null;

    [HarmonyPatch(typeof(Vars), "saveDataToString")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> patchSaveData(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .End()
            .MatchBack(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(StringBuilder), nameof(StringBuilder.AppendLine), new Type[] { })),
                new CodeMatch(OpCodes.Pop),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Callvirt)
            )
            .Advance(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SaveDataPatches), nameof(SaveDataPatches.addSaveData)))
            ).InstructionEnumeration();
    }

    [HarmonyPatch(typeof(Vars), "loadDataFromString")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> patchLoadData(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .End()
            .MatchBack(false, new CodeMatch(OpCodes.Ret))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SaveDataPatches), nameof(SaveDataPatches.readLoadedData)))
            ).InstructionEnumeration();
    }

    [HarmonyPatch(typeof(FileSelectScreen), "beginFile")]
    [HarmonyPatch(typeof(FileSelectScreen), "beginNewFile")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> patchFileLoadErrors(IEnumerable<CodeInstruction> insns, ILGenerator generator) {
        Label retLabel = generator.DefineLabel();

        return new CodeMatcher(insns)
            .End()
            .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FileSelectScreen), "whiteScreenTransition")))
            .Insert(
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SaveDataPatches), nameof(SaveDataPatches.handleLoadError))),
                new CodeInstruction(OpCodes.Brfalse_S, retLabel),
                new CodeInstruction(OpCodes.Ldarg_0)
            ).MatchForward(false, new CodeMatch(OpCodes.Ret))
            .AddLabels([retLabel])
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(Vars), nameof(Vars.goToTitleScreen))]
    [HarmonyPostfix]
    static void patchReturnToTitle() {
        APSession.cleanupSession();
        Notifications.clearQueue();
    }

    [HarmonyPatch(typeof(Vars), "loadDefaultData")]
    [HarmonyPostfix]
    static void patchNewFile() {
        APSaveData save = new();
        // TODO unhardcode
        save.connection.address = "localhost:38281";
        save.connection.slot = "test";
        save.connection.password = "";
        APSession.currentSave = save;
        APSession.connect();
    }

    static StringBuilder addSaveData(StringBuilder builder) {
        builder.AppendLine(sentinel);

        APSaveData save = APSession.currentSave;
        APConnectionData conn = save.connection;
        builder.AppendLine($"{conn.address},{conn.port},{conn.slot},{conn.password}");
        builder.AppendLine(String.Join(",", save.checkedLocations));
        builder.AppendLine(String.Join(",", save.receivedItems));

        return builder;
    }

    static void readLoadedData(string[] data) {
        int baseIndex = 22;
        if (data.Length <= baseIndex + 3 || data[baseIndex] != sentinel) {
            loadError = "Cannot load vanilla saves!";
            return;
        }

        APSaveData save = new();

        string[] connectionInfo = data[baseIndex + 1].Split(',');
        save.connection.address = connectionInfo[0];
        save.connection.port = int.Parse(connectionInfo[1]);
        save.connection.slot = connectionInfo[2];
        save.connection.password = connectionInfo[3];

        save.checkedLocations = new HashSet<long>(data[baseIndex + 2].Split(',').Where(id => !String.IsNullOrWhiteSpace(id)).Select(id => long.Parse(id)));
        save.receivedItems = new HashSet<long>(data[baseIndex + 3].Split(',').Where(id => !String.IsNullOrWhiteSpace(id)).Select(id => long.Parse(id)));

        APSession.currentSave = save;
        APSession.connect();
    }

    static bool handleLoadError(FileSelectScreen screen) {
        if (loadError != null) {
            screen.showError(loadError);
            loadError = null;
            return false;
        }

        loadError = null;
        return true;
    }
}
