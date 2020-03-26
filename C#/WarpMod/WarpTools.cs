using Eco.Core.Localization;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using Eco.Shared.Math;
using Eco.Shared.Services;
using Eco.Shared.Utils;
using LiteDB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eco.Mods.GBMods
{
    // Die Modifikation ermöglicht den Spielern sich zu vorher festgelegten Punkten auf der Spielwelt zu teleportieren
    class WarpTools
    {
        public static Dictionary<int, DateTime> timeLimit = new Dictionary<int, DateTime>();       
        public static void SavePoint(User user, string warpName)
        {

            // Bei jedem erstellen eines Warppunkt wird eine Rolle benötigt.
            WarpPunktRolleItem warpPunktItem = new WarpPunktRolleItem();
            ItemStack istack = new ItemStack(warpPunktItem, 1, null);
            List<ItemStack> itemStacks = new List<ItemStack>();
            itemStacks.Add(istack);
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var warps = db.GetCollection<WarpPoint>("warps");
                WarpPoint warpPoint = new WarpPoint
                {
                    Creator = user.Name,
                    WarpName = warpName,
                    WarpPosX = user.Position.x,
                    WarpPosY = user.Position.y + 1f,
                    WarpPosZ = user.Position.z,
                    TimeAndDate = DateTime.Now,

                };
                var existName = warps.FindOne(x => x.WarpName.Contains(warpName));
                if (existName == null)
                {
                    if (user.Inventory.Contains(itemStacks))
                    {
                        try
                        {
                            user.Inventory.TryRemoveItem(warpPunktItem.GetType(), user);
                            warps.Insert(warpPoint);
                            ChatManager.ServerMessageToAll("<color=green>" + warpPoint.Creator + " hat den Warppunkt " + warpPoint.WarpName + " erstellt!", true, DefaultChatTags.Notifications, ChatCategory.Info,null);
                        }
                        catch (Exception)
                        {
                            AdvertError(user, warpPoint.WarpName);
                            throw;
                        }
                        AdvertSuccess(user, warpPoint.WarpName);
                    }
                    else
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("<color=red>Du benötigst eine Warp Punkt Rolle um einen Warppunkt zu erstellen, diese kannst du bei einem Magier kaufen!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                    }
                }
                else
                {
                    ChatManager.ServerMessageToPlayer(new LocString("<color=red>Der Warp mit dem Namen " + warpName + " existiert bereits!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                }
            }
        }


        public static void Teleport(User user, string warpName)
        {
            bool found = false;
            float warpPosX;
            float warpPosY;
            float warpPosZ;

            if (!timeLimit.ContainsKey(user.Id))
            {
                timeLimit.Add(user.Id, DateTime.MinValue);
            }
            WarpRolleItem warpRolleItem = new WarpRolleItem();
            ItemStack istack = new ItemStack(warpRolleItem, 1, null);
            List<ItemStack> itemStacks = new List<ItemStack>();
            itemStacks.Add(istack);
            if (user.Inventory.Contains(itemStacks))
            {
                // Um einer Mutwilligen Überlastung des Servers vorzubeugen, wird ein Zeitlimit festgelegt.
                if ( DateTime.Now.Subtract(timeLimit[user.Id]).TotalSeconds > 10)
                {
                    using (var db = new LiteDatabase(@"GB.db"))
                    {
                        var warps = db.GetCollection<WarpPoint>("warps");
                        var warpTrackerCol = db.GetCollection<WarpTracker>("warptracker");

                        var result = warps.FindOne(x => x.WarpName.Contains(warpName));
                        if (result != null)
                        {
                            warpPosX = result.WarpPosX;
                            warpPosY = result.WarpPosY;
                            warpPosZ = result.WarpPosZ;
                            found = true;

                            var exisUser = warpTrackerCol.FindOne(x => x.UserId.Equals(user.Id));
                            if (exisUser != null)
                            {
                                exisUser.WarpAmount = exisUser.WarpAmount + 1;       
                                warpTrackerCol.Update(exisUser);
                            }
                            else
                            {
                                WarpTracker warpTracker = new WarpTracker()
                                {
                                    UserId = user.Id,
                                    UserName = user.Name,
                                    WarpAmount = 1,
                                };
                                warpTrackerCol.Insert(warpTracker);
                            }
                        }
                        else
                        {
                            AdvertNotFound(user, warpName);
                            found = false;
                            return;
                        }
                    }

                    if (found)
                    {
                        user.Inventory.TryRemoveItem(warpRolleItem.GetType(), user);
                        Vector3 warpPos = new Vector3(warpPosX, warpPosY, warpPosZ);
                        user.Player.SetPosition(warpPos);
                        timeLimit.Remove(user.Id);
                        timeLimit.Add(user.Id, DateTime.Now);

                        
                        
                    }
                }
                else
                {
                    double remainTime = DateTime.Now.Subtract(timeLimit[user.Id]).TotalSeconds - 10;
                    double countdown = Math.Round(remainTime * -1);

                    ChatManager.ServerMessageToPlayer(new LocString("<color=red>Du kannst in " + countdown.ToString() + "s wieder Warpen"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                }
            }
            else
            {
                ChatManager.ServerMessageToPlayer(new LocString("<color=red>Du musst eine Warp Rolle im Inventar haben um zu warpen!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
            }
        }


        public static void RemovePoint(User user, string warpName)
        {
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var warps = db.GetCollection<WarpPoint>("warps");
                var result = warps.FindOne(x => x.WarpName.Contains(warpName));
                if (result != null)
                {
                    if (result.Creator == user.Name || user.IsAdmin)
                    {
                        if (DateTime.Now.Subtract(result.TimeAndDate).TotalSeconds < 45)
                        {
                            WarpPunktRolleItem warpPunktItem = new WarpPunktRolleItem();
                            user.Inventory.TryAddItem(warpPunktItem, user);
                        }
                        warps.Delete(result.ID);
                        ChatManager.ServerMessageToPlayer(new LocString("<color=green>Der Warp " + warpName + " wurde erfolgrei gelöscht!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                    }
                    else
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("<color=red>Nur der ersteller kann den WarpPoint löschen!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                        return;
                    }
                }
                else
                {
                    AdvertNotFound(user, warpName);
                    return;
                }
            }
        }

        // Gibt einen string zurück der alle bisher gespeicherten Warppunkte in einem Infofenster auflistet
        public static string ShowWarps() //User user
        {
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var warps = db.GetCollection<WarpPoint>("warps");
                var result = warps.FindAll().OrderBy(x => x.WarpName);
                string einruecken = "\t";
                var warpList = new StringBuilder().AppendLine(TextLoc.InfoLocStr("Name" + einruecken + "Ersteller" + einruecken + "Koordinaten"));
                warpList.AppendLine();
                foreach (WarpPoint warppoint in result)
                {
                    double posX = Math.Round(Convert.ToDouble(warppoint.WarpPosX));
                    double posY = Math.Round(Convert.ToDouble(warppoint.WarpPosY));
                    double posZ = Math.Round(Convert.ToDouble(warppoint.WarpPosZ));
                    warpList.AppendLine(TextLoc.InfoLocStr("<color=white>" + warppoint.WarpName + einruecken + warppoint.Creator + einruecken + posX.ToString() + " , " + posY.ToString() + " , " + posZ.ToString()));
                };
                return warpList.ToString();
            }
        }

        public static void SearchWarp(User user, string searchKey)
        {
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var warps = db.GetCollection<WarpPoint>("warps");
                var resultName = warps.Find(x => x.WarpName.Contains(searchKey));
                var resultCreator = warps.Find(x => x.Creator.Contains(searchKey));

                if (resultName != null || resultCreator != null)
                {
                    var searchResult = new StringBuilder().AppendLine(TextLoc.HeaderLocStr("Suchergebnis:"));
                    if (resultName != null)
                    {
                        searchResult.AppendLine(resultName.Select(x => $"{x.WarpName} - {x.Creator}").NewlineList());
                    }
                    if (resultCreator != null)
                    {
                        searchResult.AppendLine(resultCreator.Select(x => $"{x.WarpName} - {x.Creator}").NewlineList());
                    }
                    ChatManager.ServerMessageToPlayer(new LocString(searchResult.ToString()), user, false);
                }
                else
                {
                    ChatManager.ServerMessageToPlayer(new LocString("<color=red>Es konnten keine Warps gefunden werden!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                }
            }
        }

        public static string WarpTrackerString()
        {
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var warpTrackerCol = db.GetCollection<WarpTracker>("warptracker");
                var warpTracker = warpTrackerCol.FindAll().OrderBy(x => x.UserName);

                string einruecken = "\t";
                var warpTrackerList = new StringBuilder().AppendLine(TextLoc.InfoLocStr("User Name" + einruecken + "WarpAmount"));
                warpTrackerList.AppendLine();
                foreach (WarpTracker warptracker in warpTracker)
                {
                    warpTrackerList.AppendLine(warptracker.UserName + einruecken + warptracker.WarpAmount);
                }
                return warpTrackerList.ToString();
            }
        }
        
        public static void AdvertNotFound(User userByName, string warpName)
        {
            ChatManager.ServerMessageToPlayer(new LocString("<color=red>Der Warp mit dem Namen " + warpName + " wurde nicht gefunden!"), userByName, false, DefaultChatTags.Notifications, ChatCategory.Info);
        }

        public static void AdvertError(User userByName, string warpName)
        {
            ChatManager.ServerMessageToPlayer(new LocString("<color=red>Du hast bereits einen Warp mit dem Namen " + warpName + " gesetzt lösche diesen erst mit /warpdel " + warpName + "!"), userByName, false, DefaultChatTags.Notifications, ChatCategory.Info);
        }

        public static void AdvertSuccess(User userByName, string warpName)
        {
            ChatManager.ServerMessageToPlayer(new LocString("<color=green>Der Warp mit dem Namen " + warpName + " wurde gespeichert!"), userByName, false, DefaultChatTags.Notifications, ChatCategory.Info);
        }
    }
}

