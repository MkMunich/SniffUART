﻿namespace SniffUART
{
    partial class FormMain
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.MenuStripMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.portsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UART1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UART2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.OnePortToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenPortsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.SaveSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.SaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ClearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.ToggleTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToggleDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SearchToolStripTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.DGVData = new System.Windows.Forms.DataGridView();
            this.ColPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColDeltaTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColHex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColASCII = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StatusStripMain = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusUART0 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusUART1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.continueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripStatusDeltaTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.MenuStripMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DGVData)).BeginInit();
            this.StatusStripMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuStripMain
            // 
            this.MenuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.HelpToolStripMenuItem,
            this.SearchToolStripTextBox});
            this.MenuStripMain.Location = new System.Drawing.Point(0, 0);
            this.MenuStripMain.Name = "MenuStripMain";
            this.MenuStripMain.Size = new System.Drawing.Size(1163, 27);
            this.MenuStripMain.TabIndex = 0;
            this.MenuStripMain.Text = "menuMain";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.portsToolStripMenuItem,
            this.OpenPortsToolStripMenuItem,
            this.toolStripSeparator5,
            this.SaveSettingsToolStripMenuItem,
            this.toolStripSeparator1,
            this.SaveToolStripMenuItem,
            this.CloseToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 23);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // portsToolStripMenuItem
            // 
            this.portsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UART1ToolStripMenuItem,
            this.UART2ToolStripMenuItem,
            this.toolStripSeparator4,
            this.OnePortToolStripMenuItem});
            this.portsToolStripMenuItem.Name = "portsToolStripMenuItem";
            this.portsToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.portsToolStripMenuItem.Text = "Port Settings";
            // 
            // UART1ToolStripMenuItem
            // 
            this.UART1ToolStripMenuItem.Name = "UART1ToolStripMenuItem";
            this.UART1ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D0)));
            this.UART1ToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.UART1ToolStripMenuItem.Text = "UART 0";
            this.UART1ToolStripMenuItem.Click += new System.EventHandler(this.UART0ToolStripMenuItem_Click);
            // 
            // UART2ToolStripMenuItem
            // 
            this.UART2ToolStripMenuItem.Name = "UART2ToolStripMenuItem";
            this.UART2ToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D1)));
            this.UART2ToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.UART2ToolStripMenuItem.Text = "UART 1";
            this.UART2ToolStripMenuItem.Click += new System.EventHandler(this.UART1ToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(144, 6);
            // 
            // OnePortToolStripMenuItem
            // 
            this.OnePortToolStripMenuItem.Checked = true;
            this.OnePortToolStripMenuItem.CheckOnClick = true;
            this.OnePortToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OnePortToolStripMenuItem.Name = "OnePortToolStripMenuItem";
            this.OnePortToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.OnePortToolStripMenuItem.Text = "One Port";
            this.OnePortToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.OnePortToolStripMenuItem_CheckStateChanged);
            // 
            // OpenPortsToolStripMenuItem
            // 
            this.OpenPortsToolStripMenuItem.Name = "OpenPortsToolStripMenuItem";
            this.OpenPortsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.O)));
            this.OpenPortsToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.OpenPortsToolStripMenuItem.Text = "Open Ports";
            this.OpenPortsToolStripMenuItem.Click += new System.EventHandler(this.OpenPortsToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(176, 6);
            // 
            // SaveSettingsToolStripMenuItem
            // 
            this.SaveSettingsToolStripMenuItem.Name = "SaveSettingsToolStripMenuItem";
            this.SaveSettingsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.S)));
            this.SaveSettingsToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.SaveSettingsToolStripMenuItem.Text = "Save Settings";
            this.SaveSettingsToolStripMenuItem.Click += new System.EventHandler(this.SaveSettingsToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(176, 6);
            // 
            // SaveToolStripMenuItem
            // 
            this.SaveToolStripMenuItem.Name = "SaveToolStripMenuItem";
            this.SaveToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.SaveToolStripMenuItem.Text = "Save as...";
            this.SaveToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
            // 
            // CloseToolStripMenuItem
            // 
            this.CloseToolStripMenuItem.Name = "CloseToolStripMenuItem";
            this.CloseToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.CloseToolStripMenuItem.Text = "Close";
            this.CloseToolStripMenuItem.Click += new System.EventHandler(this.CloseToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearToolStripMenuItem,
            this.toolStripSeparator2,
            this.ToggleTimeToolStripMenuItem,
            this.ToggleDataToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 23);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // ClearToolStripMenuItem
            // 
            this.ClearToolStripMenuItem.Name = "ClearToolStripMenuItem";
            this.ClearToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.C)));
            this.ClearToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.ClearToolStripMenuItem.Text = "Clear";
            this.ClearToolStripMenuItem.Click += new System.EventHandler(this.ClearToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(171, 6);
            // 
            // ToggleTimeToolStripMenuItem
            // 
            this.ToggleTimeToolStripMenuItem.Checked = true;
            this.ToggleTimeToolStripMenuItem.CheckOnClick = true;
            this.ToggleTimeToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToggleTimeToolStripMenuItem.Name = "ToggleTimeToolStripMenuItem";
            this.ToggleTimeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.T)));
            this.ToggleTimeToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.ToggleTimeToolStripMenuItem.Text = "Toggle Time";
            this.ToggleTimeToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.ToggleTimeToolStripMenuItem_CheckStateChanged);
            // 
            // ToggleDataToolStripMenuItem
            // 
            this.ToggleDataToolStripMenuItem.Checked = true;
            this.ToggleDataToolStripMenuItem.CheckOnClick = true;
            this.ToggleDataToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ToggleDataToolStripMenuItem.Name = "ToggleDataToolStripMenuItem";
            this.ToggleDataToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.H)));
            this.ToggleDataToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.ToggleDataToolStripMenuItem.Text = "Toggle Hex";
            this.ToggleDataToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.ToggleDataToolStripMenuItem_CheckStateChanged);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showHelpToolStripMenuItem});
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(44, 23);
            this.HelpToolStripMenuItem.Text = "Help";
            // 
            // showHelpToolStripMenuItem
            // 
            this.showHelpToolStripMenuItem.Name = "showHelpToolStripMenuItem";
            this.showHelpToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.showHelpToolStripMenuItem.Text = "Show Help";
            // 
            // SearchToolStripTextBox
            // 
            this.SearchToolStripTextBox.AcceptsReturn = true;
            this.SearchToolStripTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.SearchToolStripTextBox.Name = "SearchToolStripTextBox";
            this.SearchToolStripTextBox.Size = new System.Drawing.Size(100, 23);
            // 
            // DGVData
            // 
            this.DGVData.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColPort,
            this.ColDeltaTime,
            this.ColTime,
            this.ColHex,
            this.ColASCII});
            this.DGVData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DGVData.Location = new System.Drawing.Point(0, 27);
            this.DGVData.Name = "DGVData";
            this.DGVData.Size = new System.Drawing.Size(1163, 586);
            this.DGVData.TabIndex = 1;
            this.DGVData.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DGVData_CellContentClick);
            this.DGVData.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.DGVData_ColumnWidthChanged);
            this.DGVData.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.DGVData_RowEnter);
            // 
            // ColPort
            // 
            this.ColPort.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ColPort.Frozen = true;
            this.ColPort.HeaderText = "Port";
            this.ColPort.MinimumWidth = 50;
            this.ColPort.Name = "ColPort";
            this.ColPort.ReadOnly = true;
            this.ColPort.Width = 51;
            // 
            // ColDeltaTime
            // 
            this.ColDeltaTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ColDeltaTime.FillWeight = 200F;
            this.ColDeltaTime.HeaderText = "Delta Time";
            this.ColDeltaTime.MinimumWidth = 120;
            this.ColDeltaTime.Name = "ColDeltaTime";
            this.ColDeltaTime.ReadOnly = true;
            this.ColDeltaTime.Width = 120;
            // 
            // ColTime
            // 
            this.ColTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ColTime.FillWeight = 200F;
            this.ColTime.HeaderText = "Time";
            this.ColTime.MinimumWidth = 120;
            this.ColTime.Name = "ColTime";
            this.ColTime.Width = 120;
            // 
            // ColHex
            // 
            this.ColHex.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColHex.FillWeight = 800F;
            this.ColHex.HeaderText = "Hex";
            this.ColHex.MinimumWidth = 1000;
            this.ColHex.Name = "ColHex";
            this.ColHex.ReadOnly = true;
            // 
            // ColASCII
            // 
            this.ColASCII.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColASCII.FillWeight = 800F;
            this.ColASCII.HeaderText = "ASCII";
            this.ColASCII.MinimumWidth = 1000;
            this.ColASCII.Name = "ColASCII";
            // 
            // StatusStripMain
            // 
            this.StatusStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusUART0,
            this.toolStripStatusUART1,
            this.toolStripSplitButton1,
            this.toolStripStatusDeltaTime});
            this.StatusStripMain.Location = new System.Drawing.Point(0, 591);
            this.StatusStripMain.Name = "StatusStripMain";
            this.StatusStripMain.Size = new System.Drawing.Size(1163, 22);
            this.StatusStripMain.TabIndex = 2;
            this.StatusStripMain.Text = "Main";
            // 
            // toolStripStatusUART0
            // 
            this.toolStripStatusUART0.Name = "toolStripStatusUART0";
            this.toolStripStatusUART0.Size = new System.Drawing.Size(44, 17);
            this.toolStripStatusUART0.Text = "UART 0";
            // 
            // toolStripStatusUART1
            // 
            this.toolStripStatusUART1.Name = "toolStripStatusUART1";
            this.toolStripStatusUART1.Size = new System.Drawing.Size(44, 17);
            this.toolStripStatusUART1.Text = "UART 1";
            // 
            // toolStripSplitButton1
            // 
            this.toolStripSplitButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSplitButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startToolStripMenuItem,
            this.stopToolStripMenuItem,
            this.continueToolStripMenuItem});
            this.toolStripSplitButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton1.Image")));
            this.toolStripSplitButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Size = new System.Drawing.Size(32, 20);
            this.toolStripSplitButton1.Text = "Run";
            // 
            // startToolStripMenuItem
            // 
            this.startToolStripMenuItem.Name = "startToolStripMenuItem";
            this.startToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.startToolStripMenuItem.Text = "Start";
            this.startToolStripMenuItem.Click += new System.EventHandler(this.StartToolStripMenuItem_Click);
            // 
            // stopToolStripMenuItem
            // 
            this.stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            this.stopToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.stopToolStripMenuItem.Text = "Stop";
            this.stopToolStripMenuItem.Click += new System.EventHandler(this.StopToolStripMenuItem_Click);
            // 
            // continueToolStripMenuItem
            // 
            this.continueToolStripMenuItem.Name = "continueToolStripMenuItem";
            this.continueToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.continueToolStripMenuItem.Text = "Continue";
            this.continueToolStripMenuItem.Click += new System.EventHandler(this.ContinueToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(178, 6);
            // 
            // toolStripStatusDeltaTime
            // 
            this.toolStripStatusDeltaTime.Name = "toolStripStatusDeltaTime";
            this.toolStripStatusDeltaTime.Size = new System.Drawing.Size(63, 17);
            this.toolStripStatusDeltaTime.Text = "Delta Time";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1163, 613);
            this.Controls.Add(this.StatusStripMain);
            this.Controls.Add(this.DGVData);
            this.Controls.Add(this.MenuStripMain);
            this.MainMenuStrip = this.MenuStripMain;
            this.Name = "FormMain";
            this.Text = "Sniff UART";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormMain_FormClosed);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.MenuStripMain.ResumeLayout(false);
            this.MenuStripMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DGVData)).EndInit();
            this.StatusStripMain.ResumeLayout(false);
            this.StatusStripMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MenuStripMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem portsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UART1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem UART2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SaveToolStripMenuItem;
        private System.Windows.Forms.DataGridView DGVData;
        private System.Windows.Forms.ToolStripMenuItem ClearToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.StatusStrip StatusStripMain;
        public System.Windows.Forms.ToolStripStatusLabel toolStripStatusUART0;
        public System.Windows.Forms.ToolStripStatusLabel toolStripStatusUART1;
        private System.Windows.Forms.ToolStripMenuItem showHelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
        private System.Windows.Forms.ToolStripMenuItem startToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem continueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SaveSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripTextBox SearchToolStripTextBox;
        private System.Windows.Forms.ToolStripMenuItem CloseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToggleTimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToggleDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        public System.Windows.Forms.ToolStripMenuItem OnePortToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem OpenPortsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColPort;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColDeltaTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColHex;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColASCII;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusDeltaTime;
    }
}

