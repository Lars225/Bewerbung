using Eco.Core.Localization;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using Eco.Shared.Services;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eco.Mods.GBMods
{

    // Mit dieser Modifkation bekommen Spender die zur Finazierung des GameServers beitragen eine kleine aufmerksamkeit.
    class SpenderSystem
    {
        // Hinzufügen eines Spenders in die Datenbank.
        public static void AddSpenderUser(User runningUser, string userId, string month)
        {
            User newSpenderUser = UserManager.FindUserByID(int.Parse(userId));
            using (var db = new LiteDatabase(@"GBSpender.db"))
            {
                SpenderUser existUser;
                var premiumusers = db.GetCollection<SpenderUser>("SpenderUser");
                if (newSpenderUser.SteamId != null)
                {
                    existUser = premiumusers.FindOne(x => x.SteamId.Contains(newSpenderUser.SteamId));
                }
                else
                {
                    existUser = premiumusers.FindOne(x => x.SteamId.Contains(newSpenderUser.SlgId));
                }
                if (existUser != null)
                {
                    if (existUser.EndDate < DateTime.Now)
                    {
                        existUser.EndDate = existUser.EndDate.AddMonths(int.Parse(month));
                    }
                    else
                    {
                        existUser.EndDate = DateTime.Now.AddMonths(int.Parse(month));
                    }
                    if (existUser.Username != newSpenderUser.Name)
                        existUser.Username = newSpenderUser.Name;
                    premiumusers.Update(existUser);
                    ChatManager.ServerMessageToPlayer(new LocString("Dem Spieler mit der ID " + newSpenderUser.Id + " und dem Namen " + newSpenderUser.Name + " wurden " + month + " Monate Premium hinzugefügt!"), runningUser, false, DefaultChatTags.Notifications, ChatCategory.Info);
                }
                else
                {
                    SpenderUser newSpenderUserObject = new SpenderUser
                    {
                        Username = newSpenderUser.Name,
                        SteamId = newSpenderUser.SteamId,
                        SlgId = newSpenderUser.SlgId,
                        EndDate = DateTime.Now.Date.AddMonths(int.Parse(month))
                    };
                    premiumusers.Insert(newSpenderUserObject);
                    ChatManager.ServerMessageToPlayer(new LocString("Der Spieler mit der ID " + newSpenderUser.Id + " und dem Namen " + newSpenderUser.Name + " wurde mit " + month + " Monaten Premium angelegt!"), runningUser, false, DefaultChatTags.Notifications, ChatCategory.Info);

                }
            }
        }

        // Ändern des Patreon Status.
        public static void ChangePatreonUser(User runningUser, string userId, bool wert)
        {
            User newSpenderUser = UserManager.FindUserByID(int.Parse(userId));
            using (var db = new LiteDatabase(@"GBSpender.db"))
            {
                SpenderUser existUser;
                var premiumusers = db.GetCollection<SpenderUser>("SpenderUser");
                if (newSpenderUser.SteamId != null)
                {
                    existUser = premiumusers.FindOne(x => x.SteamId.Contains(newSpenderUser.SteamId));
                }
                else
                {
                    existUser = premiumusers.FindOne(x => x.SteamId.Contains(newSpenderUser.SlgId));
                }
                if (existUser != null)
                {
                    if (wert == true)
                    {
                        existUser.Patreon = true;
                        if (existUser.EndDate > DateTime.Now)
                        {
                            existUser.LeftDays = existUser.EndDate.Subtract(DateTime.Now).Days;
                        }
                    }
                    if (wert == false)
                    {
                        existUser.Patreon = false;
                        if (existUser.LeftDays > 0)
                        {
                            existUser.EndDate = DateTime.Now.AddDays(existUser.LeftDays + 1);
                        }
                    }
                    premiumusers.Update(existUser);
                }
                else
                {
                    SpenderUser newSpenderUserObject = new SpenderUser
                    {
                        Username = newSpenderUser.Name,
                        SteamId = newSpenderUser.SteamId,
                        SlgId = newSpenderUser.SlgId,
                        Patreon = wert
                    };
                    premiumusers.Insert(newSpenderUserObject);
                    ChatManager.ServerMessageToPlayer(new LocString("Der Spieler mit der ID " + newSpenderUser.Id + " und dem Namen " + newSpenderUser.Name + " wurde mit Patreon" + wert + "angelegt!"), runningUser, false, DefaultChatTags.Notifications, ChatCategory.Info);
                }
            }
        }

        // Die Methode gibt einen String zurück der im Spiel in einem Infofenster angezeigt wird
        public static string ShowPremiumUser()
        {
            using (var db = new LiteDatabase(@"GBSpender.db"))
            {
                var spenderUsers = db.GetCollection<SpenderUser>("SpenderUser");
                var result = spenderUsers.FindAll().OrderBy(x => x.Username);
                string einruecken = "\t";
                var premiumUserList = new StringBuilder().AppendLine(TextLoc.InfoLocStr("Name" + einruecken + "Mitglied bis:" + einruecken + "Patreon:"));
                premiumUserList.AppendLine();
                foreach (SpenderUser spenderUser in result)
                {
                    string datumString;
                    if (spenderUser.EndDate >= DateTime.Now)
                        datumString = "<color=green>" + spenderUser.EndDate.ToShortDateString() + "</color>";
                    else
                        datumString = "<color=red>" + spenderUser.EndDate.ToShortDateString() + "</color>";
                    premiumUserList.AppendLine(TextLoc.InfoLocStr("<color=white>" + spenderUser.Username + "</color>" + einruecken + datumString + einruecken + spenderUser.Patreon.ToString()));
                };
                return premiumUserList.ToString();
            }
        }

        // Alle 30,5 Tage bekommt jeder Spieler eine Goldene Bärenstatue die einen Spielvortei bewirkt, 
        // um diesen Vorteil bei einem neuen Speicherstand direkt zu haben wird das LastBear Feld zurückgesetzt.
        public static void ResetBear(User user)
        {
            using (var db = new LiteDatabase(@"GBSpender.db"))
            {
                var premiumusers = db.GetCollection<SpenderUser>("SpenderUser");
                var allSpender = premiumusers.FindAll();
                foreach (var spender in allSpender)
                {
                    spender.LastBear = DateTime.MinValue;
                    premiumusers.Update(spender);
                }
                ChatManager.ServerMessageToPlayer(new LocString("Alle Bären zurückgesetzt"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
            }
        }

        // Bei veränderung der SpenderUser Klasse wird diese Methode benutzt um die bestehnde Datenbank auf den neusten Stand zu bringen.
        public static void UpdateDatabase()
        {
            using (var db = new LiteDatabase(@"GBSpender.db"))
            {
                var premiumusers = db.GetCollection<SpenderUser>("SpenderUser");
                var allSpender = premiumusers.FindAll();
                foreach (SpenderUser spender in allSpender)
                { 
                    SpenderUser newSpenderUser = new SpenderUser
                    {
                        Username = spender.Username,
                        SteamId = spender.SteamId,
                        SlgId = spender.SlgId,
                        EndDate = spender.EndDate,
                        LastGiftDay = spender.LastGiftDay,
                        LastBear = spender.LastBear
                    };
                    premiumusers.Insert(newSpenderUser);
                    Console.WriteLine("Neu angelegt: "+ newSpenderUser.Username);
                    premiumusers.Delete(spender.ID);
                    Console.WriteLine("Gelöscht: "+ spender.Username);
                }
            }
        }
    }
}
