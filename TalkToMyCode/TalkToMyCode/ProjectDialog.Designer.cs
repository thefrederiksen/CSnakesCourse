namespace TalkToMyCode
{
    partial class ProjectDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox listBoxProjects;
        private System.Windows.Forms.TextBox textBoxNewProject;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.Button buttonCreate;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label labelExisting;
        private System.Windows.Forms.Label labelNew;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.listBoxProjects = new System.Windows.Forms.ListBox();
            this.textBoxNewProject = new System.Windows.Forms.TextBox();
            this.buttonOpen = new System.Windows.Forms.Button();
            this.buttonCreate = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.labelExisting = new System.Windows.Forms.Label();
            this.labelNew = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelExisting
            // 
            this.labelExisting.AutoSize = true;
            this.labelExisting.Location = new System.Drawing.Point(12, 9);
            this.labelExisting.Name = "labelExisting";
            this.labelExisting.Size = new System.Drawing.Size(120, 15);
            this.labelExisting.TabIndex = 0;
            this.labelExisting.Text = "Open Existing Project:";
            // 
            // listBoxProjects
            // 
            this.listBoxProjects.FormattingEnabled = true;
            this.listBoxProjects.ItemHeight = 15;
            this.listBoxProjects.Location = new System.Drawing.Point(12, 27);
            this.listBoxProjects.Name = "listBoxProjects";
            this.listBoxProjects.Size = new System.Drawing.Size(260, 94);
            this.listBoxProjects.TabIndex = 1;
            // 
            // buttonOpen
            // 
            this.buttonOpen.Location = new System.Drawing.Point(278, 27);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(75, 23);
            this.buttonOpen.TabIndex = 2;
            this.buttonOpen.Text = "Open";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.buttonOpen_Click);
            // 
            // labelNew
            // 
            this.labelNew.AutoSize = true;
            this.labelNew.Location = new System.Drawing.Point(12, 134);
            this.labelNew.Name = "labelNew";
            this.labelNew.Size = new System.Drawing.Size(108, 15);
            this.labelNew.TabIndex = 3;
            this.labelNew.Text = "Create New Project:";
            // 
            // textBoxNewProject
            // 
            this.textBoxNewProject.Location = new System.Drawing.Point(12, 152);
            this.textBoxNewProject.Name = "textBoxNewProject";
            this.textBoxNewProject.Size = new System.Drawing.Size(260, 23);
            this.textBoxNewProject.TabIndex = 4;
            // 
            // buttonCreate
            // 
            this.buttonCreate.Location = new System.Drawing.Point(278, 152);
            this.buttonCreate.Name = "buttonCreate";
            this.buttonCreate.Size = new System.Drawing.Size(75, 23);
            this.buttonCreate.TabIndex = 5;
            this.buttonCreate.Text = "Create";
            this.buttonCreate.UseVisualStyleBackColor = true;
            this.buttonCreate.Click += new System.EventHandler(this.buttonCreate_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(278, 200);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            // 
            // ProjectDialog
            // 
            this.AcceptButton = this.buttonOpen;
            this.CancelButton = this.buttonCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 240);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonCreate);
            this.Controls.Add(this.textBoxNewProject);
            this.Controls.Add(this.labelNew);
            this.Controls.Add(this.buttonOpen);
            this.Controls.Add(this.listBoxProjects);
            this.Controls.Add(this.labelExisting);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProjectDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select or Create Project";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
} 