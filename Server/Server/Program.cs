using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteSettings();
            
            try
            {
                Server server = new Server(Properties.Settings.Default.Port);

                while (true) { Thread.Sleep(1000); }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.ReadKey();
        }

        private static void WriteSettings()
        {
            Console.WriteLine("Server Settings: ");
            Console.WriteLine("SendBackToOriginalClient = " + Properties.Settings.Default.SendBackToOriginalClient);
            Console.WriteLine("Port = " + Properties.Settings.Default.Port);
            Console.WriteLine("ReadBufferSize = " + Properties.Settings.Default.ReadBufferSize);
            Console.WriteLine("MaxNumberOfClients = " + Properties.Settings.Default.MaxNumberOfClients);
            Console.WriteLine("NewPlayerByteProtocol = " + Properties.Settings.Default.NewPlayerByteProtocol);
            Console.WriteLine("DisconnectedPlayerByteProtocol = " + Properties.Settings.Default.DisconnectedPlayerByteProtocol);
            Console.WriteLine("SendMessageToClientsWhenAUserIsAdded = " + Properties.Settings.Default.SendMessageToClientsWhenAUserIsAdded);
            Console.WriteLine("SendMessageToClientsWhenAUserIsRemoved = " + Properties.Settings.Default.SendMessageToClientsWhenAUserIsRemoved);
            Console.WriteLine("EnableSendingIPAndIDWithEveryMessage = " + Properties.Settings.Default.EnableSendingIPAndIDWithEveryMessage);
        }
    }
}
