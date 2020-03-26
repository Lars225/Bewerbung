import discord
from discord import Member

import Database
import Tools


@Tools.cmd("!linkslg")
async def LinKSlg(args, msg):
    existuser = Database.mdb.EcoChat.DiscordUsers.Find({"DiscordId": msg.author.id})
    slgid = args[0]
    if existuser is not None and existuser["SlgId"] is not slgid:
        Database.mdb.EcoChat.DiscordUsers.update_one(
            {"DiscordId": msg.author.id},
            {"$set": {"SlgId": slgid},
             "$currentDate": {"lastModified": True}})
        await msg.author.send(content="Deine SlgId wurde aktualisiert")
    elif existuser is not None and existuser["SlgId"] is slgid:
        await msg.author.send(content="Du hast deine Slg Id bereits verlinkt")
    else:
        await Database.mdb.EcoChat.DiscordMessages.insert_one({"DiscordId": msg.author.id,
                                                               "SlgId": slgid})


@Tools.cmd("!linksteam")
async def LinKSteam(user, steamid):
    existuser = Database.mdb.EcoChat.DiscordUsers.Find({"DiscordId": user.id})
    if existuser is not None and existuser["SteamId"] is not steamid:
        Database.mdb.EcoChat.DiscordUsers.update_one(
            {"DiscordId": user.id},
            {"$set": {"SteamId": steamid},
             "$currentDate": {"lastModified": True}})
    elif existuser is not None and existuser["SteamId"] is steamid:
        await user.send(content="Du hast deine Steam Id bereits verlinkt")
    else:
        await Database.mdb.EcoChat.DiscordMessages.insert_one({"DiscordId": user.id,
                                                               "SteamId": steamid})


@Tools.cmd("!userinfo")
async def getUserInfo(args, msg):
    member: Member = discord.utils.find(lambda m: args in m.name, msg.guild.members)
    if member:
        embed = discord.Embed(title="UserInfo für {}".format(member.name),
                              description="Dies ist eine Userinfo für den User{}".format(member.mention),
                              color=discord.Color.green())
        embed.add_field(name="Server beigetreten:", value=member.joined_at.strftime('%d.%m.%Y, %H:%M:%S'),
                        inline=True)
        embed.add_field(name="Discord User erstellt:",
                        value=member.created_at.strftime('%d.%m.%Y, %H:%M:%S'),
                        inline=True)
        rollen = ''
        for role in member.roles:
            if not role.is_default():
                rollen += '{} \r\n'.format(role.mention)
        if rollen:
            embed.add_field(name='Rollen:', value=rollen, inline=True)
        embed.set_thumbnail(url=member.avatar_url)
        embed.set_footer(text="Ich bin ein Footer")
        mess = await msg.channel.send(embed=embed)
        # await mess.add_reaction('a:partyglasses:474768940468535297')
        # await mess.add_reaction("🤑")
    # role: Role = discord.utils.find(lambda r: "Admin" in r.name, message.guild.roles)


@Tools.cmd("!clear")
async def clearChat(args, msg):
    if msg.author.permissions_in(msg.channel).manage_message:
        if args[0].isdigit():
            count = int(args[0]) + 1
            deleted = await msg.channel.purge(limit=count, check=Tools.is_not_pinned(msg))
            await msg.channel.send("{} Nachrichten gelöscht".format(len(deleted) - 1))


@Tools.cmd("!test")
async def sendedTestCommand(args, msg):
    msg.channel.send("Wrapper funktioniert")
