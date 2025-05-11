using System.Data.SqlClient;

namespace TaskMaster
{
    public static class DBManager
    {
       
        private static readonly string connectionString = "Server=ENICH\\ENICHSERVER;Database=taskManagementDB;Trusted_Connection=True;";

      
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
