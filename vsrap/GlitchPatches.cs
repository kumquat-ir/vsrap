using System.Collections.Generic;
using HarmonyLib;

namespace vsrap;

[HarmonyPatch]
public class GlitchPatches {
    private static ISet<string> allowedGlitchRooms = new HashSet<string> {
        "glitch_arena"
    };
    public static bool noGlitches {
        get {
            return !allowedGlitchRooms.Contains(Vars.currentLevel);
        }
    }

    [HarmonyPatch(typeof(GlitchObject), nameof(GlitchObject.glitchesVisible), MethodType.Getter)]
    [HarmonyPrefix]
    static bool disableMostGlitches(ref bool __result) {
        if (noGlitches) {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(GlitchEncounterTrigger), "OnTriggerStay2D")]
    [HarmonyPrefix]
    static bool disableGlitchEncounter() {
        return false;
    }

    [HarmonyPatch(typeof(GlitchDecryptorAttack), "Start")]
    [HarmonyPrefix]
    static bool disableGlitchAttackPortal(GlitchDecryptorAttack __instance) {
        AccessTools.Method(typeof(GlitchDecryptorAttack), "hide").Invoke(__instance, new object[] { });
        return false;
    }

    [HarmonyPatch(typeof(GlitchFight), nameof(GlitchFight.BossStart))]
    [HarmonyPrefix]
    static bool disableGlitchFightWithoutVirus() {
        return Vars.abilityKnown(Decryptor.ID.VIRUS);
    }
}
