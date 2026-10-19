
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MulticastChat
{
    public partial class Form1 : Form
    {
        TextBox txtName = new TextBox();
        TextBox txtMessage = new TextBox();
        Button btnJoin = new Button();
        Button btnSend = new Button();
        ListBox lstUsers = new ListBox();
        ListBox lstChat = new ListBox();

        UdpClient receiver;
        UdpClient sender;
        string group = "239.0.0.222";
        int port = 5000;
        string userName = "";
        bool connected = false;

        HashSet<string> users = new HashSet<string>();

        public Form1()
        {
            InitializeComponent();

            Text = "Multicast Chat";
            Size = new Size(700, 450);

            Label nameLabel = new Label();
            nameLabel.Text = "Имя:";
            nameLabel.Location = new Point(10, 15);
            nameLabel.AutoSize = true;

            txtName.Location = new Point(70, 10);
            txtName.Width = 150;

            btnJoin.Text = "Подключиться";
            btnJoin.Location = new Point(240, 8);
            btnJoin.Width = 120;
            btnJoin.Click += BtnJoin_Click;

            lstChat.Location = new Point(10, 50);
            lstChat.Size = new Size(450, 300);

            lstUsers.Location = new Point(480, 50);
            lstUsers.Size = new Size(180, 300);

            txtMessage.Location = new Point(10, 365);
            txtMessage.Width = 450;

            btnSend.Text = "Отправить";
            btnSend.Location = new Point(480, 363);
            btnSend.Width = 100;
            btnSend.Enabled = false;
            btnSend.Click += BtnSend_Click;

            Controls.Add(nameLabel);
            Controls.Add(txtName);
            Controls.Add(btnJoin);
            Controls.Add(lstChat);
            Controls.Add(lstUsers);
            Controls.Add(txtMessage);
            Controls.Add(btnSend);

            FormClosing += Form1_FormClosing;
        }

        void BtnJoin_Click(object s, EventArgs e)
        {
            userName = txtName.Text.Trim();

            if (userName == "")
            {
                MessageBox.Show("Введите имя");
                return;
            }

            try
            {
                receiver = new UdpClient();
                receiver.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress, true);

                receiver.Client.Bind(
                    new IPEndPoint(IPAddress.Any, port));

                receiver.JoinMulticastGroup(
                    IPAddress.Parse(group));

                sender = new UdpClient();
                connected = true;

                btnJoin.Enabled = false;
                txtName.Enabled = false;
                btnSend.Enabled = true;

                users.Add(userName);
                UpdateUsers();

                SendMessage("JOIN|" + userName);
                lstChat.Items.Add("Вы подключились к чату.");

                Task.Run(ReceiveMessages);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        async Task ReceiveMessages()
        {
            while (connected)
            {
                try
                {
                    var result = await receiver.ReceiveAsync();
                    string msg = Encoding.UTF8.GetString(result.Buffer);

                    BeginInvoke(new Action(() =>
                    {
                        string[] p = msg.Split(new char[] { '|' }, 3);

                        if (p.Length < 2 || p[1] == userName)
                            return;

                        if (p[0] == "JOIN")
                        {
                            users.Add(p[1]);
                            UpdateUsers();
                            SendMessage("ONLINE|" + userName);
                            lstChat.Items.Add(p[1] + " подключился");
                        }
                        else if (p[0] == "ONLINE")
                        {
                            users.Add(p[1]);
                            UpdateUsers();
                        }
                        else if (p[0] == "LEAVE")
                        {
                            users.Remove(p[1]);
                            UpdateUsers();
                            lstChat.Items.Add(p[1] + " отключился");
                        }
                        else if (p[0] == "MSG" && p.Length == 3)
                        {
                            lstChat.Items.Add(p[1] + ": " + p[2]);
                        }
                    }));
                }
                catch
                {
                    break;
                }
            }
        }

        void BtnSend_Click(object s, EventArgs e)
        {
            string msg = txtMessage.Text.Trim();

            if (!connected || msg == "")
                return;

            SendMessage("MSG|" + userName + "|" + msg);
            lstChat.Items.Add("Вы: " + msg);
            txtMessage.Clear();
        }

        void SendMessage(string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);

            sender.Send(data, data.Length,
                new IPEndPoint(IPAddress.Parse(group), port));
        }

        void UpdateUsers()
        {
            lstUsers.Items.Clear();

            foreach (string user in users)
                lstUsers.Items.Add(user);
        }

        void Form1_FormClosing(object s, FormClosingEventArgs e)
        {
            if (connected)
            {
                SendMessage("LEAVE|" + userName);
                connected = false;
            }

            receiver?.Close();
            sender?.Close();
        }
    }
}
