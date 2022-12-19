using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.GameState;
using StoryMode;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using SandBox;
using TaleWorlds.MountAndBlade;

namespace QOLfixes
{
    [HarmonyPatch]
    static class SkipCampaignIntroAndCharCreation
    {				
        public static Dictionary<string, Vec2> _startingPoints = new Dictionary<string, Vec2>
        {
            {
                "empire",
                new Vec2(657.95f, 279.08f)
            },
            {
                "sturgia",
                new Vec2(356.75f, 551.52f)
            },
            {
                "aserai",
                new Vec2(300.78f, 259.99f)
            },
            {
                "battania",
                new Vec2(293.64f, 446.39f)
            },
            {
                "khuzait",
                new Vec2(680.73f, 480.8f)
            },
            {
                "vlandia",
                new Vec2(207.04f, 389.04f)
            }
        };
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SandBoxGameManager), nameof(SandBoxGameManager.OnLoadFinished))]
        public static IEnumerable<CodeInstruction> PatchSandboxOnLoadFinished(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo fromMethod = AccessTools.Method(typeof(SandBoxGameManager), nameof(SandBoxGameManager.LaunchSandboxCharacterCreation));
            MethodInfo toMethod = SymbolExtensions.GetMethodInfo(() => SkipCampaignIntroAndCharCreation.HandleQuickStart());
            MethodInfo GetDevMode =   AccessTools.PropertyGetter(typeof(TaleWorlds.Core.Game), nameof(TaleWorlds.Core.Game.IsDevelopmentMode));
            CodeInstruction prevInstruc = new CodeInstruction(OpCodes.Nop);

            Label? funcEnd;
            foreach (var instruc in instructions)
            {
                if (ConfigFileManager.configs.skipCharacterCreation && instruc.opcode == OpCodes.Ldftn && instruc.operand as MethodInfo == fromMethod)
                    instruc.operand = toMethod;

                if (ConfigFileManager.configs.skipCharacterCreation && instruc.opcode == OpCodes.Call && instruc.operand as MethodInfo == fromMethod)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    instruc.operand = toMethod;
                }

                if (ConfigFileManager.configs.skipCampaignIntro && prevInstruc.Calls(GetDevMode) && instruc.Branches(out funcEnd))
                {
                    //Load true so it definitely skips loading campaign video
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                }
                prevInstruc = instruc;
                yield return instruc;
            }
        }

        public static IEnumerable<CultureObject> GetCultures()
        {
            foreach (CultureObject cultureObject in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
            {
                if (cultureObject.IsMainCulture)
                {
                    yield return cultureObject;
                }
            }
        }

        public static void HandleQuickStart()
        {            

            /* Skip creating CharacterCreationStages and what not entirely. Instead set values of Hero and Clan directly
             * and initialize the call to MapState to load campaign.
             */

            /*
             * 1)  <CharacterCreationState> calls initializes which traces back to <CharacterCreationContentBase> which calls 
             * 
             * this.initializeMainheroStats()
             */

            // Initialize Hero SKills/Attributes
            Hero.MainHero.HeroDeveloper.ClearHero();
            Hero.MainHero.HitPoints = 100;
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-20));
            Hero.MainHero.SetName(new TextObject("Umer"), null);
            Hero.MainHero.HeroDeveloper.UnspentFocusPoints = 15;
            Hero.MainHero.HeroDeveloper.UnspentAttributePoints = 15;
            Hero.MainHero.HeroDeveloper.SetInitialLevel(1);

            foreach (SkillObject skill in Skills.All)
            {
                Hero.MainHero.HeroDeveloper.InitializeSkillXp(skill);
            }
            foreach (CharacterAttribute attrib in Attributes.All)
            {
                Hero.MainHero.HeroDeveloper.AddAttribute(attrib, 2, false);
            }

            //Apply Culture
            TextObject to = Helpers.FactionHelper.GenerateClanNameforPlayer();
            Clan.PlayerClan.ChangeClanName(to, to);
            CharacterObject.PlayerCharacter.Culture = SkipCampaignIntroAndCharCreation.GetCultures().GetRandomElementInefficiently<CultureObject>();
            Clan.PlayerClan.Culture = CharacterObject.PlayerCharacter.Culture;
            Clan.PlayerClan.UpdateHomeSettlement(null);
            Clan.PlayerClan.Renown = 0f;
            Hero.MainHero.BornSettlement = Clan.PlayerClan.HomeSettlement;

            //Can apply equipments here but we skip, since goal is to test campaign. We can make do with random/default.


            /* 2) <CharacterCreationState> calls <FinalizeCharacterCreation()>
             * This calls 
             * 
             * CharacterCreationScreen.OnCharacterCreationFinalized()
             * this.CurrentCharacterCreationContent.OnCharacterCreationFinalized();
             * CampaignEventDispatcher.Instance.OnCharacterCreationIsOver();
             * 
             * These calls mainly do the main work of applying culture and initializing main hero stats which we have done above,
             * Hence we only need to initialize MapState now and teleport the camera onto map where party is.
             */
            LoadingWindow.EnableGlobalLoadingWindow();
            Game.Current.GameStateManager.CleanAndPushState(Game.Current.GameStateManager.CreateState<MapState>(), 0);
            PartyBase.MainParty.Visuals.SetMapIconAsDirty();
            CultureObject culture = CharacterObject.PlayerCharacter.Culture;

            Vec2 position2D;
            if (_startingPoints.TryGetValue(culture.StringId, out position2D))
            {
                MobileParty.MainParty.Position2D = position2D;
            }
            else
            {
                MobileParty.MainParty.Position2D = Campaign.Current.DefaultStartingPosition;
                FileLog.Log("Selected culture is not in the dictionary!" + "\r\nIn HandleQuickStart(), Line No: 224 at <SkipIntro.cs>");
            }
            CampaignEventDispatcher.Instance.OnCharacterCreationIsOver();

            MapState mapState;
            if ((mapState = (GameStateManager.Current.ActiveState as MapState)) != null)
            {
                mapState.Handler.ResetCamera(true, true);
            }
        }
    }
}
