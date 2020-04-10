import asyncio
import random

import discord

import Database


# Die Datenbank wird auf veränderungen überprüft und sendet diese in den Channel mit der angegebenen Id
async def RecieveMessages(client):
    spielchat = client.get_channel(695958163366871081)
    async with Database.mdb.EcoChat.ServerMessages.watch(full_document='updateLookup') as change_stream:
        async for change in change_stream:
            doc = change.get("fullDocument")
            if doc:
                author = doc.get("Author")
                message = doc.get("Message")
                if author and message:
                    await spielchat.send(f"{author}: {message}")


# Alle 2 Stunden wird die Datenbank abgefragt, ein zufälliger Status ausgewählt und im DiscordBot aktualisiert
async def status_task(client):
    while True:
        status = await Database.mdb.EcoChat.checkedStatus.find().to_list(None)
        listsize = len(status) - 1
        zufallszahl = random.randrange(0, listsize)
        message = status[zufallszahl].get("Message")
        await client.change_presence(activity=discord.Game(message), status=discord.Status.online)
        await asyncio.sleep(7200)
