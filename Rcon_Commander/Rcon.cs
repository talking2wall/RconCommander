using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Rcon_Commander
{
    class Rcon
    {
        private string server_ip;
        private int server_port;
        private string rcon_password;
        private string rcon_challenge = "";

        UdpClient Udp;
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public void Connect(string ip, int port, string pass)
        {
            Udp = new UdpClient();

            // Set connection timeout
            Udp.Client.SendTimeout = 5000;
            Udp.Client.ReceiveTimeout = 5000;

            // Get parameters
            server_ip = ip;
            server_port = port;
            rcon_password = pass;

            // Establish onnecttion to the server
            Udp.Connect(server_ip, server_port);
        }

        // Code partly took from the original "send rcon command"
        // https://www.codeproject.com/Articles/30516/Send-RCON-command-to-Counter-Strike
        private string GetChallenge()
        {
            try // try get challenge
            {
                // sending challenge command to counter strike server 
                string getChallenge = "challenge rcon\n";
                byte[] bufferSend = PrepareCommand(getChallenge);

                // send challenge command and get response
                Udp.Send(bufferSend, bufferSend.Length);
                byte[] bufferRec = Udp.Receive(ref RemoteIpEndPoint);

                // retrive number from challenge response 
                string challenge_rcon = Encoding.ASCII.GetString(bufferRec);

                // return the challenge number
                rcon_challenge = string.Join(null, Regex.Split(challenge_rcon, "[^\\d]"));
            }
            catch (Exception error) // if can't get challenge number
            {
                return error.Message; // return the reason
            }

            return rcon_challenge;
        }

        // Code entairly took from the original "send rcon command"
        // https://www.codeproject.com/Articles/30516/Send-RCON-command-to-Counter-Strike
        private byte[] PrepareCommand(string command)
        {
            byte[] bufferTemp = Encoding.ASCII.GetBytes(command);
            byte[] bufferSend = new byte[bufferTemp.Length + 4];

            //intial 5 characters as per standard
            bufferSend[0] = byte.Parse("255");
            bufferSend[1] = byte.Parse("255");
            bufferSend[2] = byte.Parse("255");
            bufferSend[3] = byte.Parse("255");

            //copying bytes from challenge rcon to send buffer
            int j = 4;

            for (int i = 0; i < bufferTemp.Length; i++)
            {
                bufferSend[j++] = bufferTemp[i];
            }
            return bufferSend;
        }

        public string CheckConnection()
        {
            // Get Challenge num
            string challenge = GetChallenge(); 

            if (!int.TryParse(challenge, out _)) // Check if challenge contain only numbers
            {
                return challenge; // if not, return the exception
            }
            else // if challlenge contain only numbers (as should be)
            {
                return "Connection is good!"; // return successful message
            }
        }

        public string SendCommand(string command)
        {
            string response; // String to handle response

            try
            {
                // Get Challenge num
                string challenge = GetChallenge();

                if (!int.TryParse(challenge, out _)) // Check if challenge contain only numbers
                {
                    return challenge; // if not, return the exception
                }

                // preparing rcon command to send
                string command_string = "rcon \"" + rcon_challenge + "\" " + rcon_password + " " + command + "\n";
                byte[] bufferSend = PrepareCommand(command_string);

                // Send socket
                Udp.Send(bufferSend, bufferSend.Length);

                // Get Response
                response = Encoding.ASCII.GetString(Udp.Receive(ref RemoteIpEndPoint));

                // A little delay to let the data get through
                Thread.Sleep(200);

                // Checks if there is more new data available
                while (Udp.Available > 0)
                {
                    response += Encoding.ASCII.GetString(Udp.Receive(ref RemoteIpEndPoint)).Remove(0, 5); // adding the new data
                    Thread.Sleep(200); // wait again
                }

                // If data has more than 4 chars
                if (response.Length > 4)
                {
                    response = response.Remove(0, 5); // cut the unknown chars that usually appears with every response that recieved from the server
                }

                // If server responses with "Bad Rcon Password"
                if (response.Contains("Bad"))
                {
                    return "Wrong Rcon Password \\ No password set for this server.\nWARNNING: Spamming wrong passwords could result in ban.";
                }
                else if (response.Length < 3) // If server didn't responded with any message but message sent successfuly
                {
                    return "No response for command: " + command;
                }
            }
            catch (Exception error) // If there was exception
            {
                return error.Message; // return the exception reason
            }

            // removes "\n" from the server response
            if (response.Contains("\n")) // if strToPrint has "\n" in the end 
            {
                response = response.Remove(response.Length - 3); // remove "\n" and some useless characters
            }
            
            return response; // return the server response for the rcon command
        }
    }
}
