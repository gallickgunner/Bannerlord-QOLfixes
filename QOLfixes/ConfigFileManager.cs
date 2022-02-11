using System;
using System.IO;

namespace QOLfixes
{
    static class ConfigFileManager
    {
        private static string assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string parentDir = Directory.GetParent(Directory.GetParent(assemblyDir).ToString()).ToString();

        private static string cfgPath = parentDir + "\\QOLfixes.cfg";

        public static bool SkipMainIntro { get; private set; } = true;
        public static bool SkipSandboxIntro { get; private set; } = true;
        public static bool QuickStart { get; private set; } = false;
        public static bool RandomLoadingScreen { get; private set; } = true;
        public static bool PauseOnEnterSettlement { get; private set; } = false;
        public static bool MaintainFastForward { get; private set; } = true;
        public static bool AutoPauseInMissions { get; private set; } = true;
        public static bool EnableWaypoints { get; private set; } = true;

        public static bool HelperBooleanTryParse(string str, out bool success)
        {
            bool res;
            success = Boolean.TryParse(str, out res);
            return res;
        }

        public static bool LoadConfigFile(out string error)
        {
            bool success = true;
            error = "";
            if (File.Exists(cfgPath))
            {
                foreach (string line in File.ReadLines(cfgPath))
                {
                    if (!line.StartsWith("#") && !string.IsNullOrEmpty(line))
                    {
                        string[] option = line.Split(new char[] { '=' });

                        if(option[0] == "skipMainIntro")
                            SkipMainIntro = HelperBooleanTryParse(option[1], out success);
                        else if (option[0] == "skipCampaignIntro")
                            SkipSandboxIntro = HelperBooleanTryParse(option[1], out success);
                        else if (option[0] == "skipCC")
                            QuickStart = HelperBooleanTryParse(option[1], out success);
                        else if (option[0] == "enableRandomLoadingScreen")
                            RandomLoadingScreen = HelperBooleanTryParse(option[1], out success);
                        else if (option[0] == "pauseOnEnterSettlement")
                            PauseOnEnterSettlement = HelperBooleanTryParse(option[1], out success);
                        else if (option[0] == "autoPauseInMissions")
                            AutoPauseInMissions = HelperBooleanTryParse(option[1], out success);
                        else if (option[0] == "maintainFastForwardOnSingleClick")
                            MaintainFastForward = HelperBooleanTryParse(option[1], out success);
                        else if (option[0] == "enableWaypoints")
                            EnableWaypoints = HelperBooleanTryParse(option[1], out success);

                        if (!success)
                        {
                            error = "Error parsing options. Make sure there are no whitespaces.";
                            SkipMainIntro = true;
                            SkipSandboxIntro = true;
                            RandomLoadingScreen = true;
                            MaintainFastForward = true;
                            EnableWaypoints = true;
                            AutoPauseInMissions = true;
                            return false;
                        }
                    }
                }
                return success;
            }
            else
            {
                error = "Error finding the config file. Create a file named <QOLfixes.cfg> inside the mod folder";
                return !success;
            }
        }
    }
}
