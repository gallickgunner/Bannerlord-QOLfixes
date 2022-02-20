using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using TaleWorlds.CampaignSystem;
using SandBox.View.Map;

namespace QOLfixes
{
    [HarmonyPatch]
    public static class MaintainFastForward
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapScreen), nameof(MapScreen.HandleMouse))]
        public static IEnumerable<CodeInstruction> PatchHandleMouse(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo SetTimeControlModeMI = AccessTools.PropertySetter(typeof(Campaign), nameof(Campaign.TimeControlMode));
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(SetTimeControlModeMI))
                {
                    //Assignment to Campaign.Current.TimeControlMode
                    codes[i].opcode = OpCodes.Nop;

                    //ldc.i4.3 - set to NOP. This is the value of TimeControlMode.StoppablePlay
                    codes[i - 1].opcode = OpCodes.Nop;

                    // call to Campaign.Current
                    codes[i - 2].opcode = OpCodes.Nop;

                }
            }
            return codes.AsEnumerable();
        }
    }
}
