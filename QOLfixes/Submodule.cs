using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using SandBox.View.Map;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;

namespace QOLfixes
{
    public class SubModule : MBSubModuleBase
    {
        private static string error = "";
        private static bool exceptionThrown = false;
        protected override void OnSubModuleLoad()
        {
            try
            { 
                base.OnSubModuleLoad();
                ConfigFileManager.LoadConfigFile(out error);
                Harmony harmony = new Harmony("GG.Utilities.QOLfixes");
                //Harmony.DEBUG = true;
                harmony.CreateClassProcessor(typeof(CustomHotkeysManager)).Patch();

                if(ConfigFileManager.configs.skipMainIntro)
                    harmony.CreateClassProcessor(typeof(SkipMainIntro)).Patch();

                if(ConfigFileManager.configs.skipCampaignIntro || ConfigFileManager.configs.skipCharacterCreation)
                    harmony.CreateClassProcessor(typeof(SkipCampaignIntroAndCharCreation)).Patch();

                if (!ConfigFileManager.configs.autoPauseOnEnterSettlement)
                {
                    harmony.CreateClassProcessor(typeof(AutoPauseManager)).Patch();
                    if(ConfigFileManager.configs.pauseOnMenuOverlays)
                    {
                        var orig = typeof(MapTimeControlVM).GetMethod(nameof(MapTimeControlVM.Tick));
                        var patch = typeof(AutoPauseManager).GetMethod(nameof(AutoPauseManager.PatchMapTimeControlVMTick));
                        harmony.Patch(orig, transpiler: new HarmonyMethod(patch));
                    }
                }
                
                if(ConfigFileManager.configs.randomLoadingScreen)
                    harmony.CreateClassProcessor(typeof(RandomLoadingScreens)).Patch();
                
                if (ConfigFileManager.configs.maintainFastForwardOnSingleClick)
                    harmony.CreateClassProcessor(typeof(MaintainFastForward)).Patch();
                
                if (ConfigFileManager.configs.waypoints)
                    harmony.CreateClassProcessor(typeof(WaypointManager)).Patch();                
                
                HotKeyManager.AddAuxiliaryCategory(new CustomMapHotkeyCategory());
            }
            catch (Exception e)
            {
                exceptionThrown = true;
                FileLog.Log("Message: " + e.ToString());
            }
        }
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

            if (!string.IsNullOrEmpty(error))
                InformationManager.DisplayMessage(new InformationMessage(error));
            if(exceptionThrown)
                InformationManager.DisplayMessage(new InformationMessage("Error loading one or more mods. Check \"harmony.log\" for detailed report."));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (game.GameType is Campaign)
            {				
                AutoPauseManager.RegisterEvent();
                WaypointManager.RegisterEvent();
            }
        }
    }
}
