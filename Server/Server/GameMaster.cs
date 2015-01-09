using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Server
{
    // Rules of which game should we use
    // --> Blitz - server sends all players questions during a customizable period of time. Questions for different players will be different.
    // When the time is up, the player with most points wins. Usually 1 question costs 100 points.
    // --> FixedQuestionCount - server sends all players a customizable number of questions (the same for every player). 
    // Every player should answer a question in 10 seconds. Players with the most points wins.
    // 
    enum TypeOfGame { Blitz, FixedQuestionCount };

    // Main server game managing class
    class GameMaster
    {
        // Server status
        public bool IsServerRunning { get; set; }

        // List of all players
        public List<Player> Players;

        // Type of this game
        public TypeOfGame TypeOfGame { get; set; }

        // Time period for the whole game or per question
        public TimeSpan TimeForGame;

        // Time of game start
        public DateTime GameBeginsAt;

        // Server socket
        Socket listener;

        // Which port we are goint to use
        int port = 1991;

        // Point for incoming messages
        IPEndPoint Point;

        // List of all the threads
        List<Thread> threads = new List<Thread>();
        
        // Number of players in this particular game
        int NumPlayers;

        // List of questions for the game
        List<Question> Questions;

        // Number of questions in the game
        int NumQuestions;

        // Number of current question in the game
        int NumCurrentQuestion;

        // User interface form
        Form1 form;     
   
        // Period of the refresh timer
        long Period = 500;

        // Refreshing timer
        System.Timers.Timer gameTimer;

        // Class constructor for Blitz
        public GameMaster(int numPlayers, string filename, Form1 f, TypeOfGame tog, TimeSpan tfg, int numq)
        {
            Players = new List<Player>();
            NumPlayers = numPlayers;
            InitializeQuestionBase(filename);
            form = f;
            TypeOfGame = tog;
            TimeForGame = tfg;
            NumQuestions = numq;
            NumCurrentQuestion = 0;
        }

        // Loading questions from a file
        // in format: Question*Answer
        void InitializeQuestionBase(string filename)
        {
            Questions = new List<Question>();
            using (StreamReader sr = new StreamReader(filename))
            {
                string cur;
                string[] tokens;
                while (!sr.EndOfStream)
                {
                    cur = sr.ReadLine();
                    tokens = cur.Split('*');
                    Questions.Add(new Question(tokens[0], tokens[1]));
                }
            }
        }

        // Starting of server
        public void ServerStart()
        {
            IsServerRunning = true;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Determining an endpoint, IPAddress.Any means we will accept connections from any IP adresses
            Point = new IPEndPoint(IPAddress.Any, port);
            // Binding server socket with endpoint
            listener.Bind(Point);
            // Start listening for incoming connections
            listener.Listen(10);
            SocketAccepter();
        }

        // Establishes a connection with any willing user
        private void SocketAccepter()
        {
            Thread th = new Thread(delegate()
            {
                while (IsServerRunning)
                {
                    Socket client = listener.Accept();

                    // Adding a new player to the game
                    Player Newbie = new Player(client);
                    //Players.Add(Newbie);

                    // For every new client, new thread
                    Thread thh = new Thread(delegate()
                    {
                        byte[] bytes = new byte[1024];
                        while (IsServerRunning)
                        {
                            try
                            {
                                // Receive stuff
                                int messageSize = Newbie.PlayerSocket.Receive(bytes);
                                // Convert it into a readable string
                                string data = Encoding.Unicode.GetString(bytes, 0, messageSize);
                                // Process it
                                this.ProcessIncomingInfo(data, Newbie);
                            }
                            catch (Exception e)
                            {
                                //MessageBox.Show("Message receive failed: "+e.Message);
                            }
                        }
                    });
                    threads.Add(thh);
                    thh.Start();
                }
            });

            threads.Add(th);
            th.Start();            
        }
        
        // Sends message s to a particular user
        private void MessageSender(Player c_client, string s)
        {
            try
            {
                byte[] bytes = new byte[1024];
                bytes = Encoding.Unicode.GetBytes(s+"*");
                c_client.PlayerSocket.Send(bytes);
            }
            catch (Exception e)
            {
                //MessageBox.Show("Message sending failed: " + e.Message);
            }
        }

        // Main procedure for game managing
        public void ProcessIncomingInfo(string s, Player p)
        {
            string[] tokens = s.Split('*');
            switch (tokens[0])
            {
                case "Username":
                {
                    string name = tokens[1];
                    if (Players.Any(pl => pl.Name == name))
                        MessageBox.Show("This player already exists");
                    //Register a new player, if the number of players is ok
                    else if (Players.Count < NumPlayers)
                    {
                        Players.Add(p);
                        p.Name = name;
                        if (Players.Count == NumPlayers)
                        {
                            GameStart();
                        }
                    }
                    else
                    {
                        // Too much players, game is on already
                        MessageBox.Show("Too many players");
                    }
                    break;
                }
                case "Answer":
                {
                    // Check an answer
                    string ans = tokens[1];
                    if (ans == Questions[p.QuestionNumber].Answer)
                    {
                        p.Score += Questions[p.QuestionNumber].Score;
                    }
                    // Continue the game
                    if (TypeOfGame == TypeOfGame.Blitz)
                        GameContinue(p);
                    break;
                }

            }
            // Refresh the scoreboard
            form.ScoreBoard();
            return;
        }

        // Procedure for the start of the game
        void GameStart()
        {
            if (TypeOfGame == TypeOfGame.Blitz)
            {
                gameTimer = new System.Timers.Timer(Period);
                gameTimer.Elapsed += new System.Timers.ElapsedEventHandler(BlitzTimerHandle);
                gameTimer.Enabled = true;
                GameBeginsAt = DateTime.Now;
                foreach (Player p in Players)
                {
                    GameContinue(p);
                }
            }
            else if (TypeOfGame == TypeOfGame.FixedQuestionCount)
            {
                gameTimer = new System.Timers.Timer(Period);
                gameTimer.Elapsed += new System.Timers.ElapsedEventHandler(FixedQuestionCountHandle);
                gameTimer.Enabled = true;
                GameBeginsAt = DateTime.Now;
                GameContinue(null);
            }
        }

        // Timer handler for second type of game
        void FixedQuestionCountHandle(object source, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan ts = e.SignalTime.Subtract(GameBeginsAt);
            form.TimerUpdate(ts);
            if (ts >= TimeForGame)
            {
                gameTimer.Enabled = false;
                GameContinue(null);
            }
            foreach (Player c_client in Players)
            {
                MessageSender(c_client, "Round time*" + String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10) + ", round ends at: " + TimeForGame.ToString());
                MessageSender(c_client, GameStats());
            }
        }

        // Timer handler for blitz
        void BlitzTimerHandle(object source, System.Timers.ElapsedEventArgs e)
        {  
            TimeSpan ts = e.SignalTime.Subtract(GameBeginsAt);
            form.TimerUpdate(ts);
            if (ts >= TimeForGame)
            {
                gameTimer.Enabled = false;
                GameOver();
            }
            foreach (Player c_client in Players)
            {
                MessageSender(c_client, "Time*" + String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10) + ", game ends at: " + TimeForGame.ToString());
                MessageSender(c_client, GameStats());
            }
        }

        // What to do next
        void GameContinue(Player p)
        {
            if (TypeOfGame == TypeOfGame.Blitz)
            {
                p.QuestionNumber = NextQuestionNumber();
                MessageSender(p, SerializeQuestion(p.QuestionNumber));
            }
            else if (TypeOfGame == TypeOfGame.FixedQuestionCount)
            {
                NumCurrentQuestion++;
                if (NumCurrentQuestion <= NumQuestions)
                {
                    int a = NextQuestionNumber();
                    foreach (Player pl in Players)
                    {
                        pl.QuestionNumber = a;
                        MessageSender(pl, SerializeQuestion(pl.QuestionNumber));
                    }
                    GameBeginsAt = DateTime.Now;
                    gameTimer.Enabled = true;
                }
                else
                {
                    GameOver();
                }
            }
        }

        // Stopping the server due to the game over
        void GameOver()
        {
            int index = 0;
            for (int i = 1; i < Players.Count; i++)
            {
                if (Players[index].Score < Players[i].Score)
                {
                    index = i;
                }
            }
            // Sending info bout the result of the game to everyone
            foreach (Player p in Players)
            {                
                //MessageSender(p, GameStats());
                MessageSender(p, ("Winner*" + Players[index].Name));
                MessageSender(p, "Game over");
            }
            Disconnect();
        }

        // Serialization of scoreboard
        // ---> maybe some day this will actually be a serialization <---
        string GameStats()
        {            
            string result = "Gamestats*";
            //TODO: преобразовать в билдер
            for (int i=0; i<Players.Count; i++)
            {
                result += Players[i].Name + "_" + Players[i].Score;
                if (i != Players.Count - 1) 
                    result += "_";                
            }
            return result;
        }

        // Returns the random number of question from the base
        int NextQuestionNumber()
        {
            //TODO: внести случайность на старте
            Random r = new Random();
            return r.Next(Questions.Count);            
        }

        string SerializeQuestion(int n)
        {
            return "Question*" + Questions[n].Quest + "*" + Questions[n].Answer + "*" + Questions[n].Time;
        }

        // Closes the socket, stops all threads
        public void Disconnect()
        {
            IsServerRunning = false;
            gameTimer.Enabled = false;
            foreach (Thread th in threads)
            {
                th.Abort();
            }
            listener.Close();
        }
    }
}
