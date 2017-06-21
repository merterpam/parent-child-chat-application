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
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace Client
{
    /// <summary>
    /// Interaction logic for ChildPanel.xaml
    /// </summary>
    public partial class ChildPanel : Window
    {
        Socket parentSocket;
        Socket bindSocket;
        Thread thrParentAccept;
        string parentName;
        string name;
        Thread thrReceive;

        //Constructor
        public ChildPanel(string messages, string cName, int port, string pName)
        {
            InitializeComponent();
            bindSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            txtMessage.Text += messages;
            name = cName;
            parentName = pName;

            //Listen the port
            bindSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            bindSocket.Listen(3);
            thrParentAccept = new Thread(new ThreadStart(Accept)) { IsBackground = true };
            thrParentAccept.Start();
        }

        //Windows Loaded event handler
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        //Accept function for parent-child connection
        void Accept()
        {
            parentSocket = bindSocket.Accept();
            thrReceive = new Thread(new ThreadStart(ReceiveParent)) { IsBackground = true };
            thrReceive.Start();
            SendMessage("Parent has connected \n");

            //Enable chat-services
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    txtChat.IsEnabled = true;
                    txtSend.IsEnabled = true;
                    btnSend.IsEnabled = true;
                }));
        }

        //Receive function for parent
        void ReceiveParent()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[64];
                    parentSocket.Receive(buffer);

                    string message = Encoding.Default.GetString(buffer).Replace("\0", string.Empty);

                    //If the content of message is Message
                    if (message.IndexOf("Message:") == 0)
                    {
                        //Updates messagebox
                        SendMessage("Receiving a message from parent " + parentName + "\n");

                        //Put it on the chat screen
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            txtChat.Text += (string.Format("{0}: {1} \n", parentName, message.Substring("Message:".Length)));
                        }));
                    }
                    //If the content is a screenshot request
                    else if (message.IndexOf("ssRequest") == 0)
                    {
                        SendMessage("Received a screenshot request from parent " + parentName + "\n");
                        byte[] sendBuffer;

                        //Take the screenshot
                        Bitmap screenshot = CaptureScreen();

                        //Write it into bytes
                        using (MemoryStream memory = new MemoryStream())
                        {
                            //Get screenshot and encode it with base64 encoding
                            screenshot.Save(memory, ImageFormat.Png);
                            sendBuffer = memory.ToArray();
                            string base64String = Convert.ToBase64String(sendBuffer);

                            //Send the length of the buffer to parent
                            //Send the buffer to parent
                            Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    Send("ssLength:" + Encoding.ASCII.GetBytes(base64String).Length + "@");
                                    Send(base64String);
                                    SendMessage("Screenshot is sent to parent " + parentName + "\n");
                                }));
                        }

                    }
                }
            }
            //If connection to parent is lost
            catch (SocketException)
            {
                SendMessage("Connection to the parent is lost. \n");
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    txtSend.IsEnabled = false;
                    btnSend.IsEnabled = false;

                    System.Windows.MessageBox.Show("Connection to parent is lost.");
                    
                }));
            }
        }

        //Send message to the socket parentSocket
        void Send(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            parentSocket.Send(buffer);
        }


        //Update the messagebox
        void SendMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtMessage.Text += message;
            }));
        }

        //Button Send event handler 
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            //Sends the text in txtSend to parent
            txtChat.Text += name + ": " + txtSend.Text + "\n";
            string message = "Message:" + txtSend.Text;
            txtSend.Clear();
            Send(message);
            SendMessage(string.Format("Sending a message to {0}", parentName));
        }


        //Function to take Screenshot
        //Returns Screenshot
        Bitmap CaptureScreen()
        {
            int ix, iy, iw, ih;
            ix = Screen.PrimaryScreen.Bounds.X;
            iy = Screen.PrimaryScreen.Bounds.Y;
            iw = Screen.PrimaryScreen.Bounds.Width;
            ih = Screen.PrimaryScreen.Bounds.Height;
            Bitmap image = new Bitmap(iw, ih, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(image);
            g.CopyFromScreen(ix, iy, ix, iy, new System.Drawing.Size(iw, ih), CopyPixelOperation.SourceCopy);
            return image;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            thrReceive.Abort();
            parentSocket.Close();
        }
    }
}
