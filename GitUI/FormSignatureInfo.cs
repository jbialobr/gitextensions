using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitCommands.Utils;

namespace GitUI
{
    public partial class FormSignatureInfo : GitExtensionsForm
    {
        public FormSignatureInfo()
        {
            InitializeComponent();
            Translate();
        }

        public void ShowGpgMessage(string message)
        {
            gpgTextBox.Text = EnvUtils.ReplaceLinuxNewLinesDependingOnPlatform(message);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
