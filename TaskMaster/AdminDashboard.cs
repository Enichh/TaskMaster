using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace TaskMaster
{
    public partial class AdminDashboard : Form
    {
        private string connectionString = "Server=ENICH\\ENICHSERVER;Database=taskManagementDB;Trusted_Connection=True;";
        public AdminDashboard()
        {
            InitializeComponent();
        }
        private void LoadTasks()
        {
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();

                    SqlDataAdapter da = new SqlDataAdapter("SELECT vtaskName, assignedTo, Status, DueDate, DateAssigned FROM TASK", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    taskListAdmin.DataSource = dt; 
                    taskListAdmin.ReadOnly = true;

                  
                   
                    taskListAdmin.Columns["vtaskName"].HeaderText = "Task Name";
                    taskListAdmin.Columns["assignedTo"].HeaderText = "Assigned To";
                    taskListAdmin.Columns["Status"].HeaderText = "Task Status";
                    taskListAdmin.Columns["DueDate"].HeaderText = "Due Date";
                    taskListAdmin.Columns["DateAssigned"].HeaderText = "Date Assigned";

                    // Format Date Columns for better readability
                    taskListAdmin.Columns["DueDate"].DefaultCellStyle.Format = "MM/dd/yyyy";
                    taskListAdmin.Columns["DateAssigned"].DefaultCellStyle.Format = "MM/dd/yyyy HH:mm";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error");
                }
            }
        }

        private void AdminDashboard_Load(object sender, EventArgs e)
        {
            LoadTasks();
            OverdueTaskTimer.StartTimer();

        }
     
        private void taskListAdmin_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure it's a valid row
            {
                DataGridViewRow row = taskListAdmin.Rows[e.RowIndex]; // Get the clicked row

                string assignedUser = row.Cells["assignedTo"].Value.ToString(); // Assigned user name
                string taskName = row.Cells["vtaskName"].Value.ToString(); // Task name
                string taskStatus = row.Cells["Status"].Value.ToString(); // Task status

                MessageBox.Show($"Assigned User: {assignedUser}\nTask Name: {taskName}\nStatus: {taskStatus}", "Task Details");
            }
        }

        private void btnVerify_Click(object sender, EventArgs e)
        {
            if (taskListAdmin.SelectedRows.Count > 0) 
            {
                DataGridViewRow selectedRow = taskListAdmin.SelectedRows[0]; 

                string assignedUser = selectedRow.Cells["assignedTo"].Value?.ToString() ?? "Unknown User";
                string taskName = selectedRow.Cells["vtaskName"].Value?.ToString() ?? "Unnamed Task";
                string status = selectedRow.Cells["Status"].Value?.ToString() ?? "Status Not Set";

                
                if (status == "Pending Verification")
                {
                    DialogResult result = MessageBox.Show(
                        $"Assigned User: {assignedUser}\nTask Name: {taskName}\nStatus: {status}\n\nWould you like to Verify or Reject?",
                        "Task Verification",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    using (SqlConnection conn = DBManager.GetConnection())
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand("UPDATE TASK SET Status=@newStatus, CompletedAt=@completedAt WHERE vtaskName=@taskName", conn);
                        cmd.Parameters.AddWithValue("@taskName", taskName);
                        cmd.Parameters.AddWithValue("@newStatus", result == DialogResult.Yes ? "Completed" : "Rejected");

                       
                        if (result == DialogResult.Yes)
                        {
                            cmd.Parameters.AddWithValue("@completedAt", DateTime.Now);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@completedAt", DBNull.Value); 
                        }

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show(result == DialogResult.Yes ? "Task Verified & Marked as Completed!" : "Task Rejected.", "Verification");
                    LoadTasks(); 
                }
                else
                {
                    MessageBox.Show("Only tasks marked as 'Pending Verification' can be verified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select a task first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnGenerateReport_Click(object sender, EventArgs e)
        {
        
            string query = "SELECT itaskID, vtaskName, assignedTo, Status, CompletedAt FROM TASK WHERE Status = 'Completed'";

            GenerateTaskReport(query); 
        }

        private void GenerateTaskReport(string query)
        {
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    Document document = new Document();
                    string filePath = @"C:\Users\Enoch Gabriel Astor\Desktop\TaskReports\CompletedTaskReport.pdf"; 
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

                    document.Open();
                    document.Add(new Paragraph("Completed Task Report\n"));
                    document.Add(new Paragraph($"Generated on: {DateTime.Now}\n\n"));

                    PdfPTable table = new PdfPTable(5);
                    table.AddCell("Task ID");
                    table.AddCell("Task Name");
                    table.AddCell("Assigned To");
                    table.AddCell("Status");
                    table.AddCell("Completed At");

                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(row["itaskID"].ToString());
                        table.AddCell(row["vtaskName"].ToString());
                        table.AddCell(row["assignedTo"].ToString());
                        table.AddCell(row["Status"].ToString());
                        table.AddCell(row["CompletedAt"] != DBNull.Value ? row["CompletedAt"].ToString() : "Not Completed");
                    }

                    document.Add(table);
                    document.Close();

                    MessageBox.Show("Completed Task Report generated successfully! File saved at: " + filePath, "Success");

                    System.Diagnostics.Process.Start(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnManageTasks_Click_1(object sender, EventArgs e)
        {
            TaskManagerForm taskManager = new TaskManagerForm();
            taskManager.Show();
            this.Hide();
           
        }

        private void btnLogOut_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Logout Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Perform logout actions
                MessageBox.Show("You have been logged out successfully.", "Logout", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close()
               ; // Close the AdminDashboard form   
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
            }
        }

    }
}
