using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.CampaignSystem.Settlements;
using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.InputSystem;

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
            if (!ConfigFileManager.configs.autoPauseOnEnterSettlement)
            {
                CampaignEvents.SettlementEntered.AddNonSerializedListener(null, new Action<MobileParty, Settlement, Hero>(AutoPauseManager.OnSettlementEntered));
                
                CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(null, new Action<MobileParty, Settlement>(AutoPauseManager.OnSettlementLeft));
                
                CampaignEvents.GameMenuOpened.AddNonSerializedListener(null, new Action<MenuCallbackArgs>(AutoPauseManager.OnGameMenuOpened));
            }

            if (ConfigFileManager.configs.autoPauseInMissionsOnly)
            {
                CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(null, new Action<IMission>(AutoPauseManager.OnMissionStarted));
                CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(null, new Action<IMission>(AutoPauseManager.OnMissionEnded));
            }
        }

        public static void HandleHotkeyPresses(MapScreen instance, MenuViewContext viewContext)
        {
            InputContext inputCtx = instance.SceneLayer.Input;
            
            //Restore changing time through hotkeys when in main menus
            if (!Campaign.Current.TimeControlModeLock && Campaign.Current.CurrentMenuContext != null)
            {
                bool isMainMenu = Campaign.Current.CurrentMenuContext.GameMenu.StringId == "village" || Campaign.Current.CurrentMenuContext.GameMenu.StringId == "town" ||
                  Campaign.Current.CurrentMenuContext.GameMenu.StringId == "castle";

                if(isMainMenu)
                {
                    if (inputCtx.IsGameKeyPressed(59))
                        Campaign.Current.SetTimeSpeed(0);
                    if (inputCtx.IsGameKeyPressed(60))
                        Campaign.Current.SetTimeSpeed(1);
                    if (inputCtx.IsGameKeyPressed(61))
                        Campaign.Current.SetTimeSpeed(2);
                }
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
            /* We restore time here if this is the first main menu (town/castle/village) appearing. Since this event occurs after `OnSettlementEntered` and after `ActivateGameMenu` 
             * which occurs after `OnSettlementEntered` and stops time.
             */
            if (isFirstGameMenu && (eventArg.MenuContext.GameMenu.StringId == "village" || eventArg.MenuContext.GameMenu.StringId == "town" || eventArg.MenuContext.GameMenu.StringId == "castle"))
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
                instance.AiBehaviorPartyBase != null && instance.AiBehaviorPartyBase.IsValid && 
                instance.AiBehaviorPartyBase.IsActive && instance.AiBehaviorPartyBase.IsMobile &&
                instance.AiBehaviorPartyBase.MobileParty.CurrentSettlement != null;

            return ( (targetReached && instance.CurrentSettlement == null) || instance.DefaultBehavior == AiBehavior.Hold || aiBehavior);
        }

        public static void ModifiedExecuteTimeControlChange(MapTimeControlVM instance, int selectedTimeSpeed)
        {
            bool isMainMenu = false;
            if (Campaign.Current.CurrentMenuContext != null)
            {
                isMainMenu = Campaign.Current.CurrentMenuContext.GameMenu.StringId == "village" || Campaign.Current.CurrentMenuContext.GameMenu.StringId == "town" ||
                  Campaign.Current.CurrentMenuContext.GameMenu.StringId == "castle";
            }

            if (Campaign.Current.CurrentMenuContext == null || (Campaign.Current.CurrentMenuContext.GameMenu.IsWaitActive && !Campaign.Current.TimeControlModeLock) || isMainMenu)
            {
                int num = selectedTimeSpeed;
                if (instance.TimeFlowState == 3 && num == 2)
                {
                    num = 4;
                }
                else if (instance.TimeFlowState == 4 && num == 1)
                {
                    num = 3;
                }
                else if (instance.TimeFlowState == 2 && num == 0)
                {
                    num = 6;
                }
                if (num != instance.TimeFlowState)
                {
                    instance.TimeFlowState = num;
                    AccessTools.Method(typeof(MapTimeControlVM), "SetTimeSpeed").Invoke(instance, new object[] { (selectedTimeSpeed) });
                }
            }
        }

        //Manually patch it based on options
        public static IEnumerable<CodeInstruction> PatchMapTimeControlVMTick(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
            MethodInfo ModifiedComputeIsWaitingMI = SymbolExtensions.GetMethodInfo(() => AutoPauseManager.ModifiedComputeIsWaiting(default, default));
            FieldInfo nextTargetPositionFI = AccessTools.Field(typeof(MobileParty), "_nextTargetPosition");

            //Call our modified function and bypass original...
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, nextTargetPositionFI);
            yield return new CodeInstruction(OpCodes.Call, ModifiedComputeIsWaitingMI);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapTimeControlVM), nameof(MapTimeControlVM.ExecuteTimeControlChange))]
        public static IEnumerable<CodeInstruction> PatchExecuteTimeControlChange(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo ModifiedExecuteTimeControlChangeMI = SymbolExtensions.GetMethodInfo(() => 
                    AutoPauseManager.ModifiedExecuteTimeControlChange(default, default));

            //Call our modified function and bypass original...
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, ModifiedExecuteTimeControlChangeMI);
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
