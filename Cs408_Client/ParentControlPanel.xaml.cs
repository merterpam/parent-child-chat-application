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
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    /// <summary>
    /// Interaction logic for ParentControlPanel.xaml
    /// </summary>
    public partial class ParentControlPanel : Window
    {
        Socket serverSocket;
        string parentName;
        string childName;
        Thread thrReceive;

        //Constructor
        public ParentControlPanel(ref Socket sSocket, string pName, string messages)
        {
            InitializeComponent();
            serverSocket = sSocket;
            parentName = pName;
            txtMessage.Text += messages;
        }

        //Window Loaded event handler
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string loginString = "P:Login:" + parentName;
            //Starting thread for receiving messages
            thrReceive = new Thread(new ThreadStart(Receive)) { IsBackground = true };
            thrReceive.Start();

            //Sending an authentication message to server
            txtMessage.Text += "Sending an identification message to server... \n";

            Send(loginString);
            txtMessage.Text += "Identification message is sent \n";

        }

        //Button connect event handler
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            //Disable connect button
            btnConnect.IsEnabled = false;
            childName = txtChild.Text;

            //Update messagebox and send a message to server
            SendMessage("Requesting to server a connection with the child " + childName + " \n");
            string requestString = "Connect:" + childName;
            Send(requestString);

        }


        //Sending messages to server
        void Send(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            serverSocket.Send(buffer);
        }

        //Updates messagebox
        void SendMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtMessage.Text += message;
            }));
        }

        //Receives messages from socket
        void Receive()
        {
            try
            {
                int bufferSize = 64; 
                //Infinite loop to receive message
                while (true)
                {

                    byte[] buffer = new byte[bufferSize];
                    serverSocket.Receive(buffer);

                    bufferSize = 64;
                    string message = Encoding.Default.GetString(buffer).Replace("\0", string.Empty);

                    //If the content of message is connect
                    if (message.IndexOf("Connect:") == 0)
                    {
                        //Update messagebox
                        SendMessage(string.Format("Sending a request to connect child {0} \n", childName));

                        //Receive ip address and port number of host to be connected
                        int ipIndex = "Connect:".Length;
                        int portIndex = message.IndexOf("Port:");
                        string host = message.Substring(ipIndex, portIndex - ipIndex);
                        int port = Convert.ToInt32(message.Substring(portIndex + "Port:".Length, 4));

                        //Create socket
                        Socket childSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        //Try to connect socket
                        try
                        {
                            childSocket.Connect(host, port);
                            SendMessage(string.Format("Connection to child on host {0}:{1} is made \n", host, port));
                        }
                        //If connection fails, wait for an interval and try again
                        catch
                        {
                            Thread.Sleep(1);
                            try
                            {
                                childSocket.Connect(host, port);
                                SendMessage(string.Format("Connection to child on host {0}:{1} is made \n", host, port));
                            }
                            //If it fails again give an error message
                            catch
                            {
                                SendMessage("Connection to child was failed because there is no port at child's host. Please try again \n");
                            }
                        }

                        //After connecting, open a new window to interact with child
                        Dispatcher.BeginInvoke(new Action(() =>
                            {
                                btnConnect.IsEnabled = true;
                                ParentPanel parent = new ParentPanel(ref childSocket, txtMessage.Text, childName, parentName);
                                parent.Show();
                                txtChild.Clear();
                            }));
                    }
                    else if (message.IndexOf("B:") == 0)
                    {
                        string[] userList = message.Split(':');
                        for (int i = 1; i < userList.Length; i++)
                        {
                            SendMessage(userList[i] + Environment.NewLine);
                        }
                    }

                    else if (message.IndexOf("L:") == 0)
                    {
                        string length = message.Substring(2, (message.IndexOf("/") - 2));
                        bufferSize = Convert.ToInt32(length);
                    }

                    //If the content of message is invalid
                    else if (message.IndexOf("Invalid:") == 0)
                    {
                        //Connection attempt to child was unsuccessfull, because there is no such child
                        //Inform parent of this situation

                        SendMessage("Connection request attempt is unsuccessfull \n");

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            btnConnect.IsEnabled = true;
                            MessageBox.Show("There is no child with the name of " + txtChild.Text + ". Please check again");
                            txtChild.Clear();
                        }));
                    }
                    else if (message.IndexOf("D:") == 0)
                    {
                        SendMessage("User with the same name already exists \n");
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show("User with the same name already exists, please use another name");
                            this.DialogResult = true;
                            this.Close();
                        }));

                    }
                }
            }
            //If connection to server is lost
            catch (SocketException)
            {
                SendMessage("Connection to the main server is lost. Please restart the program if you want to connect a child \n");
                Dispatcher.Invoke(new Action(() =>
                    {
                        btnConnect.IsEnabled = false;
                        txtChild.IsEnabled = false;
                        MessageBox.Show("Connection to main server is lost");
                    }));
            }
        }

    }
}
