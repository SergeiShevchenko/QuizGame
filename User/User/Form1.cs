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
namespace User
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Client's name
        string username = "";

        // Main game class
        ClientMaster cm;

        // Connect to a server, if it was not done yet
        private void button1_Click(object sender, EventArgs e)
        {
            if (cm == null || cm.IsClientRunning == false)
            {
                cm = new ClientMaster(this);
                cm.Connect();
                username = textBox1.Text;
                cm.Send("Username*" + username);                
            }
            else
            {
                MessageBox.Show("The connection has already been established.");
            }
        }

        // Show a question on form
        public void QuestionArrived(string newText)
        {
            textBox3.Invoke(new Action(() =>
            {
                textBox3.Text = newText;
                textBox2.Text = "";
            }));
            if (button2.Enabled == true)
            {
                button2_Click(this, null);
            }
            SendButtonControl(true);
        }

        // Show how much time got left on form
        public void TimeArrived(string newText)
        {
            label2.Invoke(new Action<string>((s) => label2.Text = s), newText);
        }

        // Sending an answer and determining whether its right or wrong 
        // ------->(maybe we need to leave it to the server) <------
        private void button2_Click(object sender, EventArgs e)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer();
            checkBox1.Invoke(new Action<string>((s) => checkBox1.Text = s), cm.RightAnswer);
            if (textBox2.Text == cm.RightAnswer)
            {
                player.SoundLocation = "right.wav";
                checkBox1.Invoke(new Action<bool>((b) => checkBox1.Checked = b), true);
            }
            else
            {
                player.SoundLocation = "wrong.wav";
                checkBox1.Invoke(new Action<bool>((b) => checkBox1.Checked = b), false);
            }
            cm.Send("Answer*" + textBox2.Text);
            SendButtonControl(false);
            player.Play();
        }

        public void SendButtonControl(bool enabled)
        {
            button2.Invoke(new Action(() => button2.Enabled = enabled));
        }
        
        // Show the scoreboard
        public void Stats(string s)
        {
            dataGridView1.Invoke(new Action<string>(FreshUp), s);
        }

        public void FreshUp(string s)
        {
            string[] tokens = s.Split('_');
            dataGridView1.Rows.Clear();
            for (int i = 0; i < tokens.Count()/2; i++)
            {
                dataGridView1.Rows.Add();
                dataGridView1.Rows[i].Cells[0].Value = tokens[2*i];
                dataGridView1.Rows[i].Cells[1].Value = tokens[2*i+1];
            }
        }

        // Close the connections when the form is closed
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (cm != null)
            {
                cm.Disconnect();
                cm.KillAllThreads();
                cm = null;
            }
            Application.Exit();
        }

    }
}
