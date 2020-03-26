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


@client.event
async def on_message(message):
    if message.author.bot:
        return
    elif message.content.startswith('!'):
        Tools.call_cmd(message.content, message)
    elif message.channel.id == 564183701219442688:
        await Database.mdb.EcoChat.DiscordMessages.insert_one({"Author": message.author.display_name,
                                                               "Message": message.content})
    else:
        return


# roles = discord.utils.get(after.guild.roles, name="Spieler")


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
# async def status_task():
#    while True:
#        await client.change_presence(activity=discord.Game("Status 1!"), status=discord.Status.online)
#        await asyncio.sleep(10)
#        await client.change_presence(activity=discord.Game("Status 2!"), status=discord.Status.online)
#        await asyncio.sleep(10)


client.run(SECRET.TOKEN)
