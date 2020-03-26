using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using Eco.Shared.Services;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;

namespace GBChatBot
{
    // Die MongoDB wird in Echtzeit auf neue Einträge geprüft.
    // Der DiscordBot speichert alle Nachrichten von dem DiscordServer in der DiscordMessages Collection.
    // Bei einem neuen Eintrag wird die Nachricht in den Ingame Chat übertragen.
    class MessageReciever
    {
        public static void RecieveMessages(MongoClient client)
        {
            try
            {
                var db = client.GetDatabase("EcoChat");
                var col = db.GetCollection<DiscordMessage>("DiscordMessages");
                var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
                var cursor = col.Watch(options);
                Console.WriteLine("Message Reciever geladen");
                while (true)
                {
                    while (cursor.MoveNext() && cursor.Current.Count() == 0)
                    {
                        Thread.Sleep(1000);
                    }
                    User ecoUser = UserManager.GetOrCreateUser("GummiBaerenChatBot", "GummiBaerenChatBot", "Discord Bot");
                    var next = cursor.Current.First();
                    DiscordMessage message = next.FullDocument;
                    string ingameMessage = "<color=orange>" + message.Author + ": </color> " + message.Message;
                    ChatManager.SendChat(ingameMessage, ecoUser);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(180000);
                RecieveMessages(client);
            }
        }
    }
}
