using System;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Rcon_Commander
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer ConsoleTextHandler = new DispatcherTimer(); // Create timer to handle text print effect
        private readonly Rcon Rcon = new Rcon(); // create new Rcon class instance

        string strToPrint = ">> Welcome to RconCommander [v1.0] by JuSTaR\n"; // Initialize first line to print in console
        bool sign = true; // Create to control console sign _
        bool CanSendCommand = false; // Blocks sending commands while console effect happen

        public MainWindow()
        {
            // Load coponents of the window
            InitializeComponent();

            // Create timer event
            ConsoleTextHandler.Tick += ConsoleTextHandler_Tick; // create console-text-handler event
            ConsoleTextHandler.Start(); // start timer

            // Focus command-textbox on window load
            Tb_Command.Focus();

            // Hide textbox-password to hide password in window load
            Tb_Rcon_Password.Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Saving login information from before window closes
            Properties.Settings.Default.server_ip = Tb_Ip.Text; // copy the Tb_Ip
            Properties.Settings.Default.server_port = Tb_Port.Text; // copy the Tb_Port
            Properties.Settings.Default.rcon_password = Tb_Rcon_Password.Text; // copy the tb_rcon_password
            Properties.Settings.Default.Save(); // Save All
        }

        private bool CheckServerDetails()
        {
            if (!IPAddress.TryParse(Tb_Ip.Text, out _)) // Check if ip is valid
            {
                strToPrint = "Invalid IP address.\nIP must be in the following syntax: 127.0.0.1\n"; // print the correct syntax
                return false;
            }

            if (!int.TryParse(Tb_Port.Text, out _) || Tb_Port.Text.Length > 5 || Tb_Port.Text.Length < 1) // Check if port is valid
            {
                strToPrint = "Invalid Port number.\nPort must contain only numbers.\n"; // print the correct syntax
                return false;
            }

            return true;
        }

        private void ConsoleTextHandler_Tick(object sender, EventArgs e)
        {
            if (strToPrint.Length > 0) // If there is text to print
            {
                CanSendCommand = false; // Disable further command sending

                if (!sign) // if sign _ appears in consolle
                {
                    Tb_Console.Text = Tb_Console.Text.Remove(Tb_Console.Text.Length - 1); // remove sign _
                    sign = true; // re-enable sign to appear
                }

                // Change Timer Interval for faster print (the more text to print - the faster it will print)
                ConsoleTextHandler.Interval = new TimeSpan(0, 0, 0, 0, 50 / strToPrint.Length);

                // Add first char to console
                Tb_Console.AppendText(strToPrint[0].ToString());

                // Remove first char from string
                strToPrint = strToPrint.Substring(1, strToPrint.Length - 1);

                // Scroll console to bottom
                Tb_Console.ScrollToEnd();

                // If console had finished to print
                if (strToPrint.Length == 0)
                {
                    Tb_Console.AppendText("\n"); // step down to the next line
                    CanSendCommand = true; // re-enable command sending
                }
            }
            else // If there there is no text to print
            {
                if (!Tb_Console.IsFocused) // if console is not focused
                {
                    if (sign) // if sign is true (need to put sign)
                    {
                        Tb_Console.AppendText("_"); // put sign _
                        sign = false; // next time remove sign _
                    }
                    else
                    {
                        if (Tb_Console.Text.Length > 0) // if console is not clear
                        {
                            Tb_Console.Text = Tb_Console.Text.Remove(Tb_Console.Text.Length - 1); // remove sign _
                        }
                        sign = true; //next time add sign _
                    }
                }

                ConsoleTextHandler.Interval = new TimeSpan(0, 0, 0, 0, 500); // Set long time interval to make sign appear slow
            }
        }

        private void Btn_Check_Connection_Click(object sender, RoutedEventArgs e)
        {
            if (CanSendCommand && CheckServerDetails()) // If can send command and server details are according to syntax
            {
                Rcon.Connect(Tb_Ip.Text, int.Parse(Tb_Port.Text), Tb_Rcon_Password.Text); // Start new udp client

                strToPrint = Rcon.CheckConnection() + "\n"; // Check connection and print server response

                CanSendCommand = false; // Disable sending new commands

                ConsoleTextHandler_Tick(sender, e); // Call console timer to handle the command
            }
        }

        private void Btn_Login_Click(object sender, RoutedEventArgs e)
        {
            if (CanSendCommand && CheckServerDetails()) // If can send command and server details are according to syntax
            {
                Rcon.Connect(Tb_Ip.Text, int.Parse(Tb_Port.Text), Tb_Rcon_Password.Text); // Start new udp client

                string response = Rcon.SendCommand("console"); // Check connection and print server response

                if (response.Contains("console")) // If server responded with "console..."
                {
                    strToPrint = "- RCON ACCESS GUARANTEED! -\n"; // Print that the server has accepected  the rcon_password
                }
                else // if problem occured
                {
                    strToPrint = response + "\n"; // Set text of the problem that occured
                }

                CanSendCommand = false; // Disable sending new commands

                ConsoleTextHandler_Tick(sender, e); // Call console timer to handle the command
            }
        }

        private void Btn_Clear_Console_Click(object sender, RoutedEventArgs e)
        {
            Tb_Console.Text = ""; // clear console
        }
        private void Tb_Command_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // If user pressed Enter key
            {
                Btn_Send_Click(sender, e); // Call function of button "Send"
            }
        }

        private void Btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if (CanSendCommand && CheckServerDetails()) // If can send command and server details are according to syntax
            {
                Rcon.Connect(Tb_Ip.Text, int.Parse(Tb_Port.Text), Tb_Rcon_Password.Text); // Start new udp client

                strToPrint = "-> " + Tb_Command.Text + "\n" + Rcon.SendCommand(Tb_Command.Text) + "\n"; // Check connection and print server response

                Tb_Command.Text = ""; // Clear command-textbox

                CanSendCommand = false; // Disable sending new commands

                ConsoleTextHandler_Tick(sender, e); // Call console timer to handle the command
            }
        }

        private void Btn_Copy_Console_Click(object sender, RoutedEventArgs e)
        {
            if (CanSendCommand) // If can send command
            {
                if (Tb_Console.Text.Length > 1) // If console has text
                {
                    if (!sign) // If console has sign _ in it's text
                    {
                        Clipboard.SetText(Tb_Console.Text.Remove(Tb_Console.Text.Length - 2)); // Then, copy console text and remove sign _ from the copied text

                    }
                    else // If console doesn't has the sign _
                    {
                        Clipboard.SetText(Tb_Console.Text); // just copy the text
                    }

                    strToPrint = "Console Copied!\n"; // Set text for console

                    ConsoleTextHandler_Tick(sender, e); // Call console timer to handle the command
                }
            }
        }

        private void Tb_Rcon_Password_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Pb_Rcon_Password.Password != Tb_Rcon_Password.Text) // if passwordbox not equals to textBox
            {
                Pb_Rcon_Password.Password = Tb_Rcon_Password.Text; // copy text from textbox to passwordBox
            }
        }

        private void Pb_Rcon_Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Tb_Rcon_Password.Text != Pb_Rcon_Password.Password) // if passwordbox not equals to textBox
            {
                Tb_Rcon_Password.Text = Pb_Rcon_Password.Password; // copy text from passwordBox to textBox
            }
        }

        private void Cb_Hide_Password_Click(object sender, RoutedEventArgs e)
        {
            if (cb_hide_password.IsChecked == true) // if Hide is checked
            {
                Tb_Rcon_Password.Visibility = Visibility.Collapsed; // Hide textBox
                Pb_Rcon_Password.Visibility = Visibility.Visible; // show passwordBox
            }
            else // if Hide is not checked
            {
                Tb_Rcon_Password.Visibility = Visibility.Visible; // show textBox
                Pb_Rcon_Password.Visibility = Visibility.Collapsed; // Hide passwordBox
            }
        }
    }
}
