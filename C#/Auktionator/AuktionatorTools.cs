using Eco.Core.Localization;
using Eco.Core.Plugins.Interfaces;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Items;
using Eco.Gameplay.LegislationSystem.Demographics;
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
    class AuktionatorTools : IModKitPlugin, IServerPlugin
    {
        public static User Auktionator { get; set; }
        public static Currency BaerenTaler { get; set; }

        // Bei Starten des Servers werden der User "Auktionator" und die Währung "BärenTaler" ermittelt.
        public void Initialize()
        {
            Auktionator = UserManager.GetOrCreateUser(null, null, "Auktionator");
            BaerenTaler = CurrencyManager.Obj.GetClosestCurrency("Bärentaler");
            ChatManager.ServerMessageToAll(new LocString("Auktionator erfolgreich geladen"), false, DefaultChatTags.Notifications, ChatCategory.Info);
        }

        /* 
        Die Methoden werden durch einen Command in den SpielChat aufgerufen.
        */
        public static void AddAuktion(User user, string itemName, float anzahl, string beschreibung, float startpreis, float laufzeit)
        {
            double gerundeteAnzahl = Math.Round((double)anzahl, 0, MidpointRounding.ToEven);

            // Nur Spielern mit einer Gesamtspielzeit von mindestens 5 Stunden und Administratoren ist es gestattet eine Auktion zu erstellen
            if (user.TotalPlayTime > 5 || user.IsAdmin)
            {

                if (laufzeit >= 1 && gerundeteAnzahl >= 1)
                {
                    Item item = Item.GetItemByString(user, itemName);
                    ItemStack itemStack = new ItemStack(item, (int)Math.Round((double)anzahl, 0, MidpointRounding.ToEven));
                    List<ItemStack> istacks = new List<ItemStack>();
                    istacks.Add(itemStack);


                    if (user.Inventory.Contains(istacks))
                    {
                        // Die LiteDB wird aufgerufen und die Auktion in der Datenbank hinterlegt.
                        using (var db = new LiteDatabase(@"GB.db"))
                        {
                            var auktionen = db.GetCollection<AuktionsAngebot>("Auktionen");
                            var anzahlAuktionen = auktionen.Find(x => x.Ersteller.Equals(user));

                            if (anzahlAuktionen.Count() < 2)
                            {
                                user.Inventory.TryRemoveItems(itemStack, user);
                                AuktionsAngebot auktionsAngebot = new AuktionsAngebot
                                {
                                    AngebotsItem = itemStack,
                                    Beschreibung = beschreibung,
                                    Gebot = Math.Round(startpreis, 0, MidpointRounding.ToEven),
                                    Ersteller = user,
                                    Startzeit = DateTime.Now,
                                    Endzeit = DateTime.Now.AddHours(Math.Round((double)laufzeit, 0, MidpointRounding.ToEven)),
                                    GeldZugeteilt = false,
                                    ItemAbgeholt = false
                                };
                                auktionen.Insert(auktionsAngebot);
                                ChatManager.ServerMessageToPlayer(new LocString("<color=green>Die Auktion wurde erfolgreich erstellt!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                            }
                            else
                            {
                                ChatManager.ServerMessageToPlayer(new LocString("<color=red>Es sind Maximal 2 Auktionen pro Spieler erlaubt!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                            }

                        }
                    }
                    else
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("<color=red>Die Laufzeit und Anzahl muss mindestens 1 sein!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                    }
                }


                else
                {
                    ChatManager.ServerMessageToPlayer(new LocString("<color=red>Du hast noch nicht lange genug auf diesem Server gspielt um diese Funktion zu nutzen!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                }
            }
            else
            {
                ChatManager.ServerMessageToPlayer(new LocString("<color=red>Entweder hast du das Item das du verkaufen möchtest nicht im Inventar oder du hast den Namen falsch eingetragen!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
            }
        }

        public static void RemoveAuktion(User user, int auktionsid)
        {
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var auktionen = db.GetCollection<AuktionsAngebot>("Auktionen");
                var auktion = auktionen.FindOne(x => x.ID.Equals(auktionsid));
                if (auktion != null) {
                    if (auktion.Ersteller.Equals(user) && auktion.Startzeit.Subtract(DateTime.Now).TotalMinutes < 10 || user.IsAdmin)
                    {
                        auktionen.Delete(auktion.ID);
                        ChatManager.ServerMessageToPlayer(new LocString("<color=green>Die Auktion wurde erfolgreich gelöscht!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                    }
                    else
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("<color=red>Du musst der Ersteller sein und die Auktion darf nicht älter wie 10 Minuten sein!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                    }
                }
                else
                {
                    ChatManager.ServerMessageToPlayer(new LocString("<color=red>Auktion wurde nicht gefunden!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                }
            }
        }

        public static void Bieten(User user,int auktionsid,float gebot)
        {
            double gerundetesGebot = Math.Round((double)gebot, 0, MidpointRounding.ToEven);
            if (gerundetesGebot >= 1)
                using (var db = new LiteDatabase(@"GB.db"))
                {
                    var auktionen = db.GetCollection<AuktionsAngebot>("Auktionen");
                    var auktion = auktionen.FindOne(x => x.ID.Equals(auktionsid));
                    if (auktion != null)
                    {
                        if (gerundetesGebot > auktion.Gebot && auktion.Ersteller != user || user.IsAdmin)
                        {
                            try
                            {

                                BankAccountManager.Obj.Transfer(user.Player, user.BankAccount, BankAccountManager.Obj.Treasury(), BaerenTaler, (float)gerundetesGebot);
                                BankAccountManager.Obj.Transfer(Auktionator.Player, BankAccountManager.Obj.Treasury(), auktion.Hoechstbietender.BankAccount, BaerenTaler, (float)auktion.Gebot);
                            }
                            catch (Exception)
                            {
                                ChatManager.ServerMessageToPlayer(new LocString("<color=red>Du hast nicht genug Bären Taler!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                            }
                            auktion.Gebot = gerundetesGebot;
                            auktion.Hoechstbietender = user;
                            auktionen.Update(auktion);
                            ChatManager.ServerMessageToPlayer(new LocString("<color=green>Das Gebot auf die Auktion mit der Id: " + auktion.ID + " wurde akzeptiert!"), user, false, DefaultChatTags.Notifications, ChatCategory.Info);
                        }
                        else
                        {
                            ChatManager.ServerMessageToPlayer(new LocString("<color=red>Das Gebot muss höher sein als das Höchstgebot!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                        }
                    }
                    else
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("<color=red>Auktion wurde nicht gefunden!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                    }
                }
            else
            {
                ChatManager.ServerMessageToPlayer(new LocString("<color=red>Das Gebot muss 1 oder höher sein!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
            }
        }

        // Die Methode gibt einen String zurück, der dann in einem Infofenster im Spiel angezeigt wird.
        public static string ShowAuktionen()
        {
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var auktionen = db.GetCollection<AuktionsAngebot>("Auktionen");
                var result = auktionen.FindAll();

                string einruecken = "\t";
                string color = "white";
                float restZeit;
                var auktionenListe = new StringBuilder().AppendLine(TextLoc.InfoLocStr("Id" + einruecken + "Name" + einruecken + "Höchstgebot" + einruecken + "Endet in"));
                auktionenListe.AppendLine();
                foreach (AuktionsAngebot auktionsAngebot in result)
                {
                    restZeit = auktionsAngebot.Endzeit.Subtract(DateTime.Now).Minutes;
                    if (restZeit <= 30)
                        color = "red";
                    else if (restZeit <= 120)
                        color = "orange";
                    if (restZeit <= 0)
                    {
                    auktionenListe.AppendLine(TextLoc.InfoLocStr("<color=" + color + ">" + auktionsAngebot.ID + einruecken + auktionsAngebot.AngebotsItem.Item.DisplayName + einruecken + auktionsAngebot.Gebot + einruecken + " Beendet"));
                    }
                    else
                    {
                        auktionenListe.AppendLine(TextLoc.InfoLocStr("<color=" + color + ">" + auktionsAngebot.ID + einruecken + auktionsAngebot.AngebotsItem.Item.DisplayName + einruecken + auktionsAngebot.Gebot + einruecken + restZeit + " Minuten"));
                    }
                };
                return auktionenListe.ToString();
            }
        }

        public static void ItemAbholen (User user, int id)
        {
            using (var db = new LiteDatabase(@"GB.db"))
            {
                var auktionen = db.GetCollection<AuktionsAngebot>("Auktionen");
                var auktion = auktionen.FindOne(x => x.ID.Equals(id));
                if (auktion != null)
                {
                    if (auktion.Hoechstbietender.Equals(user))
                    {
                        if (!auktion.ItemAbgeholt)
                        {
                            try
                            {
                                user.Inventory.TryAddItems(auktion.AngebotsItem.Item.Type, auktion.AngebotsItem.Quantity, user);
                                auktion.ItemAbgeholt = true;
                            }
                            catch
                            {
                                ChatManager.ServerMessageToPlayer(new LocString("<color=red>Es ist etwas schief gelaufen Kontaktiere einen Admin!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                            }
                            
                            ChatManager.ServerMessageToPlayer(new LocString("<color=green>Danke für die nutzung des Auktionssystems! Du hast " + auktion.AngebotsItem.Quantity + " " + auktion.AngebotsItem.Item.DisplayName + " erfolgreich für " + auktion.Gebot + " ersteigert und in dein Inventar bekommen!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);

                            if (!auktion.GeldZugeteilt)
                            {
                                try
                                {
                                    BankAccountManager.Obj.Transfer(Auktionator.Player, BankAccountManager.Obj.Treasury(), auktion.Ersteller.BankAccount, BaerenTaler, (float)auktion.Gebot);
                                    auktion.GeldZugeteilt = true;
                                }
                                catch
                                {
                                    ChatManager.ServerMessageToPlayer(new LocString("<color=red>Das Geld wurde bereits zugeteilt!Kontaktiere einen Admin!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                                }

                            }
                            else
                            {
                                ChatManager.ServerMessageToPlayer(new LocString("<color=green>Die Bärentaler wurden " + auktion.Ersteller.Name + " zugeteilt!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                            }

                        }
                        else
                        {
                            ChatManager.ServerMessageToPlayer(new LocString("<color=red>Das Item wurde bereits abgeholt! Falls du es nicht warst Kontaktiere einen Admin!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                        }
                        
                    }
                    else
                    {
                        ChatManager.ServerMessageToPlayer(new LocString("<color=red>Nur der Höchstbietende kann das Item abholen!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                    }
                }
                else
                {
                    ChatManager.ServerMessageToPlayer(new LocString("<color=red>Auktion nicht gefunden!"), user, false, DefaultChatTags.Notifications, ChatCategory.Error);
                }
            }
        }

        public string GetStatus()
        {
            return null;
        }
    }
}
