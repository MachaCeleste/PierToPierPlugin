using PierToPierPlugin;
using HarmonyLib;
using System;
using UnityEngine;
using Util;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

[HarmonyPatch]
public class RedGlobalPatch
{
    //[HarmonyPatch(typeof(RedGlobal), "Inicializar")]
    //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //{
    //    var codes = new List<CodeInstruction>(instructions);
    //    MethodInfo bankSubsConfig = AccessTools.Method(typeof(BankSubs), "Config");
    //    ConstructorInfo globalChatCtor = AccessTools.Constructor(typeof(GlobalChat));
    //    int insertionIndex = -1;
    //    for (int i = 0; i < codes.Count; i++)
    //    {
    //        if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo mi && mi == bankSubsConfig)
    //        {
    //            insertionIndex = i;
    //            break;
    //        }
    //    }
    //    var newInstructions = new List<CodeInstruction>
    //    {
    //        new CodeInstruction(OpCodes.Newobj, globalChatCtor),
    //        new CodeInstruction(OpCodes.Pop)
    //    };
    //    codes.InsertRange(insertionIndex + 1, newInstructions);
    //    return codes;
    //} // Thanks for breaking chat kuro :P

    [HarmonyPatch(typeof(RedGlobal), "OnNetworkSpawn")]
    class OnNetworkSpawnPatch
    {
        static void Postfix()
        {
            if (DataUtils.hosting == true)
            {
                DataUtils.LoadDatabase();
                if (!string.IsNullOrEmpty(DataUtils.database.webHookUrl))
                {
                    DataUtils.webHookHandler = new WebhookHandler(DataUtils.database.webHookUrl);
                    DataUtils.webHookHandler?.SendEmbedAsync("SERVER STATUS", $"Server Online! P2P: v{MyPluginInfo.PLUGIN_VERSION} Game: v{DataUtils.GetGameVersion()}");
                }
                if (!string.IsNullOrEmpty(DataUtils.database.adminWebHookUrl))
                {
                    DataUtils.adminWebHookHandler = new WebhookHandler(DataUtils.database.adminWebHookUrl);
                    DataUtils.adminWebHookHandler?.SendEmbedAsync("SERVER STATUS", $"Server Online! P2P: v{MyPluginInfo.PLUGIN_VERSION} Game: v{DataUtils.GetGameVersion()}");
                }
                //if (!string.IsNullOrEmpty(DataUtils.database.chatWebHookUrl))
                //{
                //    DataUtils.chatWebHookHandler = new WebhookHandler(DataUtils.database.chatWebHookUrl);
                //    DataUtils.chatWebHookHandler?.SendEmbedAsync("CHAT LOG STATUS", "Chat Logging active!");
                //}
            }
        }
    }
    [HarmonyPatch(typeof(RedGlobal), "PregenBasicNetworks")]
    class PregenBasicNetworksPatch
    {
        static bool Prefix()
        {
            if (!Networking.IsSinglePlayer()) return true;
            try
            {
                ServerMap.TipoRed[] array = new ServerMap.TipoRed[]
                {
                ServerMap.TipoRed.TiendaInformatica,
                ServerMap.TipoRed.Bancos,
                ServerMap.TipoRed.MailServices,
                ServerMap.TipoRed.Comisaria,
                ServerMap.TipoRed.NetServices,
                ServerMap.TipoRed.CurrencyCreation
                };
                for (int i = 0; i < array.Length; i++)
                {
                    ServerMap.Singleton.SpawnRouter(array[i], 10, false);
                }
                Debug.Log("Basic Networks generated OK");
            }
            catch (Exception message)
            {
                Debug.LogError(message);
            }
            return false;
        }
    }
}