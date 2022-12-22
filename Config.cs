using Newtonsoft.Json;
using TShockAPI;

namespace EconomyPlugin
{
    public class Config
    {
        public string currencyName { get; set; } = "BokMak";

        public List<int> excludedMobs { get; set; } = new List<int>() { };

        public bool enableMobDrops { get; set; } = true;
        public bool announceMobDrops { get; set; } = true;
        public double DropOnDeath { get; set; } = 0;

        public void Write()
        {
            string path = Path.Combine(TShock.SavePath, "EconomyConfig.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public static Config Read()
        {
            string filepath = Path.Combine(TShock.SavePath, "EconomyConfig.json");
            try
            {
                Config config = new Config();

                if (!File.Exists(filepath))
                {
                    File.WriteAllText(filepath, JsonConvert.SerializeObject(config, Formatting.Indented));
                }
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filepath));


                return config;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
                return new Config();
            }
        }
    }
}
