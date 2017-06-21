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
    /// Interaction logic for childControlScreen.xaml
    /// </summary>

    public partial class ChildControlPanel : Window
    {
        Socket mainSocket;
        string name;
        Thread thrReceive;
        public ChildControlPanel(ref Socket socket, string messages, string cName)
        {
            InitializeComponent();
            mainSocket = socket;
            txtMessage.Text += messages;
            name = cName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Starting thread for receiving messages
            thrReceive = new Thread(new ThreadStart(Receive)) { IsBackground = true };
            thrReceive.Start();

            //Sending an authentication message to server and updating message box
            txtMessage.Text += "Sending an identification message to server... \n";
            Send("C:Login:" + name, mainSocket);
            txtMessage.Text += "Identification message is sent \n";
        }

        //Receive function for server
        void Receive()
        {
            try
            {
                int bufferSize = 64;
                while (true)
                {
                    byte[] buffer = new byte[bufferSize];
                    mainSocket.Receive(buffer);

                    bufferSize = 64;

                    string message = Encoding.Default.GetString(buffer).Replace("\0", string.Empty);

                    //If the content of message is create, then create a new connection for parent
                    if (message.IndexOf("Create:") == 0)
                    {
                        //Get port and parent name
                        int port = Convert.ToInt32(message.Substring("Create:".Length, 4));
                        string parentName = message.Substring("Create:0000Name:".Length);     

                        SendMessage("Creating port " + port + " for parent to listen \n");

                        Dispatcher.BeginInvoke(new Action(() =>
                        {

                        //Creating a new Screen for parent-child connection
                        ChildPanel newScreen = new ChildPanel(txtMessage.Text, name, port, parentName);
                        newScreen.Show();
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
                SendMessage("Connection to the main server is lost. \n");
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Windows.MessageBox.Show("Connection to main server is lost.");
                }));
            }
        }

        //Send message to the socket toBeSend
        void Send(string message, Socket toBeSent)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            toBeSent.Send(buffer);

        }


        //Update the messagebox
        void SendMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtMessage.Text += message;
            }));
        }

        //Closing thread and sockets
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            thrReceive.Abort();
            mainSocket.Close();
        }

    }
}
