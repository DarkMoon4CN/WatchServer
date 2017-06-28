namespace SuperSocketForm
{
    partial class SuperSocketForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SuperSocketForm));
            this.labelSrvIP = new System.Windows.Forms.Label();
            this.textBoxSrvIP = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.numericUpDownPort = new System.Windows.Forms.NumericUpDown();
            this.buttonSendCommand = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonStartSrv = new System.Windows.Forms.Button();
            listBoxLogger = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            clientListcombox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            methodListcombox = new System.Windows.Forms.ComboBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToolStripMenuItemShow = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItemClose = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).BeginInit();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelSrvIP
            // 
            this.labelSrvIP.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelSrvIP.Location = new System.Drawing.Point(12, 498);
            this.labelSrvIP.Name = "labelSrvIP";
            this.labelSrvIP.Size = new System.Drawing.Size(100, 40);
            this.labelSrvIP.TabIndex = 1;
            this.labelSrvIP.Text = "服务地址:";
            this.labelSrvIP.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // textBoxSrvIP
            // 
            this.textBoxSrvIP.Enabled = false;
            this.textBoxSrvIP.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBoxSrvIP.Location = new System.Drawing.Point(149, 506);
            this.textBoxSrvIP.Name = "textBoxSrvIP";
            this.textBoxSrvIP.ReadOnly = true;
            this.textBoxSrvIP.Size = new System.Drawing.Size(120, 29);
            this.textBoxSrvIP.TabIndex = 2;
            this.textBoxSrvIP.Text = "127.0.0.1";
            this.textBoxSrvIP.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelPort
            // 
            this.labelPort.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelPort.Location = new System.Drawing.Point(351, 498);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(80, 40);
            this.labelPort.TabIndex = 3;
            this.labelPort.Text = "端口:";
            this.labelPort.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // numericUpDownPort
            // 
            this.numericUpDownPort.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.numericUpDownPort.Location = new System.Drawing.Point(455, 506);
            this.numericUpDownPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numericUpDownPort.Name = "numericUpDownPort";
            this.numericUpDownPort.Size = new System.Drawing.Size(80, 29);
            this.numericUpDownPort.TabIndex = 4;
            this.numericUpDownPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numericUpDownPort.Value = new decimal(new int[] {
            5015,
            0,
            0,
            0});
            // 
            // buttonSendCommand
            // 
            this.buttonSendCommand.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonSendCommand.Location = new System.Drawing.Point(592, 568);
            this.buttonSendCommand.Name = "buttonSendCommand";
            this.buttonSendCommand.Size = new System.Drawing.Size(100, 40);
            this.buttonSendCommand.TabIndex = 5;
            this.buttonSendCommand.Text = "下发指令";
            this.buttonSendCommand.UseVisualStyleBackColor = true;
            this.buttonSendCommand.Click += new System.EventHandler(this.buttonSendCommand_Click);
            // 
            // buttonClose
            // 
            this.buttonClose.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonClose.Location = new System.Drawing.Point(737, 568);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(100, 40);
            this.buttonClose.TabIndex = 6;
            this.buttonClose.Text = "关闭";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // buttonStartSrv
            // 
            this.buttonStartSrv.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonStartSrv.Location = new System.Drawing.Point(592, 498);
            this.buttonStartSrv.Name = "buttonStartSrv";
            this.buttonStartSrv.Size = new System.Drawing.Size(100, 40);
            this.buttonStartSrv.TabIndex = 7;
            this.buttonStartSrv.Text = "开始服务";
            this.buttonStartSrv.UseVisualStyleBackColor = true;
            this.buttonStartSrv.Click += new System.EventHandler(this.buttonStartSrv_Click);
            // 
            // listBoxLogger
            // 
            listBoxLogger.Dock = System.Windows.Forms.DockStyle.Top;
            listBoxLogger.FormattingEnabled = true;
            listBoxLogger.HorizontalScrollbar = true;
            listBoxLogger.ItemHeight = 12;
            listBoxLogger.Location = new System.Drawing.Point(0, 0);
            listBoxLogger.Name = "listBoxLogger";
            listBoxLogger.ScrollAlwaysVisible = true;
            listBoxLogger.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            listBoxLogger.Size = new System.Drawing.Size(1160, 448);
            listBoxLogger.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 568);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 40);
            this.label1.TabIndex = 9;
            this.label1.Text = "选择客户端:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // clientListcombox
            // 
            clientListcombox.FormattingEnabled = true;
            clientListcombox.Location = new System.Drawing.Point(148, 582);
            clientListcombox.Name = "clientListcombox";
            clientListcombox.Size = new System.Drawing.Size(121, 20);
            clientListcombox.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(316, 571);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(124, 37);
            this.label2.TabIndex = 11;
            this.label2.Text = "请求服务:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // methodListcombox
            // 
            methodListcombox.FormattingEnabled = true;
            methodListcombox.Location = new System.Drawing.Point(446, 582);
            methodListcombox.Name = "methodListcombox";
            methodListcombox.Size = new System.Drawing.Size(121, 20);
            methodListcombox.TabIndex = 12;
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "notifyIcon";
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItemShow,
            this.ToolStripMenuItemClose});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(137, 48);
            // 
            // ToolStripMenuItemShow
            // 
            this.ToolStripMenuItemShow.Image = ((System.Drawing.Image)(resources.GetObject("ToolStripMenuItemShow.Image")));
            this.ToolStripMenuItemShow.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ToolStripMenuItemShow.Name = "ToolStripMenuItemShow";
            this.ToolStripMenuItemShow.Size = new System.Drawing.Size(136, 22);
            this.ToolStripMenuItemShow.Text = "打开主窗口";
            this.ToolStripMenuItemShow.Click += new System.EventHandler(this.ToolStripMenuItemShow_Click);
            // 
            // ToolStripMenuItemClose
            // 
            this.ToolStripMenuItemClose.Image = ((System.Drawing.Image)(resources.GetObject("ToolStripMenuItemClose.Image")));
            this.ToolStripMenuItemClose.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ToolStripMenuItemClose.Name = "ToolStripMenuItemClose";
            this.ToolStripMenuItemClose.Size = new System.Drawing.Size(136, 22);
            this.ToolStripMenuItemClose.Text = "关闭服务器";
            this.ToolStripMenuItemClose.Click += new System.EventHandler(this.ToolStripMenuItemClose_Click);
            // 
            // SuperSocketForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(1160, 652);
            this.Controls.Add(methodListcombox);
            this.Controls.Add(this.label2);
            this.Controls.Add(clientListcombox);
            this.Controls.Add(this.label1);
            this.Controls.Add(listBoxLogger);
            this.Controls.Add(this.buttonStartSrv);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonSendCommand);
            this.Controls.Add(this.numericUpDownPort);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.textBoxSrvIP);
            this.Controls.Add(this.labelSrvIP);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SuperSocketForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WDataServer";
            this.Load += new System.EventHandler(this.SuperSocketForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).EndInit();
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelSrvIP;
        private System.Windows.Forms.TextBox textBoxSrvIP;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.NumericUpDown numericUpDownPort;
        private System.Windows.Forms.Button buttonSendCommand;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Button buttonStartSrv;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemShow;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemClose;
        private static System.Windows.Forms.ComboBox clientListcombox;
        private static System.Windows.Forms.ComboBox methodListcombox;
        private static System.Windows.Forms.ListBox listBoxLogger;
    }
}