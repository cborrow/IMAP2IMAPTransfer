using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ImapX;

namespace ImapTestApp
{
    public partial class Form1 : Form
    {
        NewFolderDialog newFolderDialog;
        ImapClient client;

        string hostName = string.Empty;
        string userName = string.Empty;
        string passWord = string.Empty;
        int portNumber = 143;

        public Form1()
        {
            InitializeComponent();

            newFolderDialog = new NewFolderDialog();
            client = new ImapClient();
        }

        protected void ConnectAndAuthenticate()
        {
            hostName = textBox1.Text;
            userName = textBox2.Text;
            passWord = textBox3.Text;
            portNumber = Convert.ToInt32(textBox4.Text);

            if (string.IsNullOrEmpty(hostName) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(passWord))
            {
                MessageBox.Show("A valid hostname, username, and password must be provided. Please check and try again");
                return;
            }

            if(!client.IsConnected)
            {
                client.Host = hostName;
                client.Port = portNumber;

                if (portNumber == 993)
                    client.UseSsl = true;
                else
                    client.UseSsl = false;

                if (!client.Connect())
                {
                    MessageBox.Show("Unable to connect to host. Please check hostname and port and try again");
                }
            }
            if(client.IsConnected && !client.IsAuthenticated)
            {
                if(!client.Login(userName, passWord))
                {
                    MessageBox.Show("Incorrect username or password provided. Please check and try again");
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectAndAuthenticate();

                if (client.IsConnected && client.IsAuthenticated)
                {
                    MessageBox.Show("Connected and authenticated successfully");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("An exception occured\r\n\r\n" + ex.ToString());
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ConnectAndAuthenticate();

            if(client.IsConnected && client.IsAuthenticated)
            {
                foreach(Folder f in client.Folders)
                {
                    AddFolders(f);
                }
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ConnectAndAuthenticate();

            string path = comboBox1.Text;

            if (client.IsConnected && client.IsAuthenticated)
            {
                Folder folder = SelectFolder(path);
                listBox1.Items.Clear();

                if (folder != null)
                {
                    client.Behavior.MessageFetchMode = ImapX.Enums.MessageFetchMode.Headers;
                    long count = folder.Exists;
                    int messagesDownloaded = 0;
                    int messagesPerBlock = (int)Math.Ceiling((decimal)(count / 5));

                    //Download only the headers
                    folder.Messages.Download();

                    while(messagesDownloaded < count)
                    {
                        foreach(ImapX.Message message in folder.Messages.Skip(messagesDownloaded).Take(messagesPerBlock))
                        {
                            listBox1.Items.Add(message.Subject);
                        }

                        messagesDownloaded += messagesPerBlock;
                    }
                    /*for (long i = 0; i < count; i += (count / 5))
                    {
                        folder.Messages.Download("ALL", ImapX.Enums.MessageFetchMode.Tiny, (int)(count / 5));

                        foreach (ImapX.Message message in folder.Messages)
                        {
                            listBox1.Items.Add(message.Subject);
                        }
                    }*/

                    //folder.Messages.Download();

                    
                }
            }
        }

        protected void AddFolders(Folder f)
        {
            comboBox1.Items.Add(f.Path);

            if (f.HasChildren)
            {
                foreach (Folder sf in f.SubFolders)
                {
                    AddFolders(sf);
                }
            }
        }

        protected Folder SelectFolder(string path)
        {
            string[] pathParts = path.Split('.');
            Folder folder = null;

            foreach(string name in pathParts)
            {
                if(folder == null)
                {
                    folder = client.Folders.Where(f => f.Name.Equals(name)).First();

                    if (folder == null)
                        return null;
                }
                else
                {
                    folder = folder.SubFolders.Where(f => f.Name.Equals(name)).First();
                }
            }

            return folder;
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ConnectAndAuthenticate();

            if(client.IsConnected && client.IsAuthenticated)
            {
                foreach(string item in comboBox1.Items)
                {
                    newFolderDialog.FolderList.Items.Add(item);
                }

                if(newFolderDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = newFolderDialog.ParentFolder;
                    string name = newFolderDialog.FolderName;

                    Folder parentFolder = SelectFolder(path);
                    parentFolder.SubFolders.Add(name);

                    comboBox1.Items.Clear();

                    foreach(Folder f in client.Folders)
                    {
                        AddFolders(f);
                    }
                }
            }
        }
    }
}
