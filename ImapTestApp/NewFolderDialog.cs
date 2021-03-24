using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ImapTestApp
{
    public partial class NewFolderDialog : Form
    {
        public string ParentFolder
        {
            get { return comboBox1.Text; }
            set { comboBox1.Text = value; }
        }

        public string FolderName
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }

        public ComboBox FolderList
        {
            get { return comboBox1; }
        }

        public NewFolderDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
