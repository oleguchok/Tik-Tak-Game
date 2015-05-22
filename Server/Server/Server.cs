using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        public static Server singleton;

        Listener listener;
        public Listener Listener
        {
            get { return listener; }
        }

        Client[] client;

        int connectedClients = 0;

        MemoryStream readStream;
        MemoryStream writeStream;
        BinaryReader reader;
        BinaryWriter writer;

        public Server(int port)
        {
            client = new Client[Properties.Settings.Default.MaxNumberOfClients];

            listener = new Listener(port);
            listener.userAdded += new ConnectionEvent(listener_userAdded);
            listener.Start();

            readStream = new MemoryStream();
            writeStream = new MemoryStream();
            reader = new BinaryReader(readStream);
            writer = new BinaryWriter(writeStream);

            Server.singleton = this;
        }

        private void listener_userAdded(object sender, Client user)
        {
            connectedClients++;

            if (Properties.Settings.Default.SendMessageToClientsWhenAUserIsAdded)
            {
                writeStream.Position = 0;

                writer.Write(Properties.Settings.Default.NewPlayerByteProtocol);
                writer.Write(user.id);
                writer.Write(user.IP);

                SendData(GetDataFromMemoryStream(writeStream), user);
            }

            user.DataReceived += new DataReceivedEvent(user_DataReceived);
            user.UserDisconnected += new ConnectionEvent(user_UserDisconnected);

            Console.WriteLine(user.ToString() + " connected\tConnected Clients:  " + connectedClients + "\n");

            client[user.id] = user;
        }

        private void user_UserDisconnected(object sender, Client user)
        {
            connectedClients--;

            if (Properties.Settings.Default.SendMessageToClientsWhenAUserIsRemoved)
            {
                writeStream.Position = 0;

                writer.Write(Properties.Settings.Default.DisconnectedPlayerByteProtocol);
                writer.Write(user.id);
                writer.Write(user.IP);

                SendData(GetDataFromMemoryStream(writeStream), user);
            }

            Console.WriteLine(user.ToString() + " disconnected\tConnected Clients:  " + connectedClients + "\n");

            client[user.id] = null;
        }

        private void user_DataReceived(Client sender, byte[] data)
        {
            writeStream.Position = 0;

            if (Properties.Settings.Default.EnableSendingIPAndIDWithEveryMessage)
            {
                writer.Write(sender.id);
                writer.Write(sender.IP);
                data = CombineData(data, writeStream);
            }

            if (Properties.Settings.Default.SendBackToOriginalClient)
            {
                SendData(data);
            }
            else
            {
                SendData(data, sender);
            }
        }

        private byte[] GetDataFromMemoryStream(MemoryStream ms)
        {
            byte[] result;

            lock (ms)
            {
                int bytesWritten = (int)ms.Position;
                result = new byte[bytesWritten];

                ms.Position = 0;
                ms.Read(result, 0, bytesWritten);
            }

            return result;
        }

        private byte[] CombineData(byte[] data, MemoryStream ms)
        {
            byte[] result = GetDataFromMemoryStream(ms);

            byte[] combinedData = new byte[data.Length + result.Length];

            for (int i = 0; i < data.Length; i++)
            {
                combinedData[i] = data[i];
            }

            for (int j = data.Length; j < data.Length + result.Length; j++)
            {
                combinedData[j] = result[j - data.Length];
            }

            return combinedData;
        }

        private void SendData(byte[] data, Client sender)
        {
            foreach (Client c in client)
            {
                if (c != null && c != sender)
                {
                    c.SendData(data);
                }
            }

            writeStream.Position = 0;
        }

        private void SendData(byte[] data)
        {
            foreach (Client c in client)
            {
                if (c != null)
                    c.SendData(data);
            }

            writeStream.Position = 0;
        }
    }
}
