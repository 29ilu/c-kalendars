using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Csharp_programesanas_todo_kalendars
{
    // ===== ENUM un PAMATKLASE =====
    public enum TaskStatus { Pending, Completed, Later }

    public abstract class TaskBase
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; private set; } = TaskStatus.Pending;

        protected TaskBase(int id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }

        public void MarkCompleted() => Status = TaskStatus.Completed;
        public void MarkLater() => Status = TaskStatus.Later;

        public abstract string TypeName { get; }
    }

    // ===== KONKRĒTI UZDEVUMU TIPI =====
    public class SimpleTask : TaskBase
    {
        public SimpleTask(int id, string title, string description) : base(id, title, description) { }
        public override string TypeName => "Simple";
    }

    public class MeetingTask : TaskBase
    {
        public DateTime MeetingTime { get; set; }
        public MeetingTask(int id, string title, string description, DateTime meetingTime)
            : base(id, title, description)
        {
            MeetingTime = meetingTime;
        }
        public override string TypeName => "Meeting";
    }

    public class BugFixTask : TaskBase
    {
        public string BugId { get; set; }
        public BugFixTask(int id, string title, string description, string bugId)
            : base(id, title, description)
        {
            BugId = bugId;
        }
        public override string TypeName => "BugFix";
    }

    // ===== PROJEKTI UN LIETOTĀJI =====
    public class Project
    {
        public string Name { get; set; }
        public List<TaskBase> Tasks { get; } = new List<TaskBase>();
        public Project(string name) { Name = name; }
        public void AddTask(TaskBase t) => Tasks.Add(t);
    }

    public class User
    {
        public string Name { get; set; }
        public User(string name) { Name = name; }
    }

    // ===== REPOZITORIJS (DIP PRINCIPS) =====
    public interface ITaskRepository
    {
        void AddTask(TaskBase task);
        IEnumerable<TaskBase> GetAllTasks();
        TaskBase GetById(int id);
    }

    public class InMemoryTaskRepository : ITaskRepository
    {
        private readonly List<TaskBase> _tasks = new List<TaskBase>();
        public void AddTask(TaskBase task) => _tasks.Add(task);
        public IEnumerable<TaskBase> GetAllTasks() => _tasks;
        public TaskBase GetById(int id) => _tasks.FirstOrDefault(t => t.Id == id);
    }

    // ===== GALVENĀ FORMA =====
    public class MainForm : Form
    {
        private readonly ITaskRepository _repo;
        private readonly Project _project;

        private ListView lvTasks;
        private Button btnAdd, btnComplete, btnLater;
        private TextBox txtDetails;
        private int nextId = 1;

        public MainForm()
        {
            _repo = new InMemoryTaskRepository();
            _project = new Project("Default Project");

            Text = "Task Manager";
            Width = 800;
            Height = 600;

            InitializeComponents();
            SeedDemoData();
            RefreshList();
        }

        private void InitializeComponents()
        {
            lvTasks = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Width = 480,
                Height = 500,
                Location = new System.Drawing.Point(10, 50)
            };
            lvTasks.Columns.Add("ID", 40);
            lvTasks.Columns.Add("Status", 80);
            lvTasks.Columns.Add("Title", 200);
            lvTasks.Columns.Add("Type", 120);
            lvTasks.SelectedIndexChanged += LvTasks_SelectedIndexChanged;

            btnAdd = new Button { Text = "Add Task", Location = new System.Drawing.Point(10, 10), Width = 90 };
            btnAdd.Click += BtnAdd_Click;

            btnComplete = new Button { Text = "Mark Completed", Location = new System.Drawing.Point(110, 10), Width = 120 };
            btnComplete.Click += BtnComplete_Click;

            btnLater = new Button { Text = "Mark Later", Location = new System.Drawing.Point(240, 10), Width = 90 };
            btnLater.Click += BtnLater_Click;

            txtDetails = new TextBox { Multiline = true, ReadOnly = true, Width = 260, Height = 500, Location = new System.Drawing.Point(500, 50), ScrollBars = ScrollBars.Vertical };

            Controls.Add(lvTasks);
            Controls.Add(btnAdd);
            Controls.Add(btnComplete);
            Controls.Add(btnLater);
            Controls.Add(txtDetails);
        }

        private void SeedDemoData()
        {
            var t1 = new MeetingTask(nextId++, "Team Sync", "Weekly alignment meeting with dev team", DateTime.Now.AddDays(1));
            var t2 = new BugFixTask(nextId++, "Fix Login Bug", "Resolve NullReference exception in login flow", "BUG-1234");
            var t3 = new SimpleTask(nextId++, "Write Docs", "Update API documentation for auth module.");

            _project.AddTask(t1);
            _project.AddTask(t2);
            _project.AddTask(t3);

            _repo.AddTask(t1);
            _repo.AddTask(t2);
            _repo.AddTask(t3);

            t2.MarkCompleted();
            t3.MarkLater();
        }

        private void RefreshList()
        {
            lvTasks.Items.Clear();
            foreach (var t in _project.Tasks)
            {
                var item = new ListViewItem(t.Id.ToString());
                item.SubItems.Add(t.Status.ToString());
                item.SubItems.Add(t.Title);
                item.SubItems.Add(t.TypeName);
                item.Tag = t.Id;
                lvTasks.Items.Add(item);
            }
        }

        private void LvTasks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvTasks.SelectedItems.Count == 0)
            {
                txtDetails.Text = string.Empty;
                return;
            }

            var id = (int)lvTasks.SelectedItems[0].Tag;
            var task = _repo.GetById(id);
            if (task == null) return;

            var details = $"ID: {task.Id}\r\nTitle: {task.Title}\r\nStatus: {task.Status}\r\n\r\nDescription:\r\n{task.Description}\r\n";
            if (task is MeetingTask mt)
                details += $"\r\nMeeting Time: {mt.MeetingTime}";
            else if (task is BugFixTask bf)
                details += $"\r\nBug ID: {bf.BugId}";

            txtDetails.Text = details;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dlg = new AddTaskForm(nextId++))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var newTask = dlg.CreatedTask;
                    _project.AddTask(newTask);
                    _repo.AddTask(newTask);
                    RefreshList();
                }
                else
                {
                    nextId--;
                }
            }
        }

        private void BtnComplete_Click(object sender, EventArgs e)
        {
            if (lvTasks.SelectedItems.Count == 0) { MessageBox.Show("Select a task first."); return; }
            var id = (int)lvTasks.SelectedItems[0].Tag;
            var t = _repo.GetById(id);
            if (t == null) return;
            t.MarkCompleted();
            RefreshList();
            LvTasks_SelectedIndexChanged(null, null);
        }

        private void BtnLater_Click(object sender, EventArgs e)
        {
            if (lvTasks.SelectedItems.Count == 0) { MessageBox.Show("Select a task first."); return; }
            var id = (int)lvTasks.SelectedItems[0].Tag;
            var t = _repo.GetById(id);
            if (t == null) return;
            t.MarkLater();
            RefreshList();
            LvTasks_SelectedIndexChanged(null, null);
        }
    }

    // ===== UZDEVUMA PIEVIENOŠANAS LOGS =====
    public class AddTaskForm : Form
    {
        private Label lblTitle, lblDesc, lblType, lblExtra;
        private TextBox txtTitle, txtDesc, txtExtra;
        private ComboBox cbType;
        private DateTimePicker dtpMeeting;
        private Button btnOk, btnCancel;

        private int _id;
        public TaskBase CreatedTask { get; private set; }

        public AddTaskForm(int id)
        {
            _id = id;
            Text = "Add Task";
            Width = 400;
            Height = 350;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            lblTitle = new Label { Text = "Title:", Left = 10, Top = 10 };
            txtTitle = new TextBox { Left = 10, Top = 30, Width = 350 };

            lblDesc = new Label { Text = "Description:", Left = 10, Top = 60 };
            txtDesc = new TextBox { Left = 10, Top = 80, Width = 350, Height = 60, Multiline = true };

            lblType = new Label { Text = "Type:", Left = 10, Top = 150 };
            cbType = new ComboBox { Left = 10, Top = 170, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cbType.Items.AddRange(new string[] { "Simple", "Meeting", "BugFix" });
            cbType.SelectedIndex = 0;
            cbType.SelectedIndexChanged += CbType_SelectedIndexChanged;

            lblExtra = new Label { Text = "Extra:", Left = 10, Top = 200 };
            txtExtra = new TextBox { Left = 10, Top = 220, Width = 200 };
            dtpMeeting = new DateTimePicker { Left = 220, Top = 220, Width = 140, Visible = false };

            btnOk = new Button { Text = "OK", Left = 200, Top = 260, Width = 70 };
            btnOk.Click += BtnOk_Click;
            btnCancel = new Button { Text = "Cancel", Left = 280, Top = 260, Width = 70 };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblTitle);
            Controls.Add(txtTitle);
            Controls.Add(lblDesc);
            Controls.Add(txtDesc);
            Controls.Add(lblType);
            Controls.Add(cbType);
            Controls.Add(lblExtra);
            Controls.Add(txtExtra);
            Controls.Add(dtpMeeting);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void CbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var type = cbType.SelectedItem.ToString();
            if (type == "Meeting")
            {
                lblExtra.Text = "Meeting Time:";
                txtExtra.Visible = false;
                dtpMeeting.Visible = true;
            }
            else if (type == "BugFix")
            {
                lblExtra.Text = "Bug ID:";
                txtExtra.Visible = true;
                dtpMeeting.Visible = false;
            }
            else
            {
                lblExtra.Text = "Extra:";
                txtExtra.Visible = true;
                dtpMeeting.Visible = false;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text)) { MessageBox.Show("Title is required"); return; }

            var type = cbType.SelectedItem.ToString();
            if (type == "Simple")
                CreatedTask = new SimpleTask(_id, txtTitle.Text.Trim(), txtDesc.Text.Trim());
            else if (type == "Meeting")
                CreatedTask = new MeetingTask(_id, txtTitle.Text.Trim(), txtDesc.Text.Trim(), dtpMeeting.Value);
            else if (type == "BugFix")
                CreatedTask = new BugFixTask(_id, txtTitle.Text.Trim(), txtDesc.Text.Trim(), txtExtra.Text.Trim());

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

 
