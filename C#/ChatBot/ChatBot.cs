using Asphalt.Api.Event;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.Players;
using MongoDB.Driver;
using System;
using System.Threading;

namespace GBChatBot
{
    // Die Modifikation dient zur Synchronisation zwischen Gameserver und Discord und dem Speichern des Chat Log in einer Datenbank.
    class ChatBot : IModKitPlugin, IInitializablePlugin, IServerPlugin
    {
        public static MongoClient mongoClient { get; set; }
        public void Initialize(TimedTask timer)
        {   
            try
            {
                mongoClient = new MongoClient("MongoDBConnectionString");
                Console.WriteLine("DB Connected");
                EventManager.RegisterListener((object)new MessageListener());
                Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]" + " Chat Listener aktiv", Console.ForegroundColor = ConsoleColor.Green);
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now.ToShortTimeString() + e);
            }
             
        Console.WriteLine("Lade jetzt Message Reciver");
            new Thread((ThreadStart)(() =>
            {
                MessageReciever.RecieveMessages(mongoClient);
            })).Start();
        }

        public string GetStatus()
        {
            return string.Empty;
        } 
    }
   
}
