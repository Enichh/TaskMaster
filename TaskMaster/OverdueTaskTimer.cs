using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskMaster
{
    public static class OverdueTaskTimer
    {
        private static Timer overdueTimer;

        public static void StartTimer()
        {
            overdueTimer = new Timer();
            overdueTimer.Interval = 600000; 
            overdueTimer.Tick += (s, e) => UpdateOverdueTasks(); 
            overdueTimer.Start();
        }

        private static void UpdateOverdueTasks()
        {
            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE TASK SET status='Overdue' WHERE DueDate < @currentDate AND status NOT IN ('Completed', 'Overdue')",
                        conn
                    );
                    cmd.Parameters.AddWithValue("@currentDate", DateTime.Now);

                    cmd.ExecuteNonQuery(); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating overdue tasks: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}