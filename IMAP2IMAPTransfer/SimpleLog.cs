using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace IMAP2IMAPTransfer
{
    public class SimpleLog : Collection<string>
    {
        public event EventHandler EntryAdded;

        static SimpleLog instance;
        public static SimpleLog Instance
        {
            get
            {
                if (instance == null)
                    instance = new SimpleLog();
                return instance;
            }
        }

        public SimpleLog()
        {
            EntryAdded = new EventHandler(OnEntryAdded);
        }

        public void Log(string entry)
        {
            string date = DateTime.Now.ToString();
            string formattedEntry = string.Format("[{0}] {1}", date, entry);
            Items.Add(formattedEntry);
            EntryAdded(this, EventArgs.Empty);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach(string line in Items)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        protected virtual void OnEntryAdded(object sender, EventArgs e)
        {

        }
    }
}
