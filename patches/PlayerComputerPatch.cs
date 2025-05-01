using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

[HarmonyPatch]
public class PlayerComputerPatch
{
    [HarmonyPatch(typeof(PlayerComputer), "ResumeFirstInstall")]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        int startIndex = -1;
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo mi &&
                mi.Name == "IsSinglePlayer")
            {
                startIndex = i;
                break;
            }
        }
        CodeInstruction branchInstr = codes[startIndex + 1];
        var branchTarget = branchInstr.operand;
        int endIndex = codes.FindIndex(ci => ci.labels.Contains((Label)branchTarget));
        if (endIndex == -1)
        {
            endIndex = startIndex + 6;
        }
        int countToRemove = endIndex - startIndex;
        codes.RemoveRange(startIndex, countToRemove);
        return codes;
    }
}