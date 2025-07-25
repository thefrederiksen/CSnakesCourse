using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TalkToMyCode
{
    public partial class ProjectDialog : Form
    {
        public string SelectedProjectPath { get; private set; }
        private string projectsRoot;

        public ProjectDialog()
        {
            InitializeComponent();
            projectsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TalkToMyCodeProjects");
            LoadProjects();
        }

        private void LoadProjects()
        {
            listBoxProjects.Items.Clear();
            if (Directory.Exists(projectsRoot))
            {
                var dirs = Directory.GetDirectories(projectsRoot);
                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    listBoxProjects.Items.Add(name);
                }
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            if (listBoxProjects.SelectedItem != null)
            {
                SelectedProjectPath = Path.Combine(projectsRoot, listBoxProjects.SelectedItem.ToString());
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a project to open.");
            }
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {
            string newProjectName = textBoxNewProject.Text.Trim();
            if (string.IsNullOrEmpty(newProjectName))
            {
                MessageBox.Show("Please enter a project name.");
                return;
            }
            string newProjectPath = Path.Combine(projectsRoot, newProjectName);
            if (Directory.Exists(newProjectPath))
            {
                MessageBox.Show("A project with this name already exists.");
                return;
            }
            Directory.CreateDirectory(newProjectPath);
            File.WriteAllText(Path.Combine(newProjectPath, "project.json"), "{\n  \"name\": \"" + newProjectName + "\"\n}");
            SelectedProjectPath = newProjectPath;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
} 