using HarmonyLib;
using PierToPierPlugin;
using TMPro;
using UI.Dialogs;
using UnityEngine;
using Util;

[HarmonyPatch]
public class OSPatch
{
    [HarmonyPatch(typeof(OS), "ShowServerRules")]
    class ShowServerRulesPatch
    {
        static bool Prefix()
        {
            if (!Networking.IsSinglePlayer() && DataUtils.IsCustomServer())
            {
                var dialog = uDialog.NewDialog("ServerRulesPreferences", UnityEngine.Object.FindObjectOfType<DesktopFinder>().GetComponent<RectTransform>());
                dialog.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 700);
                TextMeshProUGUI text = dialog.transform.Find("Dialog/Container/Viewport/Content/Panel/TextMeshPro Text").GetComponent<TextMeshProUGUI>();
                text.text =
                "<b>The first rule of fight club is you do not talk about fight club.<b>\r\n\r\n" +
                "<b>The second rule of fight club is YOU DO NOT TALK ABOUT FIGHT CLUB</b>.\r\n\r\n" +
                "<b>•</b> <b>DO NOT CLUB NEW PEOPLE!</b>\n" +
                "   Kindly inform them of their flaws and help them get better.\r\n\r\n" +
                "<b>The following content may not be published in multiplayer</b>:\r\n\r\n" +
                "<b>•</b> Real personal information such as name, address, phone number, etc.\r\n\r\n" +
                "<b>•</b> Harassment, discrimination and abuse such as but not limited to racism, transphobia or threats.\r\n\r\n" +
                "<b>•</b> Religious, political or similarly controversial subject matters.\r\n\r\n" +
                "<b>•</b> Encouraging or providing instructions for violent, destructive or illegal activities.\r\n\r\n" +
                "<b>Violating these rules can lead to website removal, warning, temporary or permanent ban of your fight club account.</b>\n" +
                "<b>By participating in this server, you agree to adhere by them.</b>";
                return false;
            }
            return true;
        }
    }
}