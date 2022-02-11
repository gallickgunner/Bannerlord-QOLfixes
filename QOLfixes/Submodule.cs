using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using SandBox.View.Map;

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
				
				if(ConfigFileManager.SkipMainIntro || ConfigFileManager.SkipSandboxIntro || ConfigFileManager.QuickStart)
					harmony.CreateClassProcessor(typeof(SkipIntroAndCharacterCreation)).Patch();

				if (!ConfigFileManager.PauseOnEnterSettlement)
					harmony.CreateClassProcessor(typeof(AutoPauseManager)).Patch();
				
				if(ConfigFileManager.RandomLoadingScreen)
					harmony.CreateClassProcessor(typeof(RandomLoadingScreens)).Patch();
				
				if (ConfigFileManager.MaintainFastForward)
					harmony.CreateClassProcessor(typeof(MaintainFastForward)).Patch();
				
				if (ConfigFileManager.EnableWaypoints)
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
