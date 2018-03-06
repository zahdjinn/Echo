﻿using System.Windows.Forms;

namespace DAWindower
{
    public partial class MessageDialog : Form
    {
        internal static DialogResult Show(IWin32Window owner, string msg)
        {
            using (MessageDialog message = new MessageDialog(msg))
                return message.ShowDialog(owner);
        }

        public MessageDialog(string msg)
        {
            InitializeComponent();
            messageLbl.Text = msg;
        }
    }
}