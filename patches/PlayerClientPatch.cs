using PierToPierPlugin;
using HarmonyLib;
using Util;
using System.Collections;
using System.Reflection.Emit;
using System.Collections.Generic;
using static Util.Networking;

[HarmonyPatch]
public class PlayerClientPatch
{
    [HarmonyPatch(typeof(PlayerClient), "ConnectToServer")]
    class ConnectToServerPatch
    {
        static void Prefix(PlayerClient __instance)
        {
            if (!Networking.IsSinglePlayer() && !string.IsNullOrEmpty(DataUtils.server))
            {
                if (__instance.serverMode == PlayerClient.ServerMode.PUBLIC)
                    __instance.publicAddress = DataUtils.server;
                else
                    __instance.nightlyAddress = DataUtils.server;
            }
            if (Networking.IsSinglePlayer() && DataUtils.hosting)
            {
                DataUtils.port = DataUtils.hostPort;
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4 && (int)codes[i].operand == 21200)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(DataUtils), "port"));
                }
            }
            return codes;
        }
    }
}