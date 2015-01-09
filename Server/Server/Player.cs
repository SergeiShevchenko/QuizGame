using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Server
{
    public class Player
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public Socket PlayerSocket { get; set; }
        public string Answer { get; set; }
        public int QuestionNumber { get; set; }
        public Player(Socket sc)
        {
            Name = "";
            Score = 0;
            PlayerSocket = sc;
            Answer = "";
        }
    }
}
