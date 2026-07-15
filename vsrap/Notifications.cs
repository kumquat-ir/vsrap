using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace vsrap;

[HarmonyPatch]
public class Notifications {
    private static List<string> queue = new();

    public static void queueNotification(string notif) {
        queue.Add(notif);
    }

    public static void clearQueue() {
        queue.Clear();
    }

    [HarmonyPatch(typeof(HUD), "Awake")]
    [HarmonyPostfix]
    static void queuePendingNotifications() {
        Notification.instance.maxQueueSize = 9999;
        foreach (string notif in queue) {
            Notification.instance.displayNotification(notif);
        }
        clearQueue();
    }

    [HarmonyPatch(typeof(Notification), "Update")]
    [HarmonyPostfix]
    static void queueNotificationsSafely() {
        foreach (string notif in queue) {
            Notification.instance.displayNotification(notif);
        }
        clearQueue();
    }

    [HarmonyPatch(typeof(HUD), nameof(HUD.onUnloadLevel))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> doNotClearNotifs(IEnumerable<CodeInstruction> insns) {
        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Notification), nameof(Notification.clearAll))))
            .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Pop))
            .InstructionEnumeration();
    }
}
