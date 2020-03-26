using MongoDB.Bson;

namespace GBChatBot
{
    class ServerMessage
    {
        public ObjectId id { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
    }

    class DiscordMessage
    {
        public ObjectId id { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
    }
}
