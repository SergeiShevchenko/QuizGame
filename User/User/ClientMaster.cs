using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace User
{
    class ClientMaster
    {
        // Client status
        public bool IsClientRunning;
        
        // Сlient socket
        private Socket client;
       
        // Server addres
        private IPAddress ip = IPAddress.Parse("127.0.0.1");
        
        // Which port are we using
        private int port = 1991;
        
        // List of all the threads
        private List<Thread> threads = new List<Thread>();

        // Players answer
        public string Answer;

        // Right answer
        public string RightAnswer;

        // User interfacing form
        Form1 myForm;

        // Class constructor
        public ClientMaster(Form1 mf)
        {
            myForm = mf;
        }

        // Connecting to a server
        public void Connect()
        {
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ip, port);
                IsClientRunning = true;
                Receive();
            }
            catch (Exception e)
            {
                IsClientRunning = false;
                MessageBox.Show("Connection is lost: " + e.Message);
            }
        }

        // Receive info from server
        void Receive()
        {
            Thread th = new Thread(delegate()
            {
                byte[] bytes = new byte[1024];
                while (IsClientRunning)
                {
                    try
                    {
                        // Принимает данные от сервера в формате "X*Y"
                        int messageSize = client.Receive(bytes);
                        string data = Encoding.Unicode.GetString(bytes, 0, messageSize);
                        ProcessIncoming(data);
                    }
                    catch (Exception e)
                    {
                        //MessageBox.Show("Receiving info failed:" + e.Message);
                    }
                }
            });
            // Start the thread
            th.Start();
            threads.Add(th);
        }

        // Send info to server
        public void Send(string msg)
        {
            try
            {
                client.Send(Encoding.Unicode.GetBytes(msg + "*"));
            }
            catch (Exception e)
            {
                //MessageBox.Show("Sending info failed:" + e.Message);
            }
        }

        // Figure out what to do with info from server
        void ProcessIncoming(string s)
        {
            string[] tokens = s.Split('*');
            int from = 0;
            switch (tokens[0])
            {
                case "Question": // we've got ourselves a question
                    {
                        myForm.QuestionArrived(tokens[1]);
                        RightAnswer = tokens[2];
                        from = 3;
                        break;
                    }
                case "Time": // info about how much time we got left for answering 
                    {
                        myForm.TimeArrived(tokens[1]);
                        from = 2;
                        break;
                    }
                case "Round time": // info about how much time we got left for answering 
                    {
                        myForm.TimeArrived(tokens[1]);
                        from = 2;
                        break;
                    }
                case "Game over": // game is over
                    {
                        Disconnect();
                        break;
                    }
                case "Winner": // name of a winner
                    {
                        MessageBox.Show("And the winner is " + tokens[1]);
                        from = 2;
                        break;
                    }
                case "Gamestats": // score table
                    {
                        myForm.Stats(tokens[1]);
                        from = 2;
                        break;
                    }
            }
            if (from != 0)
            {
                string str = "";
                for (int i = from; i < tokens.Count(); i++)
                {
                    str += tokens[i] + "*";
                }
                ProcessIncoming(str);
            }
            return;
        }

        // Disconnect from the server and close the socket
        public void Disconnect()
        {
            IsClientRunning = false;
            if (client != null) 
                client.Close();
        }

        // Stop all working threads
        public void KillAllThreads()
        {
            foreach (Thread th in threads)
            {
                th.Abort();
            }
        }

    }
}
