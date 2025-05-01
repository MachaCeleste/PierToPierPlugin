using HarmonyLib;
using PierToPierPlugin;
using Util;

[HarmonyPatch]
public class ClockPatch
{
    [HarmonyPatch(typeof(Clock), "ProcessTime")]
    class ProcessTimePatch
    {
        private static int currentMonth = -1;
        static void Prefix(ref Clock __instance)
        {
            if (!Networking.IsSinglePlayer()) return;
            int month = __instance.GetMes();
            if (currentMonth == -1)
                currentMonth = month;
            else if (currentMonth != month)
            {
                RedGlobal.Singleton?.GetHardwareItems().UpgradeHardware();
                currentMonth = month;
                DataUtils.webHookHandler?.SendEmbedAsync("Server Info", "In game shops have been updated!");
            }
        }
    }
}