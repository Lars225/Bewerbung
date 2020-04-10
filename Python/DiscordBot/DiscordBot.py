import asyncio

import motor
import discord

import Commands
import Database

import SECRET
import Tools

global firstStart
firstStart = True

client = discord.Client()


@client.event
async def on_ready():
    global firstStart
    print("Wir sind eingeloggt als User {}".format(client.user))
    # await client.change_presence(activity=discord.Game("Work in Progress!"), status=discord.Status.online)
    if firstStart:
        await Database.init(loop=client.loop)
        client.loop.create_task(Tools.status_task(client))
        client.loop.create_task(Tools.RecieveMessages(client))
        Commands.client = client
        firstStart = False


# Event reagiert auf Nachrichten die überall auf dem Discord Server geschrieben werden
@client.event
async def on_message(message):
    # Bot Nachrichten werden ignoriert
    if message.author.bot:
        return
    # Falls die Nachricht mit ! beginnt wird sie als Command behandelt
    elif message.content.startswith('!'):
        await Commands.startCommand(message)
    # Falls die Nachricht in dem Channel mit der angegebenen Id ist
    elif message.channel.id == 695958163366871081:
        if message.content.startswith('!'):
            await Commands.startCommand(message)
        else:
            await Database.mdb.EcoChat.DiscordMessages.insert_one({"Author": message.author.display_name,
                                                                   "Message": message.content})
    # Nachrichten in dem Cahnnel mit der Id werden in die Datenbank gespeichert und Später als Status für den Bot verwendet
    elif message.channel.id == 696846184085454959:
        await Database.mdb.EcoChat.Status.insert_one({"Author": message.author.display_name,
                                                      "Message": message.content})
        await message.delete()
        await message.channel.send(
            "{} danke für deine Status Einsendung! Der Status wurde gespeichert!".format(message.author.mention))
        # Es wir eine Eingebette Nachricht erstellt mit den Infos über die Einsendung
        embedmessage = discord.Embed(title="Danke {} für deine Statuseisnendung!".format(message.author),
                                     # description="Danke {} für deine Statuseisnendung!".format(message.author),
                                     color=discord.Color.green())
        embedmessage.add_field(name="Status:", value=message.content,
                               inline=True)
        dm = await message.author.create_dm()
        await dm.send(embed=embedmessage)
    else:
        return

    # roles = discord.utils.get(after.guild.roles, name="Spieler")


client.run(SECRET.TOKEN)

# spielchat.send("{}: {}".format(change.get("fullDocument", {}).get("Author"), change.get("fullDocument",
# {}).get("Message")))

# spielchat = client.get_channel(690291237106090046)
# cursor = mongoClient.EcoChat.ServerMessages.watch(full_document='updateLookup')
# while True:
# document = next(cursor)
# if document is not None:
# await spielchat.send(
# "{}: {}".format(document.get("fullDocument", {}).get("Author"), document.get("fullDocument", {}).get("Message")))
# await asyncio.sleep(1)

# Statuswechsel
