using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace QOLfixes
{
    static class ConfigFileManager
    {
        public class ConfigData
        {
            public bool skipMainIntro = true;
            public bool skipCampaignIntro = false;
            public bool skipCharacterCreation = false;
            public bool randomLoadingScreen = true;
            public bool autoPauseOnEnterSettlement = true;
            public bool pauseOnMenuOverlays = true;
            public bool autoPauseInMissionsOnly = false;
            public bool leaveSettlementOnClick = true;            
            public bool maintainFastForwardOnSingleClick = true;
            public bool waypoints = true;
        }

        public static ConfigData configs = new ConfigData();
        private static string assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string parentDir = Directory.GetParent(Directory.GetParent(assemblyDir).ToString()).ToString();
        private static string jsonPath = parentDir + "\\QOLfixes.json";

        public static bool LoadConfigFile(out string error)
        {
            bool success;
            error = "";
            if (File.Exists(jsonPath))
            {
                using (StreamReader file = File.OpenText(jsonPath))
                {
                    try
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        configs = (ConfigData)serializer.Deserialize(file, typeof(ConfigData));
                        success = true;
                    }
                    catch (Exception e)
                    {
                        error = "Error parsing json file. Make sure it has a valid Json object.";
                        success = false;
                    }
                }
            }
            else
            {
                error = "Error finding config json file.";
                success = false;
            }
            return success;
        }
    }
}
