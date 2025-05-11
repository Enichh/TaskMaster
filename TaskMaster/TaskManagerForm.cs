using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskMaster
{
    public partial class TaskManagerForm : Form
    {
        public TaskManagerForm()
        {
            InitializeComponent();
        }

        private void TaskManagerForm_Load(object sender, EventArgs e)
        {
            LoadResources();
            LoadTasks();
        }
        private void LoadResources()
        {
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter("SELECT vuserName FROM USERS", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cmbResources.DataSource = dt;
                    cmbResources.DisplayMember = "vuserName"; 
  

                    Console.WriteLine("Loaded Users into cmbResources:");
                    foreach (DataRow row in dt.Rows)
                    {
                        Console.WriteLine($" - {row["vuserName"]}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading users: {ex.Message}");
                }
            }
        }
        private void LoadTasks()
        {
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(
                        "SELECT TASK.vtaskName, TASK.vtaskDescription, TASK.assignedTo, " +
                        "TASK.Status, TASK.DueDate, TASK.DateAssigned " +
                        "FROM TASK", conn);

                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dataGridTask.DataSource = dt;
                    dataGridTask.ReadOnly = true;
                    dataGridTask.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; 

                    dataGridTask.Columns["vtaskName"].HeaderText = "Task Name";
                    dataGridTask.Columns["assignedTo"].HeaderText = "Assigned To";
                    dataGridTask.Columns["vtaskDescription"].HeaderText = "Task Description";
                    dataGridTask.Columns["Status"].HeaderText = "Task Status";
                    dataGridTask.Columns["DueDate"].HeaderText = "Due Date";
                    dataGridTask.Columns["DateAssigned"].HeaderText = "Date Assigned";

                    dataGridTask.Columns["DueDate"].DefaultCellStyle.Format = "MM/dd/yyyy";
                    dataGridTask.Columns["DateAssigned"].DefaultCellStyle.Format = "MM/dd/yyyy HH:mm";


                    foreach (DataRow row in dt.Rows)
                    {
                        Console.WriteLine($"Task Loaded: Name = {row["vtaskName"]}, AssignedTo = {row["assignedTo"]}, Due Date = {row["DueDate"]}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error");
                }
            }
        }

        private void btnAddTask_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTaskName.Text) || string.IsNullOrWhiteSpace(txtTaskDesc.Text) || cmbResources.SelectedIndex == -1)
            {
                MessageBox.Show("Please fill in all fields before adding a task!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Console.WriteLine($"Adding Task: Name = {txtTaskName.Text}, Assigned To = {cmbResources.SelectedItem}, User ID = {cmbResources.SelectedValue}, Due Date = {dtpDueDate.Value}");
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO TASK (vtaskName, vtaskDescription, assignedTo, Status, DueDate, DateAssigned) VALUES (@taskName, @taskDesc, @assignedTo, 'Assigned', @dueDate, @dateAssigned)", conn);
                    cmd.Parameters.AddWithValue("@taskName", txtTaskName.Text);
                    cmd.Parameters.AddWithValue("@taskDesc", txtTaskDesc.Text);
                    cmd.Parameters.AddWithValue("@assignedTo", cmbResources.Text); 
                    cmd.Parameters.AddWithValue("@dueDate", dtpDueDate.Value);
                    cmd.Parameters.AddWithValue("@dateAssigned", DateTime.Now);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Task Assigned Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTasks();
                    SendUserEmail(cmbResources.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Assigning Task: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    txtTaskName.Clear();
                    txtTaskDesc.Clear();
                    cmbResources.SelectedIndex = -1;
                    dtpDueDate.Value = DateTime.Now; // Reset to current date
                }
            }
          

        }

        private void btnEditTask_Click(object sender, EventArgs e)
        {
            if (dataGridTask.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task to edit.", "No Task Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Console.WriteLine("Edit Task Clicked: No task selected.");
                return;
            }

            DataGridViewRow selectedRow = dataGridTask.SelectedRows[0];

            string status = selectedRow.Cells["Status"].Value?.ToString() ?? "Status Not Set";
            string taskName = selectedRow.Cells["vtaskName"].Value?.ToString() ?? "Unnamed Task";
            string taskDesc = selectedRow.Cells["vtaskDescription"].Value?.ToString() ?? "No Description";
            string assignedUserName = selectedRow.Cells["assignedTo"].Value?.ToString() ?? "No User Assigned";
            string dueDate = selectedRow.Cells["DueDate"].Value?.ToString() ?? "No Due Date";

           
            Console.WriteLine("----- Debugging: Editing Task -----");
            Console.WriteLine($"Task Name: {taskName}");
            Console.WriteLine($"Task Description: {taskDesc}");
            Console.WriteLine($"Assigned To: {assignedUserName}") ;
            Console.WriteLine($"Task Status: {status}");
            Console.WriteLine($"Due Date: {dueDate}");

       
            if (status == "Pending Approval" || status == "In Progress" || status == "Completed")
            {
                Console.WriteLine($"Edit Restricted: Task is currently '{status}'");
                MessageBox.Show($"You cannot edit tasks that are '{status}'.", "Edit Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtTaskName.Text = taskName;
            txtTaskDesc.Text = taskDesc;

            if (DateTime.TryParse(selectedRow.Cells["DueDate"].Value?.ToString(), out DateTime dueDateValue))
            {
                dtpDueDate.Value = dueDateValue;
            }
            else
            {
                Console.WriteLine("Invalid date format detected.");
                MessageBox.Show("Invalid date format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

           
            Console.WriteLine("Current cmbResources Items:");
            foreach (DataRowView item in cmbResources.Items)
            {
                Console.WriteLine($" - {item["vuserName"]}");
            }


            if (!string.IsNullOrEmpty(assignedUserName))
            {
                cmbResources.SelectedItem = assignedUserName;
                Console.WriteLine($"ComboBox Selection Success: {cmbResources.SelectedItem}");
            }
            else
            {
                Console.WriteLine("Warning: Assigned User is NULL or empty—setting default selection.");
                cmbResources.SelectedIndex = -1;
            }
           
           
          
        }


        private void btnDelTask_Click(object sender, EventArgs e)
        {
            if (dataGridTask.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task to delete.", "No Task Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Console.WriteLine("Delete Task Clicked: No task selected.");
                return;
            }

            DataGridViewRow selectedRow = dataGridTask.SelectedRows[0];

     
            string taskName = selectedRow.Cells["vtaskName"].Value?.ToString();
            if (string.IsNullOrEmpty(taskName))
            {
                MessageBox.Show("Invalid Task Name. Cannot proceed with deletion.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Error: Selected task name is NULL or empty.");
                return;
            }

            DialogResult result = MessageBox.Show($"Are you sure you want to delete the task '{taskName}'?",
                                                  "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                Console.WriteLine("Task deletion canceled by user.");
                return;
            }

            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("DELETE FROM TASK WHERE vtaskName = @taskName", conn);
                    cmd.Parameters.AddWithValue("@taskName", taskName);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Task '{taskName}' deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Console.WriteLine($"Task Deleted: Name = {taskName}");
                        LoadTasks();
                    }
                    else
                    {
                        MessageBox.Show("Task not found or already deleted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Console.WriteLine($"Warning: Task '{taskName}' not found in the database.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"Database Error: {ex.Message}");
                }
            }
        }


        private void btnSaveEdit_Click(object sender, EventArgs e)
        {
            if (dataGridTask.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task before saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridTask.SelectedRows[0];

            string assignedUser = cmbResources.Text;
           
            Console.WriteLine($"Task Name: {txtTaskName.Text}");
            Console.WriteLine($"Task Description: {txtTaskDesc.Text}");
            Console.WriteLine($"Due Date: {dtpDueDate.Value}");
            Console.WriteLine($"Assigned To: {assignedUser}");
            SendUserEmail(assignedUser);
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(
        "UPDATE TASK SET vtaskName=@taskName, vtaskDescription=@desc, DueDate=@dueDate, assignedTo=@resource " +
        "WHERE vtaskName=@taskName AND DateAssigned=@dateAssigned", conn);

                    cmd.Parameters.AddWithValue("@taskName", txtTaskName.Text);
                    cmd.Parameters.AddWithValue("@desc", txtTaskDesc.Text);
                    cmd.Parameters.AddWithValue("@dueDate", dtpDueDate.Value);
                    cmd.Parameters.AddWithValue("@resource", assignedUser);
                    cmd.Parameters.AddWithValue("@dateAssigned", selectedRow.Cells["DateAssigned"].Value);


                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Task updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Task update failed. No changes were made.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            LoadTasks();
            txtTaskName.Clear();
            txtTaskDesc.Clear();
            cmbResources.SelectedIndex = -1;

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AdminDashboard admin = new AdminDashboard();
            admin.Show();
            this.Hide();


        }


        public void SendUserEmail(string assignedTO)
        {
            if (string.IsNullOrEmpty(assignedTO))
            {
                Console.WriteLine("❌ Error: Assigned username is empty!");
                return; // ✅ Prevents unnecessary queries
            }

            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();

                    // 🔹 Correct SQL query: Ensure column names match your table structure
                    SqlCommand cmd = new SqlCommand("SELECT vemail FROM USERS WHERE vuserName = @vuserName", conn);
                    cmd.Parameters.AddWithValue("@vuserName", assignedTO); // ✅ Correct parameter name

                    object result = cmd.ExecuteScalar();
                    if (result != null && !string.IsNullOrWhiteSpace(result.ToString()))
                    {
                        string email = result.ToString();
                        Console.WriteLine($"✅ Retrieved Email for {assignedTO}: {email}");

                        // 🔹 Send the email notification
                        SendEmail(email);
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ No email found for user '{assignedTO}', skipping notification.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error retrieving email for {assignedTO}: {ex.Message}");
                }
            }
        }

        public void SendEmail(string recipientEmail)
        {
            if (string.IsNullOrEmpty(recipientEmail))
            {
                Console.WriteLine("❌ Error: Cannot send email, recipient is missing!");
                return;
            }

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("enochastor0@gmail.com", "osfh hyzb idep mvfd"), // 🔹 Use App Passwords!
                    EnableSsl = true
                };

                mail.From = new MailAddress("enochastor0@gmail.com", "TaskMaster Support");
                mail.To.Add(recipientEmail);
                mail.Subject = "New Task Available!";
                mail.IsBodyHtml = true;
                mail.Body = "<p><b>For more details, please open your User Dashboard.</b></p>";

                smtpServer.Send(mail);
                Console.WriteLine($"✅ Email sent successfully to {recipientEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending email: {ex.Message}");
            }
        }
    }
    }







