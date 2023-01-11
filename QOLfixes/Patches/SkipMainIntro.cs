using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using TaleWorlds.Engine;

namespace QOLfixes
{
    [HarmonyPatch]
    static class SkipMainIntro
    {

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TaleWorlds.MountAndBlade.Module),"OnInitialModuleScreenActivated")]
        public static IEnumerable<CodeInstruction> PatchOnInitialModuleScreenActivated(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            //Don't load loading Window since video has been skipped
            int i = 0;
            foreach (var instruc in instructions)
            {
                if(i > 1)
                    yield return instruc;
                i++;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TaleWorlds.MountAndBlade.Module), nameof(TaleWorlds.MountAndBlade.Module.SetInitialModuleScreenAsRootScreen))]
        public static IEnumerable<CodeInstruction> PatchSetInitialModuleScreenAsRootScreen(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo referenceMethod = AccessTools.Method(typeof(TaleWorlds.MountAndBlade.Module), "OnInitialModuleScreenActivated");
            FieldInfo toMatchFI = AccessTools.Field(typeof(TaleWorlds.MountAndBlade.Module), "_splashScreenPlayed");

            CodeInstruction prevInstruc = new CodeInstruction(OpCodes.Nop);
            Label funcCall = generator.DefineLabel();
            foreach (var instruc in instructions)
            {
                if (prevInstruc.opcode == OpCodes.Ldfld && prevInstruc.operand as FieldInfo == toMatchFI)
                {
                    //Load true, so it definitely skips playing the video
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                }

                prevInstruc = instruc;
                yield return instruc;
            }
        }
    }
}
