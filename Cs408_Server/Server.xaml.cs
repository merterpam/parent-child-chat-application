using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.ComponentModel;

namespace Cs408_Server
{
    /// <summary>
    /// Interaction logic for Server.xaml
    /// </summary>

    public partial class Server : Window
    {
        List<ClientInformation> clientList;
        Socket sckMain;
        Thread thrAccept;
        List<Thread> thrReceiveList;
        Random randGen;
        public Server()
        {
            InitializeComponent();

            //Creates a new socket to receive connections
            sckMain = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);



            //Keeps client list
            clientList = new List<ClientInformation>();

            //Random number generator
            randGen = new Random();

            thrReceiveList = new List<Thread>();
        }


        //Start button click event handler
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int socketPort;
            //If socket can be parsed, then disable textbox and initiliaze a socket to listen connections
            if (Int32.TryParse(txtPort.Text, out socketPort))
            {
                btnStart.IsEnabled = false;
                txtPort.IsEnabled = false;
                sckMain.Bind(new IPEndPoint(IPAddress.Any, socketPort));
                sckMain.Listen(3);
                thrAccept = new Thread(new ThreadStart(Accept)) { IsBackground = true };
                thrAccept.Start();
                SendMessage("Starting listening.. \n");
            }

                //Give an error message to user
            else
            {
                MessageBox.Show("Please enter a valid port number to start listening");
            }
        }

        //Accept function for new connections
        void Accept()
        {
            while (true)
            {
                Socket socket = sckMain.Accept();
                thrReceiveList.Add(new Thread(new ParameterizedThreadStart(Receive)) { IsBackground = true });
                thrReceiveList[thrReceiveList.Count - 1].Start(socket);
            }
        }

        //Receive function for accepted connections
        void Receive(object sckt)
        {
            Socket socket = sckt as Socket;
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[64];
                    socket.Receive(buffer);
                    string message = Encoding.Default.GetString(buffer).Replace("\0", string.Empty);

                    //If the content of message is Login
                    if (message.IndexOf("Login:") == 2)
                    {
                        //Log the new client in
                        ClientInformation newClient = new ClientInformation();
                        newClient.clientName = message.Substring("X:Login:".Length);
                        newClient.clientSocket = socket;
                        bool sameUserExists = false;
                        foreach (var item in clientList)
                        {
                            if (item.clientName == newClient.clientName)
                            {
                                sameUserExists = true;
                            }
                        }
                        if (sameUserExists)
                        {
                            Send("D:", newClient.clientSocket);
                        }
                        else
                        {
                            //Determining the role of client
                            if (message[0] == 'C')
                            {
                                newClient.clientRole = "Child";
                            }
                            else
                            {
                                newClient.clientRole = "Parent";
                            }
                            clientList.Add(newClient);

                            //Update the messagebox
                            string onlineUserClient = "Client " + newClient.clientName + " is connected to server with ip address " + newClient.ipAddress + Environment.NewLine;
                            onlineUserClient += "Online Users are:" + Environment.NewLine;
                            string onlineUserServer = onlineUserClient;
                            
                            foreach (var item in clientList)
                            {
                                onlineUserClient += item.clientRole + " " + item.clientName + " with IP " + item.ipAddress + ":";
                                onlineUserServer += "Role: " + item.clientRole + " Name: " + item.clientName + " IP: " + item.ipAddress + Environment.NewLine;
                            }
                            SendMessage(onlineUserServer);
                            foreach (var item in clientList)
                            {
                                Send(("L:" + (onlineUserClient.Length + 2)).PadRight(64, '/'), item.clientSocket);
                                Send("B:" + onlineUserClient, item.clientSocket);
                            }
                        }
                    }
                    //If the content of message is connect
                    else if (message.IndexOf("Connect:") == 0)
                    {

                        string childName = message.Substring("Connect:".Length);
                        ClientInformation connectedChild = null;
                        ClientInformation connectedParent = null;

                        //Search for parent and child in clientList
                        foreach (var client in clientList)
                        {
                            if (client.clientName == childName && client.clientRole == "Child")
                            {
                                connectedChild = client;
                            }
                            if ((socket.RemoteEndPoint as IPEndPoint).Address == client.ipAddress && client.clientRole == "Parent")
                            {
                                connectedParent = client;
                            }
                        }
                        SendMessage(string.Format("Client {0} with IP-Address {1} is trying to connect {2} {3}Retrieving IP adress of {2}{3}", connectedParent.clientName, connectedParent.ipAddress, childName, Environment.NewLine));

                        //If parent and child are found
                        if (connectedChild != null)
                        {
                            int port = randGen.Next(9000, 9999);
                            string childMessage = "Create:" + port + "Name:" + connectedParent.clientName;


                            SendMessage(string.Format("Sending the port number {1} to child {0} for him to listen{2}", connectedChild.clientName, port, Environment.NewLine));
                            string parentMessage = string.Format("Connect:{0}Port:{1}", (connectedChild.ipAddress), port);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Send(childMessage, connectedChild.clientSocket);
                                Send(parentMessage, socket);
                            }));
                        }

                            //If child or parent is not found
                        else
                        {
                            SendMessage("Attempt is unsuccessfull because client to be connected " + childName + " is not logged in. " + Environment.NewLine);
                            Send("Invalid:Unknown User", socket);
                        }
                    }
                }
            }
            catch (SocketException)
            {
                ClientInformation lostClient = clientList.Where(x => x.clientSocket == socket).FirstOrDefault();
                if (lostClient != null)
                {
                clientList.Remove(lostClient);

                //Update the messagebox
                string onlineUserClient = "Client " + lostClient.clientName + " is disconnected from server with ip address " + lostClient.ipAddress + Environment.NewLine;
                string onlineUserServer = onlineUserClient;
                onlineUserServer += "Online Users are:" + Environment.NewLine;
                foreach (var item in clientList)
                {
                    onlineUserClient += item.clientRole + " " + item.clientName + " with IP " + item.ipAddress + ":";
                    onlineUserServer += "Role: " + item.clientRole + "Name: " + item.clientName + " IP: " + item.ipAddress + Environment.NewLine;
                }
                if (clientList.Count == 0)
                {
                    onlineUserClient += "None:";
                    onlineUserServer += "None \n";
                }

                SendMessage(onlineUserServer);
                foreach (var item in clientList)
                {
                    Send(("L:" + (onlineUserClient.Length + 2)).PadRight(64, '/'), item.clientSocket);
                    Send("B:" + onlineUserClient, item.clientSocket);
                }
            }
            }
        }

        void Send(string message, Socket toBeSent)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            toBeSent.Send(buffer);
        }

        void SendMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtMessage.Text += message;
            }));
        }


    }
    class ClientInformation
    {
        public IPAddress ipAddress
        {
            get { return (clientSocket.RemoteEndPoint as IPEndPoint).Address; }
        }
        public string clientRole;
        public string clientName;
        public Socket clientSocket;
    };
}
