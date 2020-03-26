using Asphalt.Api.Event;
using Asphalt.Api.Event.PlayerEvents;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Mods.TechTree;
using Eco.Shared.Localization;
using Eco.Shared.Services;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eco.Mods.GBMods.Spender
{
    public class OnLoginHandler
    {
        private string directory = Directory.GetCurrentDirectory() + "\\Mods\\GBMods\\Users.txt";

        // Die Methode wird bei jedem Login eines Spielers auf dem GameServer aufgerufen.
        [EventHandler(EventPriority.High, RunIfEventCancelled = true)]
        public void UserLogin(PlayerLoginEvent loginevent)
        {
            new Thread((ThreadStart)(() => 
            {
                User user = loginevent.Player.User;
                Thread.Sleep(20000);
                CheckPlayer(user);
                using (var db = new LiteDatabase(@"GBSpender.db"))
                {
                    SpenderUser existUser;
                    var spenderUsers = db.GetCollection<SpenderUser>("SpenderUser");
                    if (user.SteamId != null)     
                        existUser = spenderUsers.FindOne(x => x.SteamId.Contains(user.SteamId));
                    else
                        existUser = spenderUsers.FindOne(x => x.SteamId.Contains(user.SlgId));
                    if (existUser != null && (existUser.EndDate.Subtract(DateTime.Now).Days >= 0 || existUser.Patreon == true))
                    {
                        User savedUser;
                        if (user.SteamId != null)
                            savedUser = UserManager.FindUserBySteamId(existUser.SteamId);
                        else
                            savedUser = UserManager.FindUserBySlgId(existUser.SlgId);
                        ChatManager.ServerMessageToPlayer(new LocString("Willkommen Spender!"), savedUser, false, DefaultChatTags.Notifications, ChatCategory.Info);
                        ChatManager.ServerMessageToPlayer(new LocString("Deine Spender Mitgliedschaft ist noch bis zum " + existUser.EndDate.ToShortDateString() + " gültig!"), savedUser, false, DefaultChatTags.Notifications, ChatCategory.Info);
                        if (existUser.LastGiftDay != DateTime.Now.Day)
                        {
                            GiftItem giftItem = new GiftItem();
                            try
                            {
                                savedUser.Inventory.AddItem(giftItem);
                            }
                            catch (Exception)
                            {
                                ChatManager.ServerMessageToPlayer(new LocString("Fehler beim hinzufügen"), savedUser, false, DefaultChatTags.Notifications, ChatCategory.Error);
                            }
                            existUser.LastGiftDay = DateTime.Now.Day;
                            spenderUsers.Update(existUser);
                        }
                        if (existUser.LastBear == null || DateTime.Now.Subtract(existUser.LastBear).Days >= 30.5)
                        {
                            BonusBaerGoldItem bonusBaerGoldItem = new BonusBaerGoldItem();
                            try
                            {
                                savedUser.Inventory.AddItem(bonusBaerGoldItem);
                                ChatManager.ServerMessageToPlayer(new LocString("Dein Monatlicher Goldener Bonusbär wurde deinem Inventar hinzugefügt!"), savedUser, false, DefaultChatTags.Notifications, ChatCategory.Info);
                            }
                            catch (Exception)
                            {
                                ChatManager.ServerMessageToPlayer(new LocString("Fehler beim hinzufügen des Bonus Bär Gold"), savedUser, false, DefaultChatTags.Notifications, ChatCategory.Error);
                            }
                            existUser.LastBear = DateTime.Now;
                            spenderUsers.Update(existUser);
                        }


                    }
                    else
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("Willkommen im GummiBärenLand!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                    }
                }
            })).Start();
        }

        private void CheckPlayer(User userByName)
        {
            if (File.Exists(this.directory))
            {
                if (DateTime.Compare(DateTime.Parse(File.ReadAllLines(directory)[0]).AddDays(1.0), DateTime.Now) < 0)
                    ResetFile();
            }
            else
                this.ResetFile();
            if (((IEnumerable<string>)this.ReadPlayers()).Contains<string>(userByName.Name))
                return;
            this.AddPlayer(userByName.Name);
            this.RewardPlayer(userByName);
        }

        private void AddPlayer(string name)
        {
            File.AppendAllLines(this.directory, (IEnumerable<string>)new string[1]
            {
        name
            });
        }

        private void RewardPlayer(User player)
        {
            player.Inventory.TryAddItem(Item.GetItemByString(player, "BaerenSaftItem"), (User)null);
            ChatManager.ServerMessageToPlayer("Dein täglicher Loginbonus!", player, true, DefaultChatTags.Notifications, ChatCategory.Info);
        }

        private string[] ReadPlayers()
        {
            if (File.Exists(this.directory))
                return File.ReadAllLines(this.directory);
            this.ResetFile();
            return new string[0];
        }

        private void ResetFile()
        {
            if (File.Exists(directory))
                File.Delete(directory);
            File.WriteAllLines(directory, new string[1]
            {
            DateTime.Now.ToString()
            });
        }
    }
}

