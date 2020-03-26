import Database
import DiscordBot


def cmd(befehl_name):
    def wrapper(func):
        def inner_wrapper(args, msg):
            func(args, msg)

        DiscordBot.all_commands[befehl_name] = inner_wrapper
        return func
    return wrapper


def call_cmd(content, msg):
    befehl_name = content.split(" ")[0].lower()
    print("Befehl: {}".format(befehl_name))
    DiscordBot.all_commands[befehl_name](content.split(" ")[1:], msg)


def is_not_pinned(mess):
    return not mess.pinned
