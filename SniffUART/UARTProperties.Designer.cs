namespace SniffUART
{
    partial class UARTProperties
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
            this.labUART = new System.Windows.Forms.Label();
            this.butCancel = new System.Windows.Forms.Button();
            this.butOK = new System.Windows.Forms.Button();
            this.cBoxHandshake = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cBoxStopBits = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tBoxDataBits = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cBoxParity = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cBoxBaudRate = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cBoxUART = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tBoxNo = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tBoxReadTimeout = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // labUART
            // 
            this.labUART.AutoSize = true;
            this.labUART.Location = new System.Drawing.Point(8, 9);
            this.labUART.Name = "labUART";
            this.labUART.Size = new System.Drawing.Size(37, 13);
            this.labUART.TabIndex = 30;
            this.labUART.Text = "UART";
            // 
            // butCancel
            // 
            this.butCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.butCancel.Location = new System.Drawing.Point(122, 334);
            this.butCancel.Name = "butCancel";
            this.butCancel.Size = new System.Drawing.Size(75, 23);
            this.butCancel.TabIndex = 29;
            this.butCancel.Text = "Cancel";
            this.butCancel.UseVisualStyleBackColor = true;
            this.butCancel.Click += new System.EventHandler(this.butCancel_Click);
            // 
            // butOK
            // 
            this.butOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.butOK.Location = new System.Drawing.Point(31, 334);
            this.butOK.Name = "butOK";
            this.butOK.Size = new System.Drawing.Size(75, 23);
            this.butOK.TabIndex = 28;
            this.butOK.Text = "OK";
            this.butOK.UseVisualStyleBackColor = true;
            this.butOK.Click += new System.EventHandler(this.butOK_Click);
            // 
            // cBoxHandshake
            // 
            this.cBoxHandshake.FormattingEnabled = true;
            this.cBoxHandshake.Location = new System.Drawing.Point(90, 240);
            this.cBoxHandshake.Name = "cBoxHandshake";
            this.cBoxHandshake.Size = new System.Drawing.Size(127, 21);
            this.cBoxHandshake.TabIndex = 27;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 243);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 13);
            this.label6.TabIndex = 26;
            this.label6.Text = "Handshake";
            // 
            // cBoxStopBits
            // 
            this.cBoxStopBits.FormattingEnabled = true;
            this.cBoxStopBits.Location = new System.Drawing.Point(90, 199);
            this.cBoxStopBits.Name = "cBoxStopBits";
            this.cBoxStopBits.Size = new System.Drawing.Size(127, 21);
            this.cBoxStopBits.TabIndex = 25;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 202);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 24;
            this.label5.Text = "Stop Bits";
            // 
            // tBoxDataBits
            // 
            this.tBoxDataBits.Location = new System.Drawing.Point(90, 155);
            this.tBoxDataBits.Name = "tBoxDataBits";
            this.tBoxDataBits.Size = new System.Drawing.Size(57, 20);
            this.tBoxDataBits.TabIndex = 23;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 158);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "Data Bits";
            // 
            // cBoxParity
            // 
            this.cBoxParity.FormattingEnabled = true;
            this.cBoxParity.Location = new System.Drawing.Point(90, 115);
            this.cBoxParity.Name = "cBoxParity";
            this.cBoxParity.Size = new System.Drawing.Size(127, 21);
            this.cBoxParity.TabIndex = 21;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 118);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 20;
            this.label3.Text = "Parity";
            // 
            // cBoxBaudRate
            // 
            this.cBoxBaudRate.FormattingEnabled = true;
            this.cBoxBaudRate.Location = new System.Drawing.Point(90, 73);
            this.cBoxBaudRate.Name = "cBoxBaudRate";
            this.cBoxBaudRate.Size = new System.Drawing.Size(127, 21);
            this.cBoxBaudRate.TabIndex = 19;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 18;
            this.label2.Text = "Baudrate";
            // 
            // cBoxUART
            // 
            this.cBoxUART.FormattingEnabled = true;
            this.cBoxUART.Location = new System.Drawing.Point(90, 38);
            this.cBoxUART.Name = "cBoxUART";
            this.cBoxUART.Size = new System.Drawing.Size(127, 21);
            this.cBoxUART.TabIndex = 17;
            this.cBoxUART.TextChanged += new System.EventHandler(this.cBoxUART_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "Port";
            // 
            // tBoxNo
            // 
            this.tBoxNo.Location = new System.Drawing.Point(90, 6);
            this.tBoxNo.Name = "tBoxNo";
            this.tBoxNo.ReadOnly = true;
            this.tBoxNo.Size = new System.Drawing.Size(57, 20);
            this.tBoxNo.TabIndex = 31;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 288);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(74, 13);
            this.label7.TabIndex = 32;
            this.label7.Text = "Read Timeout";
            // 
            // tBoxReadTimeout
            // 
            this.tBoxReadTimeout.Location = new System.Drawing.Point(90, 285);
            this.tBoxReadTimeout.Name = "tBoxReadTimeout";
            this.tBoxReadTimeout.Size = new System.Drawing.Size(57, 20);
            this.tBoxReadTimeout.TabIndex = 33;
            // 
            // UARTProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(230, 365);
            this.Controls.Add(this.tBoxReadTimeout);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tBoxNo);
            this.Controls.Add(this.labUART);
            this.Controls.Add(this.butCancel);
            this.Controls.Add(this.butOK);
            this.Controls.Add(this.cBoxHandshake);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cBoxStopBits);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tBoxDataBits);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cBoxParity);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cBoxBaudRate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cBoxUART);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "UARTProperties";
            this.Text = "UART Properties";
            this.Enter += new System.EventHandler(this.butOK_Click);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labUART;
        private System.Windows.Forms.Button butCancel;
        private System.Windows.Forms.Button butOK;
        private System.Windows.Forms.ComboBox cBoxHandshake;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cBoxStopBits;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tBoxDataBits;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cBoxParity;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cBoxBaudRate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cBoxUART;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tBoxNo;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tBoxReadTimeout;
    }
}