using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Archipelago.MultiClient.Net.Models;
using HarmonyLib;

namespace vsrap;

[HarmonyPatch]
public class CheckHandler {
    public static void getItem(long id) {
        APSession.currentSave.receivedItems.Add(id);

        if (Data.DECRYPTOR_ITEMS.ContainsKey(id)) {
            Vars.collectDecryptor(Data.DECRYPTOR_ITEMS[id]);
        }
        else if (Data.CARD_ITEMS.ContainsKey(id)) {
            Vars.creatureCardFind(Data.CARD_ITEMS[id]);
        }
        else if (Data.PHASE_REFILLS.ContainsKey(id)) {
            if (!APSession.currentSave.recievedMultiples.ContainsKey(id)) {
                APSession.currentSave.recievedMultiples[id] = 0;
            }
            if (!APSession.sessionRecievedMultiples.ContainsKey(id)) {
                APSession.sessionRecievedMultiples[id] = 1;
            }
            APSession.currentSave.recievedMultiples[id]++;

            if (Player.instance != null) {
                Player.instance.phasePickup(Data.PHASE_REFILLS[id]);
            }
        }
        else {
            VSRAP.logger.LogWarning($"Recieved unknown item {id}!");
        }
    }

    private static void sendCheck(long check) {
        if (!APSession.currentSave.checkedLocations.Contains(check) && APSession.session.Locations.AllLocations.Contains(check)) {
            APSession.currentSave.checkedLocations.Add(check);
            APSession.session.Locations.CompleteLocationChecks(check);

            if (APSession.uncollectedLocationInfo != null) {
                ScoutedItemInfo itemHere = APSession.uncollectedLocationInfo[check];
                if (itemHere.Player.Slot != APSession.session.ConnectionInfo.Slot) {
                    Notifications.queueNotification($"Sent {itemHere.ItemDisplayName} to {itemHere.Player} from {itemHere.LocationDisplayName}");
                }
            }
        }
    }

    public static void externalCollectLocation(long id) {
        // this is from a !collect or /send_location, the item has already been sent
        APSession.currentSave.checkedLocations.Add(id);
    }


    // -- check sending patch targets --

    public static void collectDecryptor(Decryptor.ID decryptor) {
        if (!Data.DECRYPTOR_LOCATIONS.ContainsKey(decryptor)) {
            VSRAP.logger.LogError($"No location ID for decryptor {decryptor}!");
            return;
        }
        sendCheck(Data.DECRYPTOR_LOCATIONS[decryptor]);
    }

    public static void collectCard(int card) {
        if (!Data.CARD_LOCATIONS.ContainsKey(card)) {
            VSRAP.logger.LogError($"No location ID for card number {card}!");
            return;
        }
        sendCheck(Data.CARD_LOCATIONS[card]);
    }

    // -- simple check sending patches --

    [HarmonyPatch(typeof(NodeData), nameof(NodeData.defeatAmbush), new Type[] { typeof(int), typeof(int) })]
    [HarmonyPostfix]
    public static void clearAmbush(int gridX, int gridY) {
        (int, int) pos = (gridX, gridY);
        if (!Data.AMBUSH_LOCATIONS.ContainsKey(pos)) {
            VSRAP.logger.LogError($"No location ID for ambush at {pos}!");
            return;
        }
        sendCheck(Data.AMBUSH_LOCATIONS[pos]);
    }

    [HarmonyPatch(typeof(NodeData), nameof(NodeData.healthUpgradeCollect))]
    [HarmonyPostfix]
    public static void collectHealthUpgrade(PhysicalUpgrade.HealthUpgrade healthUpgrade) {
        if (!Data.HEALTH_UPGRADE_LOCATIONS.ContainsKey(healthUpgrade)) {
            VSRAP.logger.LogError($"No location ID for health upgrade {healthUpgrade}!");
            return;
        }
        sendCheck(Data.HEALTH_UPGRADE_LOCATIONS[healthUpgrade]);
    }

    [HarmonyPatch(typeof(NodeData), nameof(NodeData.phaseUpgradeCollect))]
    [HarmonyPostfix]
    public static void collectPhaseUpgrade(PhysicalUpgrade.PhaseUpgrade phaseUpgrade) {
        if (!Data.PHASE_UPGRADE_LOCATIONS.ContainsKey(phaseUpgrade)) {
            VSRAP.logger.LogError($"No location ID for phase upgrade {phaseUpgrade}!");
            return;
        }
        sendCheck(Data.PHASE_UPGRADE_LOCATIONS[phaseUpgrade]);
    }

    [HarmonyPatch(typeof(NodeData), nameof(NodeData.orbCollect))]
    [HarmonyPostfix]
    public static void collectOrb(PhysicalUpgrade.Orb orb) {
        if (!Data.ORB_LOCATIONS.ContainsKey(orb)) {
            VSRAP.logger.LogError($"No location ID for orb {orb}!");
            return;
        }
        sendCheck(Data.ORB_LOCATIONS[orb]);
    }

    // -- checked location redirection patch targets --

    public static bool decryptorChecked(Decryptor.ID decryptor) {
        return APSession.currentSave.checkedLocations.Contains(Data.DECRYPTOR_LOCATIONS[decryptor]);
    }

    public static bool cardChecked(int card) {
        return APSession.currentSave.checkedLocations.Contains(Data.CARD_LOCATIONS[card]);
    }

    // -- decryptor patches --

    [HarmonyPatch(typeof(DecryptorPickup), "OnTriggerEnter2D")]
    [HarmonyPatch(typeof(DecryptorPickup), "OnRevertExist")]
    [HarmonyPatch(typeof(GlitchFight), "BossStart")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> redirectToAPDecryptorChecked(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.abilityKnown))))
            .Repeat(match => match.SetOperandAndAdvance(AccessTools.Method(typeof(CheckHandler), nameof(CheckHandler.decryptorChecked))))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(DecryptorPickup), "Start")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> redirectToAPDecryptorChecked2(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(true, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.abilityKnown)))) // discard first match (check for golden view)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.abilityKnown))))
            .Repeat(match => match.SetOperandAndAdvance(AccessTools.Method(typeof(CheckHandler), nameof(CheckHandler.decryptorChecked))))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(DecryptorAnimation), "setPositions")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> redirectToAPCollectDecryptor(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(DecryptorText), nameof(DecryptorText.display))))
            .RemoveInstruction()
            .InsertAndAdvance(new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Pop))
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.collectDecryptor))))
            .SetOperandAndAdvance(AccessTools.Method(typeof(CheckHandler), nameof(CheckHandler.collectDecryptor)))
            .InstructionEnumeration();
    }

    // -- glitch fight related decryptor patches --

    public static void collectGlitchFightDecryptors() {
        collectDecryptor(Decryptor.ID.VIRUS_WIPE);
        collectDecryptor(Decryptor.ID.STRIP_SUIT);
    }

    public static bool shouldSkipSalesmanEncounter(Decryptor.ID canaryDecryptor) {
        return decryptorChecked(canaryDecryptor) || !Vars.currentNodeData.eventHappened(AdventureEvent.Physical.GLITCH_DEFEAT_CUTSCENE_START);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.enterShellSuit))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> removeSpecialDecryptorCollection(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.collectDecryptor))))
            .Repeat(match => match.SetOpcodeAndAdvance(OpCodes.Pop))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(SalesmanWarehouseEncounterStart), "Start")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> fixupStartConditions(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.abilityKnown))))
            .SetOperandAndAdvance(AccessTools.Method(typeof(CheckHandler), nameof(CheckHandler.shouldSkipSalesmanEncounter)))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(SalesmanWarehouseEncounterStart), "Update")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> collectDecryptorsOnCutscene(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ScriptRunner), nameof(ScriptRunner.runScript))))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CheckHandler), nameof(CheckHandler.collectGlitchFightDecryptors))))
            .InstructionEnumeration();
    }

    // -- creature card patches --

    [HarmonyPatch(typeof(CreatureCardPickup), "Start")]
    [HarmonyPatch(typeof(CreatureCardPickup), "OnRevert")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> redirectToAPCardChecked(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.creatureCardFound), new Type[] { typeof(int) })))
            .Repeat(match => match.SetOperandAndAdvance(AccessTools.Method(typeof(CheckHandler), nameof(CheckHandler.cardChecked))))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(CreatureCardPickup), nameof(CreatureCardPickup.state), MethodType.Setter)]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> redirectToAPCollectCard(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.creatureCardFind), new Type[] { typeof(int) })))
            .SetOperandAndAdvance(AccessTools.Method(typeof(CheckHandler), nameof(CheckHandler.collectCard)))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(NodeData), nameof(NodeData.creatureCardCollect), new Type[] { typeof(int) })]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> removeRedundantCollect(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Vars), nameof(Vars.creatureCardFind), new Type[] { typeof(int) })))
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Pop))
            .InstructionEnumeration();
    }

    // -- goal patches --

    [HarmonyPatch(typeof(Salesman.Salesman), nameof(Salesman.Salesman.DefeatExamineEnd))]
    [HarmonyPrefix]
    static void completeGoal() {
        Notifications.queueNotification("Completed the goal!");
        APSession.session.SetGoalAchieved();
    }
}
