using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server
{
    class Question
    {
        public string Quest { get; set; }
        public string Answer { get; set; }
        public int Score { get; set; }
        public double Time { get; set; }
        public Question(string quest, string ans)
        {
            Quest = quest;
            Answer = ans;
            Score = 100;
            Time = 10;
        }
    }
}
