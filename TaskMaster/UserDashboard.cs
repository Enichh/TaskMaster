using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskMaster
{
    public partial class UserDashboard : Form
    {
        private string userEmail;

        public UserDashboard(string email) 
        {
            InitializeComponent();
            userEmail = email;
        }
        private void UserDashboard_Load(object sender, EventArgs e)
        {
            OverdueTaskTimer.StartTimer(); // ✅ Start overdue tracking

            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT vuserName FROM USERS WHERE vemail = @email", conn); // ✅ Match correct column names
                    cmd.Parameters.AddWithValue("@email", Sessions.CurrentUserEmail);

                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        Sessions.CurrentUserName = result.ToString(); // ✅ Store username globally
                        userNamePlaceHolder.Text = $"Welcome, {Sessions.CurrentUserName}!"; // ✅ Display username
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error retrieving username: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            LoadUserTasks(Sessions.CurrentUserName); // ✅ Pass username, not email
        }



        private void LoadUserTasks(string username)
        {
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(
                        "SELECT TASK.itaskID, TASK.vtaskName, TASK.vtaskDescription, TASK.Status, TASK.CompletedAt, TASK.DueDate, TASK.DateAssigned " +
                        "FROM TASK INNER JOIN USERS ON TASK.assignedTo = USERS.vuserName " +  
                        "WHERE USERS.vuserName = @username",
                        conn
                    );
                    da.SelectCommand.Parameters.AddWithValue("@username", username); 

                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dataGridViewTasks.DataSource = dt;
                    dataGridViewTasks.ReadOnly = true;
                    dataGridViewTasks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading tasks: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        private void btnLogOut_Click_1(object sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm();
            loginForm.Show();
            this.Close(); // Close the dashboard
        }

        private void btnAcceptTask_Click(object sender, EventArgs e)
        {
            if (dataGridViewTasks.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task to accept.", "No Task Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = dataGridViewTasks.SelectedRows[0];
            string taskID = selectedRow.Cells["itaskID"].Value.ToString();
            string taskName = selectedRow.Cells["vtaskName"].Value.ToString();
            string taskStatus = selectedRow.Cells["status"].Value.ToString();

            if (taskStatus != "Assigned")
            {
                MessageBox.Show($"Only tasks with status 'Assigned' can be accepted! \nCurrent status: {taskStatus}",
                                "Cannot Accept Task", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show($"Do you want to accept '{taskName}'?",
                                                  "Confirm Acceptance",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return;
            }

            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("UPDATE TASK SET status='In Progress' WHERE itaskID=@taskID", conn);
                    cmd.Parameters.AddWithValue("@taskID", taskID);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Task '{taskName}' is now In Progress!", "Task Accepted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadUserTasks(Sessions.CurrentUserName);
                    }
                    else
                    {
                        MessageBox.Show("Task update failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnMarkComplete_Click(object sender, EventArgs e)
        {
            // 🔹 Ensure a task is selected
            if (dataGridViewTasks.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a task to mark as complete.", "No Task Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🔹 Get selected task details
            DataGridViewRow selectedRow = dataGridViewTasks.SelectedRows[0];
            string taskName = selectedRow.Cells["vtaskName"].Value.ToString();
            string taskStatus = selectedRow.Cells["status"].Value.ToString();
            string taskID = selectedRow.Cells["itaskID"].Value.ToString(); // Assuming task ID exists

            // 🔹 Ensure the task is "In Progress" before marking complete
            if (taskStatus != "In Progress")
            {
                MessageBox.Show($"Only tasks that are 'In Progress' can be marked as completed! \nCurrent status: {taskStatus}",
                                "Cannot Mark Complete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🔹 Confirmation prompt
            DialogResult result = MessageBox.Show($"Admin will verify if you completed '{taskName}'. Do you want to proceed?",
                                                  "Confirm Completion",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return; // User canceled marking task complete
            }

            // 🔹 Update task status to "Pending Verification"
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("UPDATE TASK SET status='Pending Verification' WHERE itaskID=@taskID", conn);
                    cmd.Parameters.AddWithValue("@taskID", taskID);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Task '{taskName}' is now Pending Verification by Admin!", "Task Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadUserTasks(userEmail); // 🔹 Refresh task list
                    }
                    else
                    {
                        MessageBox.Show("Task update failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void dataGridViewTasks_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }



}
