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
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace Client
{
    /// <summary>
    /// Interaction logic for ParentPanel.xaml
    /// </summary>
    public partial class ParentPanel : Window
    {
        Socket childSocket;
        Thread thrReceive;
        string childName;
        string parentName;

        //Constructor
        public ParentPanel(ref Socket socket, string messages, string cName, string pName)
        {
            InitializeComponent();
            childSocket = socket;
            childName = cName;
            parentName = pName;
            txtMessage.Text += messages;

            //Starts a new thread for childSocket to receive messages
            thrReceive = new Thread(new ThreadStart(Receive)) { IsBackground = true };
            thrReceive.Start();
        }


        //Receive function of socket childSocket
        void Receive()
        {
            try
            {
                //ssBuffer: true if a screenshot is waited. Initially false
                //length: total length of screenshot. When there is no screenshot, it is 0
                //memory: converter from byte[] (transformed from Bitmap) to BitmapImage

                bool ssBuffer = false;
                int ssLength = 0;
                MemoryStream memory = new MemoryStream();

                //Infinite loop of receive
                while (true)
                {
                    //Wait for incoming buffer


                    //If no screenshot is being waited
                    if (ssBuffer == false)
                    {
                        byte[] buffer = new byte[64];
                        childSocket.Receive(buffer);

                        string message = Encoding.Default.GetString(buffer).Replace("\0", string.Empty);

                        //Checks if the content of the message is a Message
                        if (message.IndexOf("Message:") == 0)
                        {
                            //Updates messagebox
                            SendMessage("Receiving a message from child " + childName + "\n");

                            //Updates chatbox
                            Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    txtChat.Text += (string.Format("{0}: {1} \n", childName, message.Substring("Message:".Length)));
                                }));
                        }
                        //Checks if the content of the message is the beginning of a screenshot queue
                        else if (message.IndexOf("ssLength:") == 0)
                        {
                            //Updates messagebox
                            SendMessage("Receiving screenshot of " + childName + "'s screen \n");



                            //Raise a flag about screenshot and take its length
                            ssBuffer = true;
                            ssLength = Convert.ToInt32(message.Substring("ssLength:".Length, message.IndexOf('@') - 9));

                            if (message.Length > message.IndexOf('@') + 1)
                            {
                                string message2 = message;
                            }
                            //If there is old image, refresh the memory
                            if (memory.Length > 0)
                                memory = new MemoryStream(ssLength);
                        }
                    }

                        //If receive status is in the middle of a screenshot queue
                    else
                    {
                        byte[] buffer = new byte[ssLength];
                        childSocket.Receive(buffer);

                        //Write the buffer to memory
                        memory.Write(buffer, 0, buffer.Length);

                        ssLength = -1;

                        if (ssLength <= 0)
                        {
                            //Update messagebox and reset ssLength and ssBuffer
                            SendMessage("End of receiving screenshot. Displaying screenshot \n");
                            ssLength = 0;
                            ssBuffer = false;

                            Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        btnSs.IsEnabled = true;

                                        //Flush stream and convert it to base64String
                                        memory.Flush();
                                        string base64String = Encoding.Default.GetString(memory.ToArray());

                                        //Convert base64String to image bytes and write bytes into a stream
                                        byte[] imageBytes = Convert.FromBase64String(base64String);
                                        MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                                        ms.Position = 0; ;

                                        //Write stream to image
                                        BitmapImage bitmapImage = new BitmapImage();
                                        bitmapImage.BeginInit();
                                        bitmapImage.StreamSource = ms;
                                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmapImage.EndInit();

                                        //Open a new page and show screenshot there
                                        ScreenshotPanel screenshot = new ScreenshotPanel(bitmapImage);
                                        screenshot.Show();
                                    }
                                    //If there was a problem with the image
                                    catch
                                    {
                                        System.Windows.MessageBox.Show("Screenshot could not be received, please try again");
                                    }
                                }));
                        }
                    }
                }
            }
            //If connection to server is lost
            catch (SocketException)
            {
                SendMessage("Connection to the child is lost. \n");

                Dispatcher.BeginInvoke(new Action(() =>
                    {
                        txtSend.IsEnabled = false;
                        btnSend.IsEnabled = false;
                        btnSs.IsEnabled = false;
                        System.Windows.MessageBox.Show("Connection to child is lost.");
                        
                    }));
            }
        }

        //Sends message to the remote end through socket toBeSent
        void Send(string message, Socket toBeSent)
        {
            //Convert string to buffer
            byte[] buffer = Encoding.ASCII.GetBytes(message);

            //Sends the message
            toBeSent.Send(buffer);
        }

        //Updates messagebox from threades other than UI-thread
        void SendMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtMessage.Text += message;
            }));
        }


        //Send button event handler
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            //Updates message box
            SendMessage("Sending a message to " + childName + "\n");

            //Prepares the message to be sent to child and send the message through socket
            string message = "Message:" + txtSend.Text;
            Send(message, childSocket);


            //Update the chat
            txtChat.Text += parentName + ": " + txtSend.Text + "\n";
            //Clear the textbox
            txtSend.Clear();

            SendMessage(string.Format("Sending a message to {0}", childName));
        }


        //Button take a screenshot event handler
        private void btnSs_Click(object sender, RoutedEventArgs e)
        {
            btnSs.IsEnabled = false;
            //Updates message box
            SendMessage("Requesting a screenshot from child " + childName + "\n");

            //Sends screenshot request to child's socket
            Send("ssRequest", childSocket);
        }

        //Closing thread and sockets
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            thrReceive.Abort();
            childSocket.Close();
        }
    }
}
