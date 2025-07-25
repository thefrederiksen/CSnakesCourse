using System;
using System.Drawing;
using System.Windows.Forms;

namespace TalkToMyCode
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonSelectCodeBaseDirectory = new Button();
            textBoxCodeBaseDirectory = new TextBox();
            labelCodeBaseDirectory = new Label();
            textBoxOutput = new TextBox();
            buttonCountLines = new Button();
            menuStrip1 = new MenuStrip();
            menuItemFile = new ToolStripMenuItem();
            menuItemOpenProject = new ToolStripMenuItem();
            labelFileCount = new Label();
            textBoxFileCount = new TextBox();
            labelLineCount = new Label();
            textBoxLineCount = new TextBox();
            buttonStartShadowScan = new Button();
            buttonStopScan = new Button();
            buttonCheckOldSummaries = new Button();
            buttonOlamaInstructions = new Button();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // buttonSelectCodeBaseDirectory
            // 
            buttonSelectCodeBaseDirectory.Location = new Point(12, 41);
            buttonSelectCodeBaseDirectory.Name = "buttonSelectCodeBaseDirectory";
            buttonSelectCodeBaseDirectory.Size = new Size(150, 23);
            buttonSelectCodeBaseDirectory.TabIndex = 0;
            buttonSelectCodeBaseDirectory.Text = "Select Code Base Directory";
            buttonSelectCodeBaseDirectory.UseVisualStyleBackColor = true;
            buttonSelectCodeBaseDirectory.Click += buttonSelectCodeBaseDirectory_Click;
            // 
            // textBoxCodeBaseDirectory
            // 
            textBoxCodeBaseDirectory.Location = new Point(168, 42);
            textBoxCodeBaseDirectory.Name = "textBoxCodeBaseDirectory";
            textBoxCodeBaseDirectory.ReadOnly = true;
            textBoxCodeBaseDirectory.Size = new Size(666, 23);
            textBoxCodeBaseDirectory.TabIndex = 1;
            // 
            // labelCodeBaseDirectory
            // 
            labelCodeBaseDirectory.AutoSize = true;
            labelCodeBaseDirectory.Location = new Point(12, 15);
            labelCodeBaseDirectory.Name = "labelCodeBaseDirectory";
            labelCodeBaseDirectory.Size = new Size(116, 15);
            labelCodeBaseDirectory.TabIndex = 2;
            labelCodeBaseDirectory.Text = "Code Base Directory:";
            // 
            // textBoxOutput
            // 
            textBoxOutput.Location = new Point(12, 323);
            textBoxOutput.Multiline = true;
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.Size = new Size(1076, 179);
            textBoxOutput.TabIndex = 3;
            // 
            // buttonCountLines
            // 
            buttonCountLines.Location = new Point(939, 42);
            buttonCountLines.Name = "buttonCountLines";
            buttonCountLines.Size = new Size(138, 23);
            buttonCountLines.TabIndex = 4;
            buttonCountLines.Text = "Count Lines";
            buttonCountLines.UseVisualStyleBackColor = true;
            buttonCountLines.Click += buttonCountLines_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuItemFile });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1100, 24);
            menuStrip1.TabIndex = 100;
            menuStrip1.Text = "menuStrip1";
            // 
            // menuItemFile
            // 
            menuItemFile.DropDownItems.AddRange(new ToolStripItem[] { menuItemOpenProject });
            menuItemFile.Name = "menuItemFile";
            menuItemFile.Size = new Size(37, 20);
            menuItemFile.Text = "File";
            // 
            // menuItemOpenProject
            // 
            menuItemOpenProject.Name = "menuItemOpenProject";
            menuItemOpenProject.Size = new Size(143, 22);
            menuItemOpenProject.Text = "Open Project";
            menuItemOpenProject.Click += menuItemOpenProject_Click;
            // 
            // labelFileCount
            // 
            labelFileCount.AutoSize = true;
            labelFileCount.Location = new Point(12, 75);
            labelFileCount.Name = "labelFileCount";
            labelFileCount.Size = new Size(64, 15);
            labelFileCount.TabIndex = 5;
            labelFileCount.Text = "File Count:";
            // 
            // textBoxFileCount
            // 
            textBoxFileCount.Location = new Point(80, 72);
            textBoxFileCount.Name = "textBoxFileCount";
            textBoxFileCount.ReadOnly = true;
            textBoxFileCount.Size = new Size(80, 23);
            textBoxFileCount.TabIndex = 6;
            // 
            // labelLineCount
            // 
            labelLineCount.AutoSize = true;
            labelLineCount.Location = new Point(180, 75);
            labelLineCount.Name = "labelLineCount";
            labelLineCount.Size = new Size(68, 15);
            labelLineCount.TabIndex = 7;
            labelLineCount.Text = "Line Count:";
            // 
            // textBoxLineCount
            // 
            textBoxLineCount.Location = new Point(255, 72);
            textBoxLineCount.Name = "textBoxLineCount";
            textBoxLineCount.ReadOnly = true;
            textBoxLineCount.Size = new Size(100, 23);
            textBoxLineCount.TabIndex = 8;
            // 
            // buttonStartShadowScan
            // 
            buttonStartShadowScan.Location = new Point(939, 72);
            buttonStartShadowScan.Name = "buttonStartShadowScan";
            buttonStartShadowScan.Size = new Size(138, 23);
            buttonStartShadowScan.TabIndex = 9;
            buttonStartShadowScan.Text = "Start Shadow Scan";
            buttonStartShadowScan.UseVisualStyleBackColor = true;
            buttonStartShadowScan.Click += buttonStartShadowScan_Click;
            // 
            // buttonStopScan
            // 
            buttonStopScan.Location = new Point(939, 102);
            buttonStopScan.Name = "buttonStopScan";
            buttonStopScan.Size = new Size(138, 23);
            buttonStopScan.TabIndex = 10;
            buttonStopScan.Text = "Stop Scan";
            buttonStopScan.UseVisualStyleBackColor = true;
            buttonStopScan.Click += buttonStopScan_Click;
            // 
            // buttonCheckOldSummaries
            // 
            buttonCheckOldSummaries.Location = new Point(939, 132);
            buttonCheckOldSummaries.Name = "buttonCheckOldSummaries";
            buttonCheckOldSummaries.Size = new Size(138, 23);
            buttonCheckOldSummaries.TabIndex = 11;
            buttonCheckOldSummaries.Text = "Update Summaries";
            buttonCheckOldSummaries.UseVisualStyleBackColor = true;
            buttonCheckOldSummaries.Click += buttonCheckOldSummaries_Click;
            // 
            // buttonOlamaInstructions
            // 
            buttonOlamaInstructions.Location = new Point(939, 162);
            buttonOlamaInstructions.Name = "buttonOlamaInstructions";
            buttonOlamaInstructions.Size = new Size(138, 23);
            buttonOlamaInstructions.TabIndex = 12;
            buttonOlamaInstructions.Text = "Olama Install";
            buttonOlamaInstructions.UseVisualStyleBackColor = true;
            buttonOlamaInstructions.Click += buttonOlamaInstructions_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 514);
            Controls.Add(menuStrip1);
            Controls.Add(labelFileCount);
            Controls.Add(textBoxFileCount);
            Controls.Add(labelLineCount);
            Controls.Add(textBoxLineCount);
            Controls.Add(buttonStartShadowScan);
            Controls.Add(buttonStopScan);
            Controls.Add(buttonCheckOldSummaries);
            Controls.Add(buttonOlamaInstructions);
            Controls.Add(buttonCountLines);
            Controls.Add(textBoxOutput);
            Controls.Add(labelCodeBaseDirectory);
            Controls.Add(textBoxCodeBaseDirectory);
            Controls.Add(buttonSelectCodeBaseDirectory);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "Talk To My Code";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button buttonSelectCodeBaseDirectory;
        private System.Windows.Forms.TextBox textBoxCodeBaseDirectory;
        private System.Windows.Forms.Label labelCodeBaseDirectory;
        private TextBox textBoxOutput;
        private Button buttonCountLines;
        private Label labelFileCount;
        private TextBox textBoxFileCount;
        private Label labelLineCount;
        private TextBox textBoxLineCount;
        private Button buttonStartShadowScan;
        private Button buttonStopScan;
        private Button buttonCheckOldSummaries;
        private Button buttonOlamaInstructions;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem menuItemFile;
        private ToolStripMenuItem menuItemOpenProject;
    }
}
