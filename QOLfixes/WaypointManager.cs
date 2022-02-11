using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.InputSystem;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.GameMenus;
using SandBox.View.Menu;
using SandBox.ViewModelCollection.Nameplate;

namespace QOLfixes
{
    [HarmonyPatch]
    public static class WaypointManager
    {
        public static Queue<Settlement> waypoints = new Queue<Settlement>(10);
        public static CampaignTimeControlMode prevTimeControlMode = CampaignTimeControlMode.StoppablePlay;
                     
        public static void RegisterEvent()
        {
            if (ConfigFileManager.EnableWaypoints)
            {
                CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(null, new Action<GameMenuOption>(QOLfixes.WaypointManager.OnGameMenuOptionSelectedEvent));
                CampaignEvents.SettlementEntered.AddNonSerializedListener(null, new Action<MobileParty, Settlement, Hero>(QOLfixes.WaypointManager.OnSettlementEntered));
            }
        }

        /* Remove waypoints on settlment enter 
         */
        public static void OnSettlementEntered(MobileParty party, Settlement sett, Hero hero)
        {
            if (Hero.MainHero == hero || (party != null && party.IsMainParty))
            {
                prevTimeControlMode = Campaign.Current.TimeControlMode;
                if (!waypoints.IsEmpty() && waypoints.Peek() == sett)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Waypoint Reached: " + sett.ToString()));
                    Campaign.Current.VisualTrackerManager.RemoveTrackedObject(sett);
                    waypoints.Dequeue();
                }
            }
        }

        /* When leaving settlements automatically move to next ones
         */
        public static void OnGameMenuOptionSelectedEvent(GameMenuOption option)
        {
            if (option.OptionLeaveType == GameMenuOption.LeaveType.Leave)
            {
                if (!waypoints.IsEmpty())
                {
                    InformationManager.DisplayMessage(new InformationMessage("Moving to next waypoint: " + waypoints.Peek().Name.ToString()));
                    MobileParty.MainParty.SetMoveGoToSettlement(waypoints.Peek());
                    Campaign.Current.TimeControlMode = prevTimeControlMode;
                }
            }
        }

        /* This is our custom function called inside the original function `SettlementNameplateVM.ExecuteSetCameraPosition`.
         * Check if Shift key is pressed. IF so, we don't need to zoom camera to settlement, instead add to the waypoint
         */
        public static bool onSettlementNamePlateClick(Settlement sett)
        {
            if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
            {
                if (waypoints.Count < 10 && !waypoints.Contains(sett))
                {
                    InformationManager.DisplayMessage(new InformationMessage("Adding waypoint: " + sett.Name.ToString()));
                    waypoints.Enqueue(sett);
                    Campaign.Current.VisualTrackerManager.RegisterObject(sett);
                }
                return true;
            }
            return false;
        }

        public static void HandleHotkeyPresses(MapScreen instance, MenuViewContext viewContext)
        {
            InputContext inputCtx = instance.SceneLayer.Input;
            
            if(ConfigFileManager.EnableWaypoints && viewContext == null)
            {
                if (inputCtx.IsHotKeyPressed(CustomMapHotkeyCategory.clearWaypointKeyName))
                {
                    foreach (var waypoint in waypoints)
                    {
                        Campaign.Current.VisualTrackerManager.RemoveTrackedObject(waypoint);
                    }
                    waypoints.Clear();
                }
                else if (inputCtx.IsHotKeyPressed(CustomMapHotkeyCategory.dequeueWaypointKeyName))
                {
                    if(!waypoints.IsEmpty())
                        Campaign.Current.VisualTrackerManager.RemoveTrackedObject(waypoints.Dequeue());
                }

                else if (inputCtx.IsHotKeyPressed(CustomMapHotkeyCategory.startWaypointTravelKeyName))
                {
                    MobileParty mainParty = MobileParty.MainParty;
                    bool flag = mainParty != null && PartyBase.MainParty.IsValid;
                    if (flag && !waypoints.IsEmpty())
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Moving to next waypoint: " + waypoints.Peek().Name.ToString()));
                        mainParty.SetMoveGoToSettlement(waypoints.Peek());
                        if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
                            Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                    }
                }
            }
            
            if (inputCtx.IsHotKeyPressed(CustomMapHotkeyCategory.IncreaseFFSpeedKeyName))
            {
                Campaign.Current.SpeedUpMultiplier += 1.0f;
                InformationManager.DisplayMessage(new InformationMessage("Fast Forward Speed set to: " + Campaign.Current.SpeedUpMultiplier.ToString()));
            }
            else if (inputCtx.IsHotKeyPressed(CustomMapHotkeyCategory.DecreaseFFSpeedKeyName))
            {
                if (Campaign.Current.SpeedUpMultiplier > 4.0f)
                    Campaign.Current.SpeedUpMultiplier -= 1.0f;
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
            MethodInfo HandleHotkeyPressesMI = SymbolExtensions.GetMethodInfo(() => WaypointManager.HandleHotkeyPresses(null, null));

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

        /* Transpiler to handle camera zoom logic. The zoom happens on left clicking settlement Nameplate.
         * In other words this function is actually the event callback on shift-leftclicking the nameplate
         * We want to make sure that shift-leftclicking doesn't make the camera zoom.
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SettlementNameplateVM), "ExecuteSetCameraPosition")]
        public static IEnumerable<CodeInstruction> ExecuteSetCameraPosition(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo HandleWayPointMI = SymbolExtensions.GetMethodInfo(() => QOLfixes.WaypointManager.onSettlementNamePlateClick(null));
            MethodInfo GetSettlementMI = AccessTools.PropertyGetter(typeof(SettlementNameplateVM), nameof(SettlementNameplateVM.Settlement));
            Label funcEnd = generator.DefineLabel();

            //Add a call to our own method which manages waypoints and decides whether to move camera to settlement or not
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, GetSettlementMI);
            yield return new CodeInstruction(OpCodes.Call, HandleWayPointMI);
            yield return new CodeInstruction(OpCodes.Brtrue, funcEnd);

            foreach (var instruc in instructions)
            {
                if (instruc.opcode == OpCodes.Ret)
                    instruc.labels.Add(funcEnd);
                yield return instruc;
            }
        }

        /* This function is the event callback for when the track button is pressed. 
         * When manually tracking without using Shift key, We need to handle cases for being already tracked by waypoints. 
         * We want the original system to co-exist with the waypoint system. The vanilla code makes this possible
         * since adding/removing track objects doesn't only add/removes them but also increments a counter and only remove when
         * counter reaches 0. This can be used to check if tracking is being done by both Button Tracking and Waypoint tracking.
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SettlementNameplateVM), "ExecuteTrack")]
        public static IEnumerable<CodeInstruction> PatchExecuteTrack(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            bool var = false;
            MethodInfo isTrackedMI = AccessTools.PropertyGetter(typeof(SettlementNameplateVM), nameof(SettlementNameplateVM.IsTracked));

            //Add our own method that executes custom logic. Run original only if current settlement is not a waypoint.            
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SettlementNameplateVM), "_isTrackedManually"));

            foreach (var instruc in instructions)
            {
                if (instruc.Calls(isTrackedMI))
                {
                    yield return instruc;
                    yield return new CodeInstruction(OpCodes.And);
                    continue;
                }
                yield return instruc;
            }
        }

        /* If isTrackedManually is true, don't register object any further. This is done because Track() is called by at the start of some
         * events. Hence, this function can be called when you haven't pressed the track button explicitly. Originally this check wasn't needed
         * because the original check tracked only if the settlment wasn't tracked previously. Since we want it to be tracked manually PLUS through waypoints
         * we have to remove the old check and add this new one.
         */
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SettlementNameplateVM), "Track")]
        public static IEnumerable<CodeInstruction> PatchTrack(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            bool var = false;
            MethodInfo isTrackedMI = AccessTools.PropertySetter(typeof(SettlementNameplateVM), nameof(SettlementNameplateVM.IsTracked));
            bool branchReached = false, instructionSet = false;
            Label? label;

            foreach (var instruc in instructions)
            {
                if (instruc.Branches(out label))
                {
                    branchReached = true;
                }
                else if (instruc.Calls(isTrackedMI))
                {
                    yield return instruc;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SettlementNameplateVM), "_isTrackedManually"));
                    instructionSet = true;
                    continue;
                }

                if (!branchReached && instructionSet)
                    continue;
                yield return instruc;
            }
        }        
    }
}
