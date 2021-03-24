using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ImapX;

namespace IMAP2IMAPTransfer
{
    public class SimpleImap
    {
        ImapClient activeClient;
        public ImapClient Client
        {
            get { if (activeClient == null)
                    activeClient = new ImapClient();
                return activeClient;
            }
        }

        MailServerSettings serverSettings;
        public MailServerSettings ServerSettings
        {
            get { return serverSettings; }
            set { serverSettings = value; }
        }

        public SimpleImap()
        {

        }

        public void Update()
        {
            if(!Client.IsConnected)
            {
                try
                {
                    if(!Client.Connect())
                    {
                        Client.Host = serverSettings.Hostname;
                        Client.Port = serverSettings.Port;
                        Client.UseSsl = serverSettings.UseSSL;
                        Client.Connect();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if(!Client.IsAuthenticated)
            {
                Client.Login(serverSettings.Username, serverSettings.Password);
            }
        }

        public void Disconnect()
        {
            if (Client.IsConnected)
                Client.Disconnect();
        }

        public int CountAllMessages()
        {
            int total = 0;
            foreach(Folder f in Client.Folders)
            {
                total += GetFolderCount(f);
            }
            return total;
        }

        protected int GetFolderCount(Folder f)
        {
            int subTotal = 0;

            if(f.HasChildren)
            {
                foreach(Folder sf in f.SubFolders)
                {
                    subTotal += GetFolderCount(sf);
                }
            }

            subTotal += (int)f.Exists;
            return subTotal;
        }

        public List<string> GetFolderPaths()
        {
            List<Folder> folders = GetFolders();
            List<string> folderPaths = new List<string>();

            foreach(Folder f in folders)
            {
                folderPaths.Add(f.Path);
            }

            return folderPaths;
        }

        public List<Folder> GetFolders()
        {
            Update();
            if (!Client.IsConnected)
                throw new Exception("A valid IMAP connection must exist before getting folder list");

            List<Folder> folders = new List<Folder>();

            foreach(Folder f in Client.Folders)
            {
                folders.Add(f);

                if(f.HasChildren)
                {
                    foreach(Folder sf in f.SubFolders)
                    {
                        folders.AddRange(GetSubFolders(sf));
                    }
                }
            }
            return folders;
        }

        protected List<Folder> GetSubFolders(Folder f)
        {
            List<Folder> folders = new List<Folder>();
            foreach(Folder sf in f.SubFolders)
            {
                folders.Add(sf);
                if (sf.HasChildren)
                    folders.AddRange(GetSubFolders(sf));
            }

            return folders;
        }

        public Folder GetFolderByName(string name)
        {
            var folders = Client.Folders.Where(f => f.Name.Equals(name));

            if (folders.Count() > 0)
                return folders.First();
            return null;
        }

        public void DownloadMessageHeaders(Folder folder)
        {
            ImapX.Enums.MessageFetchMode messageFetchMode = Client.Behavior.MessageFetchMode;
            Client.Behavior.MessageFetchMode = ImapX.Enums.MessageFetchMode.Headers;

            folder.Messages.Download();

            Client.Behavior.MessageFetchMode = messageFetchMode;
        }

        public IEnumerable<Message> DownloadMessages(Folder folder, int start, int count)
        {
            Client.Behavior.MessageFetchMode = ImapX.Enums.MessageFetchMode.Full;
            folder.Messages.Download();
            IEnumerable<Message> messages = folder.Messages.Skip(start).Take(count);
            return messages;
        }

        public void UploadMessages(IEnumerable<Message> messages, string path, ref int messagesTransferred)
        {
            Update();

            Folder targetFolder = SelectFolder(path);

            if (targetFolder == null)
            {
                SimpleLog.Instance.Log("Target folder is null");
                return; //Throw some kind of error here so we don't forget why this isn't working in 6 months
            }

            try
            {
                if(messages.Count() == 0)
                {
                    return;
                }
                foreach (Message m in messages)
                {
                    m.Download(ImapX.Enums.MessageFetchMode.Full);
                    if (targetFolder.AppendMessage(m.ToEml()))
                    {
                        SimpleLog.Instance.Log("Succesfully uploaded message " + m.Subject);
                        messagesTransferred++;
                    }
                    else
                        SimpleLog.Instance.Log("Failed to upload message " + m.Subject);
                }
            }
            catch(Exception ex)
            {
                SimpleLog.Instance.Log("Exception occured while attempting to upload files " + ex.Message);
            }
        }

        public Folder SelectFolder(string path)
        {
            string[] pathParts = path.Split('.');
            Folder folder = null;
            Update();

            foreach (string name in pathParts)
            {
                string fName = name.Replace(' ', '-');

                if(folder == null)
                {
                    folder = Client.Folders.Where(f => f.Name.Equals(name)).FirstOrDefault();

                    if (folder == null)
                        return null;
                }
                else
                {
                    Folder newFolder = null;
                    try
                    {
                        newFolder = folder.SubFolders[name];
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    if (newFolder == null)
                        newFolder = folder.SubFolders.Add(fName);
                    folder = newFolder;
                }
            }

            return folder;
        }

        public void CreateFolder(Folder folder, string name)
        {
            if (folder.SubFolders.Where(sf => sf.Name.Equals(name)).Count() == 0)
            {
                Update();
                folder.SubFolders.Add(name);
            }
        }
    }
}
