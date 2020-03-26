import discord
from discord import Member, Role

import Database
import SECRET
import Commands
import Tools

client = discord.Client()

all_commands = {}


@client.event
async def on_ready():
    print("Wir sind eingeloggt als User {}".format(client.user))
    await client.change_presence(activity=discord.Game("Work in Progress!"), status=discord.Status.online)
    await Database.init(loop=client.loop)
    # client.loop.create_task(status_task())
    client.loop.create_task(RecieveMessages())

# Das Event reagiert auf gesendete Nachrichten auf dem Discord Server
@client.event
async def on_message(message):
    if message.author.bot:
        return
    # Die Nachrichten werden nach Commands die mit ! beginnen gefiltert.
    elif message.content.startswith('!'):
        Tools.call_cmd(message.content, message)
    # Wenn eine Nachricht in den Channel mit der Id 564183701219442688 gesendet wird, 
    # wird die Nachricht in die MongoDB und durch eine Modifikation in den Spiel Chat auf dem GameServer übertragen
    elif message.channel.id == 564183701219442688:
        await Database.mdb.EcoChat.DiscordMessages.insert_one({"Author": message.author.display_name,
                                                               "Message": message.content})
    else:
        return


# Die mongoDb wird auf neue Einträge die von dem GameServer kommen überprüft und in den Channel mit der Id 564183701219442688 übertragen.
async def RecieveMessages():
    spielchat = client.get_channel(564183701219442688)
    async with Database.mdb.EcoChat.ServerMessages.watch(full_document='updateLookup') as change_stream:
        async for change in change_stream:
            doc = change.get("fullDocument")
            if doc:
                author = doc.get("Author")
                message = doc.get("Message")
                if author and message:
                    await spielchat.send(f"{author}: {message}")


client.run(SECRET.TOKEN)
