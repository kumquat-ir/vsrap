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
    }

    private static void sendCheck(long check) {
        if (!APSession.currentSave.checkedLocations.Contains(check)) {
            APSession.currentSave.checkedLocations.Add(check);
            // TODO make sure this doesnt die when we are disconnected
            APSession.session.Locations.CompleteLocationChecks(check);

            if (APSession.uncollectedLocationInfo != null) {
                ScoutedItemInfo itemHere = APSession.uncollectedLocationInfo[check];
                if (itemHere.Player.Slot != APSession.session.ConnectionInfo.Slot) {
                    Notifications.queueNotification($"Sent {itemHere.ItemDisplayName} to {itemHere.Player} from {itemHere.LocationDisplayName}");
                }
            }
        }
    }

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

    public static void externalCollectLocation(long id) {
        // this is from a !collect or /send_location, the item has already been sent
        APSession.currentSave.checkedLocations.Add(id);
    }

    public static bool decryptorChecked(Decryptor.ID decryptor) {
        return APSession.currentSave.checkedLocations.Contains(Data.DECRYPTOR_LOCATIONS[decryptor]);
    }

    public static bool cardChecked(int card) {
        return APSession.currentSave.checkedLocations.Contains(Data.CARD_LOCATIONS[card]);
    }

    [HarmonyPatch(typeof(DecryptorPickup), "OnTriggerEnter2D")]
    [HarmonyPatch(typeof(DecryptorPickup), "OnRevertExist")]
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
}
