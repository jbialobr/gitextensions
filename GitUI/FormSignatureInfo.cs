using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GitUI
{
    public partial class FormSignatureInfo : Form
    {
        public FormSignatureInfo()
        {
            InitializeComponent();
        }

        public void ShowGpgMessage(string message)
        {
            gpgTextBox.Text = message;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
