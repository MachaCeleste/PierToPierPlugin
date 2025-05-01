using HarmonyLib;
using PierToPierPlugin;
using System.Reflection;
using Util;

[HarmonyPatch]
public class HelperServerPatch
{
    [HarmonyPatch(typeof(HelperServer), "IsGameAdmin")]
    class IsGameAdminPatch
    {
        static bool Prefix(HelperServer __instance, ref Roles minRole, ref bool ignoreDisabled, ref bool __result)
        {
            if (!DataUtils.hosting) return true;
            FieldInfo fieldInfo = AccessTools.Field(typeof(HelperServer), "player");
            var player = fieldInfo.GetValue(__instance) as PlayerServer;
            if (DataUtils.GetRole(player.playerID) >= minRole)
            {
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
    }
}