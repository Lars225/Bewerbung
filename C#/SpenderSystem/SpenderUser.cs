using System;

namespace Eco.Mods.GBMods
{
    class SpenderUser
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string SteamId { get; set; }
        public string SlgId { get; set; }
        public DateTime EndDate { get; set; }
        public int LastGiftDay { get; set; }
        public DateTime LastBear { get; set; }
        public bool Patreon { get; set; } = false;
        public int LeftDays { get; set; }
    }
}
