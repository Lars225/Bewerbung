using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServerLauncher
{
    class Program
    {
        private static Process mProcess;
        private static MongoClient mongoClient;
        private static IMongoDatabase db;
        private static object mLocker = new object();
        private static bool Restart = false;
        private static string shutdownCommand = "exit";
        private static string processFilePath = "process.txt";
        static Task Main(string[] args)
        {
            if (File.Exists(processFilePath))
            {
                int processId = int.Parse(File.ReadAllLines(processFilePath)[0]);
                mProcess = Process.GetProcessById(processId);
            }
            try
            {
                mongoClient = new MongoClient("MongoDB_Connection_String");
            }
            catch (Exception e) { Console.WriteLine(e); }
            db = mongoClient.GetDatabase("eco_server01");
            var col = db.GetCollection<ServerCommand>("ServerCommands");

            var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
            
            // Durch die Watch Methode wird die MongoDB Datenbank in Echtzeit auf Veränderungen überwacht.
            var cursor = col.Watch(options);

            // Eine Endlosschleife die dauerhaft auf einen Eintrag wartet und auswertet.
            // Zur Schonung der Hardware und Performance des Dedicated Server werden die Schleifen jeweils um 1 Sekunde verzögert. 
            while (true)
            {
                Console.WriteLine("Warte auf DB Eintrag");
                while (cursor.MoveNext() && cursor.Current.Count() == 0)
                {
                    Thread.Sleep(1000); 
                    if (DateTime.Now.Hour == 5 && mProcess != null)
                    {
                        Console.WriteLine("Timer ausgelöst um: " + DateTime.Now.ToShortTimeString());
                        Restart = true;
                        SendMessage(shutdownCommand);
                    }
                }
                Console.WriteLine("Neuer DB Eintrag erkannt");
                var next = cursor.Current.First();
                ServerCommand command = next.FullDocument;
                if (command.Command.Equals("StartServer"))
                {
                    Console.WriteLine("Command: Start Server");
                    StartServer();
                }
                else if (command.Command.Equals("StopServer"))
                {
                    Console.WriteLine("Command: Stop Server");
                    SendMessage(shutdownCommand);
                    File.Delete(processFilePath);
                }
                else if (command.Command.Equals("RestartServer"))
                {
                    Console.WriteLine("Command: Restart");
                    Restart = true;
                    SendMessage(shutdownCommand);
                    File.Delete(processFilePath);
                }
                else if (command.Command.Equals("EmergencyShutdown"))
                {
                    Console.WriteLine("EmergencyShutdown wird eingeleitet!");
                    mProcess.Kill();
                    File.Delete(processFilePath);
                }
                else
                {
                    Console.WriteLine();
                }
                Thread.Sleep(50);
            }
            return null;
        }

        private static void StartServer()
        {
            var col = db.GetCollection<ConsoleEntry>("ConsoleLog");
            if (mProcess != null)
            {
                Console.WriteLine("Server läuft bereits");
                col.InsertOne(new ConsoleEntry { dateTime = DateTime.Now, EntryMessage = "Server läuft bereits!" });
                return;
            }

            ProcessStartInfo processSettings = new ProcessStartInfo
            ();
            processSettings.CreateNoWindow = true;
            processSettings.RedirectStandardOutput = true;
            processSettings.RedirectStandardInput = true;
            processSettings.UseShellExecute = false;
            processSettings.Arguments = "-nogui";
            processSettings.FileName = Directory.GetCurrentDirectory() + "\\EcoServer\\EcoServer.exe";
            processSettings.WorkingDirectory = Path.GetDirectoryName(processSettings.FileName);

            mProcess = new Process();
            mProcess.StartInfo = processSettings;

            mProcess.OutputDataReceived += (s, e) =>
            {
                col.InsertOne(new ConsoleEntry { dateTime = DateTime.Now, EntryMessage = e.Data });
                Console.WriteLine(e.Data);
            };

            mProcess.Exited += (s, e) =>
            {
                col.InsertOne(new ConsoleEntry { dateTime = DateTime.Now, EntryMessage = "Process exited." });
                if (Restart == true)
                {
                    Restart = false;
                    StartServer();
                }

            };
        }

        private static void SendMessage(string message)
        {
            mProcess.StandardInput.WriteLine(message);
        }
    }
}
