using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMAP2IMAPTransfer
{
    public struct MailServerSettings
    {
        public string Hostname;
        public string Username;
        public string Password;
        public int Port;
        public bool UseSSL;
    }
}
