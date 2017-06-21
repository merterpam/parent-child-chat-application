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

namespace Client
{
    /// <summary>
    /// Interaction logic for StartPanel.xaml
    /// </summary>
    public partial class StartPanel : Window
    {
        public StartPanel()
        {
            InitializeComponent();
        }

        //Login button event handler
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Disable login, take necessary informations(username, port, ip) from UI
                btnLogin.IsEnabled = false;
                string userName = txtUsername.Text;
                int port;

                //Check if port can be converted to integer
                if (int.TryParse(txtPort.Text, out port))
                {
                    //message: message to be passed on new window
                    //Updating messagebox
                    string message = txtMessage.Text + "Attempting to connect server \n";
                    sendMessage("Attempting to connect server \n");

                    //Connect attempt to server
                    Socket connect = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    connect.Connect(txtIpAddress.Text, port);

                    //After connecting, make this window disappear and open a new window
                    this.Visibility = Visibility.Collapsed;

                    //Check if the user is a parent or child
                    bool isChild = (rdChild.IsChecked == true && rdParent.IsChecked == false);
                    bool? isNotAccepted = false;
                    //Open a child window
                    if (isChild)
                    {
                        ChildControlPanel child = new ChildControlPanel(ref connect, message, userName);
                        isNotAccepted = child.ShowDialog();
                        child.Close();
                    }

                    //Open a parent window    
                    else
                    {
                        ParentControlPanel parent = new ParentControlPanel(ref connect, userName, message);
                        isNotAccepted = parent.ShowDialog();
                        parent.Close();
                    }

                    if (isNotAccepted == true)
                    {
                        sendMessage("User with the same name already exists \n");
                        btnLogin.IsEnabled = true;
                        this.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        sendMessage("Disconnected from server \n");
                        this.Close();
                    }

                }

                //Show a message if the port is not integer
                else
                {
                    //Enable login button
                    MessageBox.Show("Please enter an integer for port number");
                    btnLogin.IsEnabled = true;
                }

            }
            //Catch any exception regarding server and give an error message
            catch (SocketException)
            {
                MessageBox.Show("There was an error while connecting to server. Please check your ip address and port again");
                sendMessage("Connection attempt was unsuccessfull \n");
                btnLogin.IsEnabled = true;
            }
        }

        //Updates messagebox from threades other than UI-thread
        void sendMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    txtMessage.Text += message;
                }));
        }
    }
}
