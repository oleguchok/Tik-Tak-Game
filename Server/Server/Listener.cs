using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Listener
    {
        private TcpListener listener;

        public event ConnectionEvent userAdded;

        private bool[] usedUserID;

        public Listener(int portNr)
        {
            usedUserID = new bool[Properties.Settings.Default.MaxNumberOfClients];

            listener = new TcpListener(IPAddress.Any, portNr);
        }

        public void Start()
        {
            listener.Start();
            ListenForNewClient();
        }

        public void Stop()
        {
            listener.Stop();
        }

        private void ListenForNewClient()
        {
            listener.BeginAcceptTcpClient(AcceptClient, null);
        }

        private void AcceptClient(IAsyncResult ar)
        {
            TcpClient client = listener.EndAcceptTcpClient(ar);

            int id = -1;
            for (byte i = 0; i < usedUserID.Length; i++)
            {
                if (usedUserID[i] == false)
                {
                    id = i;
                    break;
                }
            }

            if (id == -1)
            {
                Console.WriteLine("Client " + client.Client.RemoteEndPoint.ToString() + " cannot connect. ");
                return;
            }

            usedUserID[id] = true;
            Client newClient = new Client(client, (byte)id);

            newClient.UserDisconnected += new ConnectionEvent(client_UserDisconnected);

            if (userAdded != null)
                userAdded(this, newClient);

            ListenForNewClient();
        }

        void client_UserDisconnected(object sender, Client user)
        {
            usedUserID[user.id] = false;
        }

    }
}
