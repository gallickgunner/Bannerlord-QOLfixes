using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace QOLfixes
{
    [HarmonyPatch]
    static class AutoPauseManager
    {
        public static CampaignTimeControlMode prevTimeControlMode = CampaignTimeControlMode.StoppablePlay;
        public static bool stopGameonFocusLostOriginal;
        public static bool isFirstGameMenu = true;
        public static void RegisterEvent()
        {
            if (!ConfigFileManager.configs.pauseOnEnterSettlement)
            {
                CampaignEvents.SettlementEntered.AddNonSerializedListener(null, new Action<MobileParty, Settlement, Hero>(AutoPauseManager.OnSettlementEntered));
                CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(null, new Action<MobileParty, Settlement>(AutoPauseManager.OnSettlementLeft));
                CampaignEvents.GameMenuOpened.AddNonSerializedListener(null, new Action<MenuCallbackArgs>(AutoPauseManager.OnGameMenuOpened));
            }
            
            if(ConfigFileManager.configs.autoPauseInMissions)
            {
                CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(null, new Action<IMission>(AutoPauseManager.OnMissionStarted));
                CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(null, new Action<IMission>(AutoPauseManager.OnMissionEnded));
            }			
        }

        public static void OnSettlementEntered(MobileParty party, Settlement sett, Hero hero)
        {
            if(Hero.MainHero == hero || (party != null && party.IsMainParty) || Hero.MainHero.PartyBelongedTo == party)
                prevTimeControlMode = Campaign.Current.TimeControlMode;
        }

        //We set a flag to figure out if this is the first menu appearing when entering settlements
        public static void OnSettlementLeft(MobileParty party, Settlement sett)
        {
            if (party.IsMainParty || Hero.MainHero.PartyBelongedTo == party || (MobileParty.MainParty.AttachedTo != null && MobileParty.MainParty.AttachedTo == party.Army?.LeaderParty))
                isFirstGameMenu = true;
        }


        public static void OnGameMenuOpened(MenuCallbackArgs eventArg)
        {
            /* We restore time here if this is the first menu appearing. Since this event occurs after `OnSettlementEntered` and after `ActivateGameMenu` 
             * which occurs after `OnSettlementEntered` and stops time.
             */
            if (isFirstGameMenu)
            {
                isFirstGameMenu = false;
                Campaign.Current.TimeControlMode = prevTimeControlMode;
            }            
        }

        public static void OnMissionStarted(IMission eventArg)
        {
            Mission mission = eventArg as Mission;
            if(mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.Tournament ||
                mission.Mode == MissionMode.Duel || mission.Mode == MissionMode.Stealth)
            {
                stopGameonFocusLostOriginal = BannerlordConfig.StopGameOnFocusLost;
                BannerlordConfig.StopGameOnFocusLost = true;
            }
        }

        public static void OnMissionEnded(IMission eventArg)
        {
            Mission mission = eventArg as Mission;
            if (mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.Tournament ||
                mission.Mode == MissionMode.Duel || mission.Mode == MissionMode.Stealth)
            {
                BannerlordConfig.StopGameOnFocusLost = stopGameonFocusLostOriginal;
            }
        }

        public static bool ModifiedComputeIsWaiting(MobileParty instance, Vec2 nextTargetPosition)
        {
            bool targetReached = (2f * instance.Position2D - instance.TargetPosition - nextTargetPosition).LengthSquared < 1E-08f;
            bool aiBehavior = (instance.DefaultBehavior == AiBehavior.EngageParty || instance.DefaultBehavior == AiBehavior.EscortParty) && 
                instance.AiBehaviorObject != null && instance.AiBehaviorObject.IsValid && 
                instance.AiBehaviorObject.IsActive && instance.AiBehaviorObject.IsMobile &&
                instance.AiBehaviorObject.MobileParty.CurrentSettlement != null;

            return ( (targetReached && instance.CurrentSettlement == null) || instance.DefaultBehavior == AiBehavior.Hold || aiBehavior);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapTimeControlVM), nameof(MapTimeControlVM.Tick))]
        public static IEnumerable<CodeInstruction> PatchTick(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Label funcEnd = generator.DefineLabel();
            LocalBuilder IsCenterPanelEnabled = generator.DeclareLocal(typeof(bool));

            foreach (var instruc in instructions)
            {
                if (instruc.opcode == OpCodes.Ret)
                {
                    //Get value of IsCenterPanelEnabled in a local var
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(MapTimeControlVM), nameof(MapTimeControlVM.IsCenterPanelEnabled)));
                    yield return new CodeInstruction(OpCodes.Stloc, IsCenterPanelEnabled.LocalIndex);

                    //Check if that value is true, if so, return
                    yield return new CodeInstruction(OpCodes.Ldloc, IsCenterPanelEnabled.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Brtrue_S, funcEnd);

                    //If that value was false, set timecontrolmode to Stop
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Campaign), nameof(Campaign.Current)));
                    yield return new CodeInstruction(OpCodes.Ldloc, IsCenterPanelEnabled.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(Campaign), nameof(Campaign.TimeControlMode)));
                    instruc.labels.Add(funcEnd);
                }
                yield return instruc;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MobileParty), nameof(MobileParty.ComputeIsWaiting))]
        public static IEnumerable<CodeInstruction> PatchComputeIsWaiting(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeInstruction prevInstruc = new CodeInstruction(OpCodes.Nop);
            MethodInfo ModifiedComputeIsWaitingMI = SymbolExtensions.GetMethodInfo(() => AutoPauseManager.ModifiedComputeIsWaiting(default, default));
            FieldInfo nextTargetPositionFI = AccessTools.Field(typeof(MobileParty), nameof(MobileParty._nextTargetPosition));

            //Call our modified function and bypass original...
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, nextTargetPositionFI);
            yield return new CodeInstruction(OpCodes.Call, ModifiedComputeIsWaitingMI);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapTimeControlVM), nameof(MapTimeControlVM.ExecuteTimeControlChange))]
        public static void PatchExecuteTimeControlChange(int selectedTimeSpeed, ref MapTimeControlVM __instance, ref int ____timeFlowState)
        {
            if (!Campaign.Current.TimeControlModeLock)
            {
                int num = selectedTimeSpeed;
                if (____timeFlowState == 3 && num == 2)
                {
                    num = 4;
                }
                else if (____timeFlowState == 4 && num == 1)
                {
                    num = 3;
                }
                else if (____timeFlowState == 2 && num == 0)
                {
                    num = 6;
                }
                if (num != ____timeFlowState)
                {
                    __instance.TimeFlowState = num;
                    AccessTools.Method(typeof(MapTimeControlVM), nameof(MapTimeControlVM.SetTimeSpeed)).Invoke(__instance, new object[] { (selectedTimeSpeed) });
                }
            }
        }
    }
}
