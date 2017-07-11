namespace GitUI
{
    partial class FormSignatureInfo
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSignatureInfo));
            this.closeBtn = new System.Windows.Forms.Button();
            this.gpgTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // closeBtn
            // 
            this.closeBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeBtn.Location = new System.Drawing.Point(329, 87);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(116, 26);
            this.closeBtn.TabIndex = 1;
            this.closeBtn.Text = "Close";
            this.closeBtn.UseVisualStyleBackColor = true;
            this.closeBtn.Click += new System.EventHandler(this.button1_Click);
            // 
            // gpgTextBox
            // 
            this.gpgTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gpgTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gpgTextBox.Location = new System.Drawing.Point(12, 12);
            this.gpgTextBox.Multiline = true;
            this.gpgTextBox.Name = "gpgTextBox";
            this.gpgTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gpgTextBox.Size = new System.Drawing.Size(433, 69);
            this.gpgTextBox.TabIndex = 2;
            // 
            // FormSignatureInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(457, 125);
            this.Controls.Add(this.gpgTextBox);
            this.Controls.Add(this.closeBtn);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormSignatureInfo";
            this.Text = "GPG Info";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button closeBtn;
        private System.Windows.Forms.TextBox gpgTextBox;
    }
}