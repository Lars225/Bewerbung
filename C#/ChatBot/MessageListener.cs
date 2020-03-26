using Asphalt.Api.Event;
using Asphalt.Api.Event.PlayerEvents;
using System;

namespace GBChatBot
{
    // Der MessageListener reagiert auf das SendMessage Event.
    // Die Nachrichten werden in der ServerMessages Collection abgelegt wo sie vom DiscordBot abgefragt und in einen speziellen Channel gesendet.
    class MessageListener
    {
        [EventHandler(EventPriority.Normal, RunIfEventCancelled = false)]
        public void ListenMessages(PlayerSendMessageEvent evt)
        {
            var db = ChatBot.mongoClient.GetDatabase("EcoChat");
            var col = db.GetCollection<ServerMessage>("ServerMessages");
            
            // Private Nachrichten zwischen Spielern werden ignoriert
            if (!evt.Message.Priv && evt.User.Name != "Discord Bot")
            {
                ServerMessage message = new ServerMessage
                {
                    Author = evt.User.Name,
                    Message = evt.Message.Text
                };
                try
                {
                    col.InsertOne(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else return;
        }
    }
}
