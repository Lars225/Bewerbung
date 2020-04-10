import motor.motor_asyncio
import SECRET

global mdb


# Erstellt einen Motor MongoDB Client
async def init(uri=SECRET.CONNECTIONSTRING, loop=None):
    global mdb
    mdb = motor.motor_asyncio.AsyncIOMotorClient(uri, io_loop=loop)
