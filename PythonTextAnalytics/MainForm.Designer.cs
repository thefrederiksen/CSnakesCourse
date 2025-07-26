namespace PythonTextAnalytics
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Button btnSelectFile;
        private Label lblSelectedFile;
        private Label lblStatus;
        private PictureBox pictureBoxWordCloud;
        private TextBox txtFrequencies;
        private Label lblInstructions;
        private GroupBox groupBoxFile;
        private GroupBox groupBoxResults;
        private SplitContainer splitContainer;
        private TextBox txtLog;
        private GroupBox groupBoxLog;
        private Button btnCopyLog;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing)
            {
                pictureBoxWordCloud.Image?.Dispose();
                _host?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSelectFile = new Button();
            this.lblSelectedFile = new Label();
            this.lblStatus = new Label();
            this.pictureBoxWordCloud = new PictureBox();
            this.txtFrequencies = new TextBox();
            this.lblInstructions = new Label();
            this.groupBoxFile = new GroupBox();
            this.groupBoxResults = new GroupBox();
            this.splitContainer = new SplitContainer();
            this.txtLog = new TextBox();
            this.groupBoxLog = new GroupBox();
            this.btnCopyLog = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWordCloud)).BeginInit();
            this.groupBoxFile.SuspendLayout();
            this.groupBoxResults.SuspendLayout();
            this.groupBoxLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Left)));
            this.btnSelectFile.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.btnSelectFile.Location = new Point(12, 45);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new Size(120, 35);
            this.btnSelectFile.TabIndex = 0;
            this.btnSelectFile.Text = "Select File...";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new EventHandler(this.btnSelectFile_Click);
            // 
            // lblSelectedFile
            // 
            this.lblSelectedFile.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.lblSelectedFile.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblSelectedFile.Location = new Point(150, 45);
            this.lblSelectedFile.Name = "lblSelectedFile";
            this.lblSelectedFile.Size = new Size(530, 35);
            this.lblSelectedFile.TabIndex = 2;
            this.lblSelectedFile.Text = "No file selected - word cloud will generate automatically when you select a file";
            this.lblSelectedFile.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblInstructions
            // 
            this.lblInstructions.AutoSize = true;
            this.lblInstructions.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblInstructions.Location = new Point(12, 25);
            this.lblInstructions.Name = "lblInstructions";
            this.lblInstructions.Size = new Size(400, 15);
            this.lblInstructions.TabIndex = 3;
            this.lblInstructions.Text = "Supported formats: Text (.txt), PDF (.pdf), Word (.docx), Markdown (.md)";
            // 
            // groupBoxFile
            // 
            this.groupBoxFile.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.groupBoxFile.Controls.Add(this.lblInstructions);
            this.groupBoxFile.Controls.Add(this.btnSelectFile);
            this.groupBoxFile.Controls.Add(this.lblSelectedFile);
            this.groupBoxFile.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.groupBoxFile.Location = new Point(12, 12);
            this.groupBoxFile.Name = "groupBoxFile";
            this.groupBoxFile.Size = new Size(708, 95);
            this.groupBoxFile.TabIndex = 4;
            this.groupBoxFile.TabStop = false;
            this.groupBoxFile.Text = "File Selection";
            // 
            // pictureBoxWordCloud
            // 
            this.pictureBoxWordCloud.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.pictureBoxWordCloud.BackColor = Color.White;
            this.pictureBoxWordCloud.BorderStyle = BorderStyle.FixedSingle;
            this.pictureBoxWordCloud.Location = new Point(10, 25);
            this.pictureBoxWordCloud.Name = "pictureBoxWordCloud";
            this.pictureBoxWordCloud.Size = new Size(425, 320);
            this.pictureBoxWordCloud.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBoxWordCloud.TabIndex = 5;
            this.pictureBoxWordCloud.TabStop = false;
            // 
            // txtFrequencies
            // 
            this.txtFrequencies.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.txtFrequencies.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtFrequencies.Location = new Point(10, 25);
            this.txtFrequencies.Multiline = true;
            this.txtFrequencies.Name = "txtFrequencies";
            this.txtFrequencies.ReadOnly = true;
            this.txtFrequencies.ScrollBars = ScrollBars.Vertical;
            this.txtFrequencies.Size = new Size(230, 320);
            this.txtFrequencies.TabIndex = 6;
            this.txtFrequencies.Text = "Word frequencies will appear here after processing...";
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.splitContainer.Location = new Point(10, 25);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.pictureBoxWordCloud);
            this.splitContainer.Panel1MinSize = 300;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.txtFrequencies);
            this.splitContainer.Panel2MinSize = 200;
            this.splitContainer.Size = new Size(688, 355);
            this.splitContainer.SplitterDistance = 445;
            this.splitContainer.TabIndex = 7;
            // 
            // groupBoxResults
            // 
            this.groupBoxResults.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.groupBoxResults.Controls.Add(this.splitContainer);
            this.groupBoxResults.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.groupBoxResults.Location = new Point(12, 115);
            this.groupBoxResults.Name = "groupBoxResults";
            this.groupBoxResults.Size = new Size(708, 390);
            this.groupBoxResults.TabIndex = 8;
            this.groupBoxResults.TabStop = false;
            this.groupBoxResults.Text = "Analysis Results";
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
            this.lblStatus.BackColor = SystemColors.Control;
            this.lblStatus.BorderStyle = BorderStyle.Fixed3D;
            this.lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblStatus.Location = new Point(12, 620);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(708, 25);
            this.lblStatus.TabIndex = 9;
            this.lblStatus.Text = "Status: Initializing...";
            this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.txtLog.Font = new Font("Consolas", 8F, FontStyle.Regular, GraphicsUnit.Point);
            this.txtLog.Location = new Point(10, 20);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.Size = new Size(600, 75);
            this.txtLog.TabIndex = 10;
            this.txtLog.Text = "Application log will appear here...";
            // 
            // btnCopyLog
            // 
            this.btnCopyLog.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.btnCopyLog.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            this.btnCopyLog.Location = new Point(620, 20);
            this.btnCopyLog.Name = "btnCopyLog";
            this.btnCopyLog.Size = new Size(75, 23);
            this.btnCopyLog.TabIndex = 12;
            this.btnCopyLog.Text = "Copy Log";
            this.btnCopyLog.UseVisualStyleBackColor = true;
            this.btnCopyLog.Click += new EventHandler(this.btnCopyLog_Click);
            // 
            // groupBoxLog
            // 
            this.groupBoxLog.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left) | AnchorStyles.Right)));
            this.groupBoxLog.Controls.Add(this.btnCopyLog);
            this.groupBoxLog.Controls.Add(this.txtLog);
            this.groupBoxLog.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.groupBoxLog.Location = new Point(12, 515);
            this.groupBoxLog.Name = "groupBoxLog";
            this.groupBoxLog.Size = new Size(708, 100);
            this.groupBoxLog.TabIndex = 11;
            this.groupBoxLog.TabStop = false;
            this.groupBoxLog.Text = "Debug Log";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(732, 658);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.groupBoxLog);
            this.Controls.Add(this.groupBoxResults);
            this.Controls.Add(this.groupBoxFile);
            this.MinimumSize = new Size(600, 400);
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Python Text Analytics - Word Cloud Generator";
            this.WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWordCloud)).EndInit();
            this.groupBoxFile.ResumeLayout(false);
            this.groupBoxFile.PerformLayout();
            this.groupBoxResults.ResumeLayout(false);
            this.groupBoxLog.ResumeLayout(false);
            this.groupBoxLog.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}