from asyncio import sleep

import discord
from discord import Member

import Tools

global client
all_commands = {}


# Durch den Wrapper wird jede deklarierte Methode in all_commands gespeichert. Dies ermöglichgt ein einfacheres Abrufen
def cmd(befehl_name):
    def wrapper(func):
        async def inner_wrapper(args, msg):
            await func(args, msg)

        all_commands[befehl_name] = inner_wrapper
        return func

    return wrapper


# Die in all_commands gespeicherte Methode wird aufgerufen
async def startCommand(msg):
    command = msg.content[1:].split(" ")[0].lower()
    args = msg.content.split(" ")[1:]
    await all_commands[command](args, msg)


# Mit @cmd wird die Methode beim Compilieren unter dem Namen "userinfo" über den Wrapper in all_commands eingefügt
@cmd("userinfo")
async def userInfo(args, msg):
    member: Member = discord.utils.find(lambda m: args[0] in m.name, msg.guild.members)
    infochat = client.get_channel(697051653567938621)
    if member:
        embed = discord.Embed(title="UserInfo für {}".format(member.name),
                              description="Dies ist eine Userinfo für den User {}".format(member.mention),
                              color=discord.Color.green())
        embed.add_field(name="Server beigetreten:", value=member.joined_at.strftime('%d.%m.%Y, %H:%M:%S'),
                        inline=True)
        embed.add_field(name="Discord User erstellt:", value=member.created_at.strftime('%d.%m.%Y, %H:%M:%S'),
                        inline=True)
        rollen = ''
        for role in member.roles:
            if not role.is_default():
                rollen += '{} \r\n'.format(role.mention)
        if rollen:
            embed.add_field(name='Rollen:', value=rollen, inline=True)
        embed.set_thumbnail(url=member.avatar_url)
        # embed.set_footer(text="Ich bin ein Footer")
        mess = await infochat.send("{} deine angefragte Userinfo:".format(msg.author.mention), embed=embed)
        await msg.delete()
        await sleep(60)
        await mess.delete()
        # await mess.add_reaction('a:partyglasses:474768940468535297')
        # await mess.add_reaction("🤑")
    # role: Role = discord.utils.find(lambda r: "Admin" in r.name, message.guild.roles)


# Abfrage ob die Nachricht angepinngt ist
def is_not_pinned(mess):
    return not mess.pinned


@cmd("clear")
async def clearChat(args, msg):
    if msg.author.permissions_in(msg.channel).administrator:
        if args[0].isdigit():
            count = int(args[0]) + 1
            deleted = await msg.channel.purge(limit=count, check=is_not_pinned)
            await msg.channel.send("{} Nachrichten gelöscht".format(len(deleted) - 1))


@cmd("help")
async def helpText(args, msg):
    await msg.channel.send("**Hilfe zum Bot**\r\n"
                           "!help - zeigt diese Zeilen an")


@cmd("testcommand")
async def testWrapper(args, msg):
    print("Args: {} Message:{}".format(args, msg))
