using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace InfamousRP_Bot
{
    public class SettingsFile
    {
        public string MySQLString = "Server=myserver;User ID=mylogin;Password=mypass;Database=mydatabase";
        public string DiscordBotToken = "";
        public string WhitelistedRoleName = "";
        public List<string> ProductionGuildServerNames = new List<string>();
        public List<string> DevelopmentGuildServerNames = new List<string>();
    }
    
    public static class Settings
    {
        private const string fileLocation = "settings.json";
        
        public static bool isConfigured()
        {
            return File.Exists(fileLocation);
        }
        
        private static SettingsFile settings = new SettingsFile();
        
        public static void loadSettings()
        {
            settings = isConfigured() ? JsonConvert.DeserializeObject<SettingsFile>(File.ReadAllText(fileLocation)) : new SettingsFile();
        }
        
        public static SettingsFile getSettings()
        {
            if (settings == null)
            {
                loadSettings();
            }

            return settings;
        }

        public static void saveSettings()
        {
            File.WriteAllText(fileLocation,JsonConvert.SerializeObject(settings));
        }
        
    }
}
