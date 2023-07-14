namespace SniffUART {
    partial class FrmDecodeMessages {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tBoxMessages = new System.Windows.Forms.TextBox();
            this.butDecode = new System.Windows.Forms.Button();
            this.butClear = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tBoxMessages
            // 
            this.tBoxMessages.AllowDrop = true;
            this.tBoxMessages.Dock = System.Windows.Forms.DockStyle.Top;
            this.tBoxMessages.Location = new System.Drawing.Point(0, 0);
            this.tBoxMessages.Multiline = true;
            this.tBoxMessages.Name = "tBoxMessages";
            this.tBoxMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tBoxMessages.Size = new System.Drawing.Size(800, 398);
            this.tBoxMessages.TabIndex = 0;
            // 
            // butDecode
            // 
            this.butDecode.Location = new System.Drawing.Point(13, 415);
            this.butDecode.Name = "butDecode";
            this.butDecode.Size = new System.Drawing.Size(75, 23);
            this.butDecode.TabIndex = 1;
            this.butDecode.Text = "Decode";
            this.butDecode.UseVisualStyleBackColor = true;
            this.butDecode.Click += new System.EventHandler(this.butDecode_Click);
            // 
            // butClear
            // 
            this.butClear.Location = new System.Drawing.Point(94, 415);
            this.butClear.Name = "butClear";
            this.butClear.Size = new System.Drawing.Size(75, 23);
            this.butClear.TabIndex = 2;
            this.butClear.Text = "Clear";
            this.butClear.UseVisualStyleBackColor = true;
            this.butClear.Click += new System.EventHandler(this.butClear_Click);
            // 
            // FrmDecodeMessages
            // 
            this.AcceptButton = this.butDecode;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.butClear);
            this.Controls.Add(this.butDecode);
            this.Controls.Add(this.tBoxMessages);
            this.Name = "FrmDecodeMessages";
            this.Text = "Decode Messages";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmDecodeMessages_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tBoxMessages;
        private System.Windows.Forms.Button butDecode;
        private System.Windows.Forms.Button butClear;
    }
}