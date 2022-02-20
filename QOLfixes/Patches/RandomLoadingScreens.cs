using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.GauntletUI;
namespace QOLfixes
{
    [HarmonyPatch]
    public static class RandomLoadingScreens
    {
        public static int queueCapacity = 4;
        public static Queue<int> hotIndexes = new Queue<int>(queueCapacity);
        public static Random randomGen = new Random(Environment.TickCount);
        public static int totalLoadingScreens = -1;
        public static int prevImage = -1;
        public static int currentImage = 0;

        /* HOW LOADING SCREENS WORK
         * TW has two functions called PartialLoading and PartialUnLoading.
         * TW has implemented a "Load the next loading screen while showing current loading screen and unload the previous one" system.
         * When the game starts, before any loading screen is showed, TW loads a screen for the first time in the constructor
         * Then whenever they want to show a loading screen, they call "SetNextGenericImage" which sets the current loading screen
         * index to the previously loaded one, unloads the previous loading screen shown and loads the next one.
         * However all of this is done in an ascending order which means SAME OLD PATTERN every time you open the game.
         * We use Random Numbers to spice things up
         */

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadingWindowViewModel), nameof(LoadingWindowViewModel.SetTotalGenericImageCount))]
        public static void PatchSetTotalGenericImageCount(ref int ____totalGenericImageCount, ref Action<bool, int> ____handleSPPartialLoading)
        {
            // +1 for ease when calling rand next
            totalLoadingScreens = ____totalGenericImageCount + 1;

            //Since I'm too fed up with images having the first indices, we put a 6 here for the first loading screen.
            currentImage = randomGen.Next(6, totalLoadingScreens);
            hotIndexes.Enqueue(currentImage);

            if (____handleSPPartialLoading != null)
                ____handleSPPartialLoading(true, currentImage);
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(LoadingWindowViewModel), nameof(LoadingWindowViewModel.SetNextGenericImage))]
        public static IEnumerable<CodeInstruction> PatchSetNextGenericImage(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo CalculateLoadingScreenNumberMI = AccessTools.Method(typeof(RandomLoadingScreens), nameof(RandomLoadingScreens.CalculateLoadingScreenNumber));
            FieldInfo currentImageFI = AccessTools.Field(typeof(LoadingWindowViewModel), nameof(LoadingWindowViewModel._currentImage));
            FieldInfo handlePartialLoadActionFI = AccessTools.Field(typeof(LoadingWindowViewModel), nameof(LoadingWindowViewModel._handleSPPartialLoading));
            bool isReferencePointReached = false;

            //Calculate current Image and prev and next image numbers to unload/load
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldflda, currentImageFI);
            yield return new CodeInstruction(OpCodes.Ldloca, 0);
            yield return new CodeInstruction(OpCodes.Ldloca, 1);
            yield return new CodeInstruction(OpCodes.Call, CalculateLoadingScreenNumberMI);

            foreach (var instruc in instructions)
            {	
                if(instruc.LoadsField(handlePartialLoadActionFI) && !isReferencePointReached)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    isReferencePointReached = true;
                }
                if (!isReferencePointReached)
                    continue;

                yield return instruc;		
            }				
        }

        public static void CalculateLoadingScreenNumber(out int currImageNumber, out int unLoadImageNumber, out int loadImageNo)
        {
            int rand;
            if(Game.Current != null && Game.Current.RandomGenerator != null)
                rand = MBRandom.RandomInt(1, totalLoadingScreens);
            else
                rand = randomGen.Next(1, totalLoadingScreens);

            int i = 0;
            //We don't want to keep calling Random for a long time. If it can't find a unique, just dequeue the oldest one.
            while (hotIndexes.Contains(rand) && i < queueCapacity)
            {
                if (Game.Current != null && Game.Current.RandomGenerator != null)
                    rand = MBRandom.RandomInt(1, totalLoadingScreens);
                else
                    rand = randomGen.Next(1, totalLoadingScreens);
                i++;
            }
            if (i >= queueCapacity)
                rand = hotIndexes.Dequeue();

            hotIndexes.Enqueue(rand);
            
            if (hotIndexes.Count > queueCapacity)
                hotIndexes.Dequeue();

            currImageNumber = currentImage;
            unLoadImageNumber = prevImage;
            loadImageNo = rand;

            //Update for next time
            prevImage = currImageNumber;
            currentImage = loadImageNo;			
        }
    }	
}
