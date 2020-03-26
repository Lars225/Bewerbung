import Database
import DiscordBot

# Der Wrapper ermöglicht ein komfortableres Zugreifen und anlegen von neuen ChatCommands
def cmd(befehl_name):
    def wrapper(func):
        def inner_wrapper(args, msg):
            func(args, msg)

        DiscordBot.all_commands[befehl_name] = inner_wrapper
        return func
    return wrapper

# ChatCommands werden in Invoke und Args unterteilt. Der Invoke ruft über den Wrapper die zugeordnete Methode mit den passenden Args auf.
def call_cmd(content, msg):
    invoke = content.split(" ")[0].lower()
    print("Befehl: {}".format(invoke))
    DiscordBot.all_commands[invoke](content.split(" ")[1:], msg)


def is_not_pinned(mess):
    return not mess.pinned
