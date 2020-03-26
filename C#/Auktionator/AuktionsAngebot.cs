using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using System;

namespace Eco.Mods.GBMods
{
    public class AuktionsAngebot
    {
        public int ID { get; set; }
        public ItemStack AngebotsItem { get; set; }
        public string Kurzbeschreibung { get; set; }
        public string Beschreibung { get; set; }
        public User Ersteller { get; set; }
        public DateTime Startzeit { get; set; }
        public DateTime Endzeit { get; set; }
        public double Gebot { get; set; }
        public User Hoechstbietender { get; set; }
        public bool GeldZugeteilt { get; set; }
        public bool ItemAbgeholt { get; set; }
    }
}
