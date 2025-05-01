using HarmonyLib;
using PierToPierPlugin;

[HarmonyPatch]
public class IconBarChatPatch
{
    [HarmonyPatch(typeof(IconBarChat), "Start")]
    class StartPatch
    {
        static void Postfix(ref IconBarChat __instance)
        {
            if (DataUtils.hosting == true)
                __instance.gameObject.SetActive(true);
        }
    }
}