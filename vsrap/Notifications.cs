using System.Collections.Generic;
using HarmonyLib;

namespace vsrap;

[HarmonyPatch]
public class Notifications {
    private static List<string> queue = new();
    public static bool canSendNotifications {
        get {
            return Notification.instance != null;
        }
    }

    public static void queueNotification(string notif) {
        if (canSendNotifications) {
            Notification.instance.displayNotification(notif);
        }
        else {
            queue.Add(notif);
        }
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
}
