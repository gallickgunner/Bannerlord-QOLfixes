using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using SandBox.View.Map;
using TaleWorlds.Library;

namespace QOLfixes
{
    public class SubModule : MBSubModuleBase
    {
        private static string error = "";
        private static bool exceptionThrown = false;
        public override void OnSubModuleLoad()
        {
            try
            { 
                base.OnSubModuleLoad();
                ConfigFileManager.LoadConfigFile(out error);
                Harmony harmony = new Harmony("GG.Utilities.QOLfixes");
                
                if(ConfigFileManager.configs.skipMainIntro)
                    harmony.CreateClassProcessor(typeof(SkipMainIntro)).Patch();

                if(ConfigFileManager.configs.skipCampaignIntro || ConfigFileManager.configs.skipCharacterCreation)
                    harmony.CreateClassProcessor(typeof(SkipCampaignIntroAndCharCreation)).Patch();

                if (!ConfigFileManager.configs.pauseOnEnterSettlement)
                    harmony.CreateClassProcessor(typeof(AutoPauseManager)).Patch();
                
                if(ConfigFileManager.configs.randomLoadingScreen)
                    harmony.CreateClassProcessor(typeof(RandomLoadingScreens)).Patch();
                
                if (ConfigFileManager.configs.maintainFastForwardOnSingleClick)
                    harmony.CreateClassProcessor(typeof(MaintainFastForward)).Patch();
                
                if (ConfigFileManager.configs.waypoints)
                    harmony.CreateClassProcessor(typeof(WaypointManager)).Patch();
                else
                {
                    //only patch functionality related to hotkeys for increasing/decreasing party speed
                    harmony.Patch(AccessTools.Method(typeof(MapScreen), "OnInitialize"), postfix: new HarmonyMethod(AccessTools.Method(typeof(WaypointManager), nameof(WaypointManager.PatchOnInitialize))));
                    harmony.Patch(AccessTools.Method(typeof(MapScreen), "OnFrameTick"), transpiler: new HarmonyMethod(AccessTools.Method(typeof(WaypointManager), nameof(WaypointManager.PatchOnFrameTick))));
                }
                
                HotKeyManager.AddAuxiliaryCategory(new CustomMapHotkeyCategory());
            }
            catch (Exception e)
            {
                exceptionThrown = true;
                FileLog.Log("Message: " + e.ToString());
            }
        }
        public override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (!string.IsNullOrEmpty(error))
                InformationManager.DisplayMessage(new InformationMessage(error));
            if(exceptionThrown)
                InformationManager.DisplayMessage(new InformationMessage("Error loading one or more mods. Check \"harmony.log\" for detailed report."));
        }

        public override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {				
                AutoPauseManager.RegisterEvent();
                WaypointManager.RegisterEvent();
            }
        }
    }
}
