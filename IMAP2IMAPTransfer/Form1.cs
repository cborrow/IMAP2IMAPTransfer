using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using ImapX;

namespace IMAP2IMAPTransfer
{
    public partial class Form1 : Form
    {
        MailServerSettings sourceServerSettings;
        MailServerSettings targetServerSettings;

        SimpleImap sourceImapClient;
        SimpleImap targetImapClient;

        int transferredMessageCount = 0;
        int totalSourceMessages = 0;
        int messagesPerTransfer = 50;

        bool transferInProgress = false;
        Thread transferThread = null;

        protected delegate void AddToTotalTransferCountDel(int count);
        protected delegate IEnumerable<string> GetSelectedFoldersInvoker();

        public Form1()
        {
            InitializeComponent();

            SimpleLog.Instance.EntryAdded += SimpleLog_EntryAdded;
        }

        protected void UpdateSourceServerSettings()
        {
            string hostname = sourceHostnameTextBox.Text;
            string username = sourceUsernameTextBox.Text;
            string password = sourcePasswordTextBox.Text;
            int port = (int)sourcePortNumeric.Value;
            bool ssl = checkBox1.Checked;

            if (!string.IsNullOrEmpty(hostname) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                sourceServerSettings = new MailServerSettings();
                sourceServerSettings.Hostname = hostname;
                sourceServerSettings.Username = username;
                sourceServerSettings.Password = password;
                sourceServerSettings.Port = (port != 0) ? port : 143;
                sourceServerSettings.UseSSL = ssl;
            }
            else
            {
                MessageBox.Show("The source settings must contain at minimum a valid hostname, username, and password");
            }
        }

        protected void UpdateTargetServerSettings()
        {
            string hostname = targetHostnameTextBox.Text;
            string username = targetUsernameTextBox.Text;
            string password = targetPasswordTextBox.Text;
            int port = (int)targetPortNumeric.Value;
            bool ssl = checkBox2.Checked;

            if (!string.IsNullOrEmpty(hostname) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                targetServerSettings = new MailServerSettings();
                targetServerSettings.Hostname = hostname;
                targetServerSettings.Username = username;
                targetServerSettings.Password = password;
                targetServerSettings.Port = (port != 0) ? port : 143;
                targetServerSettings.UseSSL = ssl;
            }
            else
            {
                MessageBox.Show("The target settings must contain at minimum a valid hostname, username, and password");
            }
        }

        protected delegate void SetLogTextDel(string text);
        protected void SetLogText(string text)
        {
            textBox1.Text = SimpleLog.Instance.ToString();
        }

        protected delegate void SetButtonTextDel(string text);
        protected void SetButtonText(string text)
        {
            button3.Text = text;
        }

        private void SimpleLog_EntryAdded(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new SetLogTextDel(SetLogText), SimpleLog.Instance.ToString());
            }
            else
                textBox1.Text = SimpleLog.Instance.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateSourceServerSettings();

            using (ImapClient client = new ImapClient(sourceServerSettings.Hostname, sourceServerSettings.Port, sourceServerSettings.UseSSL))
            {

                if (client != null)
                {
                    if (!client.IsConnected)
                        client.Connect();

                    if (client.Login(sourceServerSettings.Username, sourceServerSettings.Password))
                    {
                        client.Logout();
                        client.Disconnect();

                        MessageBox.Show("Connection succesful.");
                    }
                    else
                    {
                        MessageBox.Show("Username or password is incorrect, could not login");
                    }
                }
                else
                {
                    MessageBox.Show("Failed to connect to host, please check hostname, port and SSL option");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UpdateTargetServerSettings();

            using (ImapClient client = new ImapClient(targetServerSettings.Hostname, targetServerSettings.Port, targetServerSettings.UseSSL))
            {

                if (client != null)
                {
                    if (!client.IsConnected)
                        client.Connect();

                    if (client.Login(targetServerSettings.Username, targetServerSettings.Password))
                    {
                        client.Logout();
                        client.Disconnect();

                        MessageBox.Show("Connection succesful.");
                    }
                    else
                    {
                        MessageBox.Show("Username or password is incorrect, could not login");
                    }
                }
                else
                {
                    MessageBox.Show("Failed to connect to host, please check hostname, port and SSL option");
                }
            }
        }

        //Get folder list
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UpdateSourceServerSettings();

            using (ImapClient client = new ImapClient(sourceServerSettings.Hostname, sourceServerSettings.Port, sourceServerSettings.UseSSL))
            {

                if (client != null)
                {
                    if (!client.IsConnected)
                        client.Connect();

                    if (client.Login(sourceServerSettings.Username, sourceServerSettings.Password))
                    {
                        checkedListBox1.Items.Clear();

                        foreach(Folder topFolder in client.Folders)
                        {
                            AddSubFolders(topFolder);
                        }

                        client.Logout();
                        client.Disconnect();
                    }
                    else
                    {
                        MessageBox.Show("Username or password is incorrect, could not login");
                    }
                }
                else
                {
                    MessageBox.Show("Failed to connect to host, please check hostname, port and SSL option");
                }
            }
        }

        protected void AddSubFolders(Folder f)
        {
            checkedListBox1.Items.Add(f.Path);

            if(f.SubFolders.Count() > 0)
            {
                foreach(Folder sf in f.SubFolders)
                {
                    AddSubFolders(sf);
                }
            }
        }

        protected IEnumerable<string> GetSelectedFolders()
        {
            List<string> items = new List<string>();

            foreach(string selectedFolder in checkedListBox1.SelectedItems)
            {
                items.Add(selectedFolder);
            }

            return items;
        }

        protected List<ImapX.Message> GetAllMessages(ImapClient client, Folder folder)
        {
            List<ImapX.Message> messages = new List<ImapX.Message>();
            client.Behavior.MessageFetchMode = ImapX.Enums.MessageFetchMode.Headers;

            foreach(Folder f in client.Folders)
            {
                f.Messages.Download();
                messages.AddRange(f.Messages.ToArray());

                if(f.HasChildren)
                {
                    foreach(Folder sf in f.SubFolders)
                    {
                        sf.Messages.Download();
                        messages.AddRange(sf.Messages.ToArray());
                    }
                }
            }

            return messages;
        }

        //Select all items
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            for(int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        //Unselect all items
        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }
        }

        protected void AddToTotalMessageCount(int count)
        {
            transferredMessageCount += count;
            progressBar1.Value = transferredMessageCount;
        }

        protected void TransferMessages(Folder f)
        {
            int mCount = (int)f.Exists;
            int mPerTransfer = (mCount > messagesPerTransfer) ? messagesPerTransfer : mCount;
            int messagesTransferred = 0;

            SimpleLog.Instance.Log(string.Format("Downloading headers of {0} messages from {1}", mCount, f.Name));
            sourceImapClient.DownloadMessageHeaders(f);

            //Need to try and find a way to download blocks of emails at a time rather than all at once.
            //No way to easily download a group of messages. However, we can download headers and then download the full email before transferring.
            SimpleLog.Instance.Log(string.Format("Uploading {0} messages to {1} on target server", mCount, f.Name));

            while (messagesTransferred < mCount)
            {
                IEnumerable<ImapX.Message> messages = f.Messages.Skip(messagesTransferred).Take(messagesPerTransfer);
                targetImapClient.UploadMessages(messages, f.Path, ref messagesTransferred);
            }

            if(f.HasChildren)
            {
                foreach(Folder subFolder in f.SubFolders)
                {
                    TransferMessages(subFolder);
                }
            }

            transferredMessageCount += messagesTransferred;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (transferInProgress)
            {
                transferThread.Abort();
                transferInProgress = false;
                button3.Text = "Start Transfer";
            }
            else
            {
                transferThread = new Thread(new ThreadStart(delegate ()
                {
                    UpdateSourceServerSettings();
                    UpdateTargetServerSettings();

                    sourceImapClient = new SimpleImap();
                    sourceImapClient.ServerSettings = sourceServerSettings;

                    targetImapClient = new SimpleImap();
                    targetImapClient.ServerSettings = targetServerSettings;
                    transferredMessageCount = 0;

                    List<Folder> selectedFolders = null;

                    if (radioButton1.Checked)
                    {
                        selectedFolders = sourceImapClient.GetFolders();
                    }
                    else if (radioButton2.Checked)
                    {
                        selectedFolders = new List<Folder>();

                        //Fix Invoke issue for CrossThread operation.
                        //TODO: Create delegate to grab list of selected folders
                        IEnumerable<string> paths = (IEnumerable<string>)Invoke(new GetSelectedFoldersInvoker(GetSelectedFolders));

                        foreach (string folderPath in paths)
                        {
                            selectedFolders.Add(sourceImapClient.SelectFolder(folderPath));
                        }
                    }

                    try
                    {
                        sourceImapClient.Update();
                        totalSourceMessages = sourceImapClient.CountAllMessages();

                        progressBar1.Minimum = 0;
                        progressBar1.Maximum = totalSourceMessages;

                        Invoke(new SetButtonTextDel(SetButtonText), "Cancel Transfer");

                        foreach(Folder folder in selectedFolders)
                        {
                            SimpleLog.Instance.Log(string.Format("Opening folder {0}", folder.Name));
                            TransferMessages(folder);
                        }

                        transferInProgress = false;
                        SimpleLog.Instance.Log(string.Format("Transferred a total of {0} messages", transferredMessageCount));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        sourceImapClient.Disconnect();
                        Invoke(new SetButtonTextDel(SetButtonText), "Start Transfer");
                        transferInProgress = false;
                    }
                }));
                transferThread.Start();
                transferInProgress = true;
            }
        }
    }
}
