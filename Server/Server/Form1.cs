using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();            
        }

        /// <summary>
        /// Main game managing object
        /// </summary>
        GameMaster gm;

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        // Start the server if it hasnt already been started
        private void button1_Click(object sender, EventArgs e)
        {
            if (gm == null || gm.IsServerRunning == false)
            {
                TimeSpan ts = new TimeSpan(0, 0, Convert.ToInt32(textBox1.Text));
                if (radioButton1.Checked)
                {
                    gm = new GameMaster((int)numericUpDown1.Value, "questions.txt", this, TypeOfGame.Blitz, ts, 0);
                }
                else if (radioButton2.Checked)
                {
                    gm = new GameMaster((int)numericUpDown1.Value, "questions.txt", this, TypeOfGame.FixedQuestionCount, ts, Convert.ToInt32(textBox2.Text));
                }
                gm.ServerStart();
            }
            else
                MessageBox.Show("The game is already on!");
        }

        public void FreshUp()
        {
            dataGridView1.Rows.Clear();
            for (int i = 0; i < gm.Players.Count; i++)
            {

                dataGridView1.Rows.Add();
                dataGridView1.Rows[i].Cells[0].Value = gm.Players[i].Name;
                dataGridView1.Rows[i].Cells[1].Value = gm.Players[i].Score;
                dataGridView1.Rows[i].Cells[2].Value = gm.Players[i].PlayerSocket.Handle;
            }
        }
        
        // Show the scoreboard
        public void ScoreBoard()
        {
            dataGridView1.Invoke(new Action(FreshUp));
        }

        // Stop the server when the form is closed
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopTheServer();
            Application.Exit();
        }

        // Updating the game time
        public void TimerUpdate(TimeSpan ts)
        {
            try
            {
                if (label3.IsHandleCreated) 
                    label3.Invoke(new Action(() => 
                        label3.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",ts.Hours, ts.Minutes, ts.Seconds,ts.Milliseconds / 10
                    )));
            }
            catch (Exception e)
            {
                MessageBox.Show("Some kind of invoke error: " + e.Message);
            }
        }
        
        // Button "Stop"
        public void button3_Click(object sender, EventArgs e)
        {
            StopTheServer();
        }

        // Disconnecting the server
        void StopTheServer()
        {
            if (gm != null)
            {                
                gm.Disconnect();
                gm = null;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                label2.Visible = false;
                textBox2.Visible = false;
                textBox1.Text = "300";
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                label2.Visible = true;
                textBox2.Visible = true;
                textBox1.Text = "15";
                textBox2.Text = "15";
            }
        }
    }
}
