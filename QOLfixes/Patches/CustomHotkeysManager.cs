using HarmonyLib;
using SandBox.View.Map;
using SandBox.View.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace QOLfixes
{
    [HarmonyPatch]
    static class CustomHotkeysManager
    {
        public static void HandleHotkeyPresses(MapScreen instance, MenuViewContext viewContext)
        {
            InputContext inputCtx = instance.SceneLayer.Input;

            if (ConfigFileManager.configs.waypoints)
                WaypointManager.HandleHotkeyPresses(instance, viewContext);
           
            //Restore functionality to change time speed through hotkeys on game menu
            if (!ConfigFileManager.configs.autoPauseOnEnterSettlement)
                AutoPauseManager.HandleHotkeyPresses(instance, viewContext);

            //Change fast forward speed through hotkeys.
            if (inputCtx.IsHotKeyPressed(CustomMapHotkeyCategory.IncreaseFFSpeedKeyName))
            {
                Campaign.Current.SpeedUpMultiplier += 0.5f;
                InformationManager.DisplayMessage(new InformationMessage("Fast Forward Speed set to: " + Campaign.Current.SpeedUpMultiplier.ToString()));
            }
            else if (inputCtx.IsHotKeyPressed(CustomMapHotkeyCategory.DecreaseFFSpeedKeyName))
            {
                if (Campaign.Current.SpeedUpMultiplier > 4.0f)
                    Campaign.Current.SpeedUpMultiplier -= 0.5f;
                InformationManager.DisplayMessage(new InformationMessage("Fast Forward Speed set to: " + Campaign.Current.SpeedUpMultiplier.ToString()));
            }
        }

        /* Register our custom hotkey category for hotkeys to work
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapScreen), "OnInitialize")]
        public static void PatchOnInitialize(ref MapScreen __instance)
        {
            __instance.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("CustomMapHotkeyCategory"));
        }

        /* Transpiler to check for hotkey presses in FrameTick function
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapScreen), "OnFrameTick")]
        public static IEnumerable<CodeInstruction> PatchOnFrameTick(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo HandleHotkeyPressesMI = SymbolExtensions.GetMethodInfo(() => CustomHotkeysManager.HandleHotkeyPresses(null, null));

            //Add a call to our own method which manages hotkey presses
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MapScreen), "_menuViewContext"));
            yield return new CodeInstruction(OpCodes.Call, HandleHotkeyPressesMI);

            foreach (var instruc in instructions)
            {
                yield return instruc;
            }
        }
    }
}
