using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace TaskMaster
{
    public partial class VerificationForm : Form
    {
        private string userType;
        private string userEmail;
        private string verificationCode;
        

        public VerificationForm(string userType, string userEmail, string verificationCode)
        {
            InitializeComponent();
            this.userType = userType;
            this.userEmail = userEmail;
            this.verificationCode = verificationCode;
        
        }

 
        private void btnVerify_Click(object sender, EventArgs e)
        {
            if (txtVerificationCode.Text == this.verificationCode) 
            {
                MessageBox.Show("Verification successful! You are now logged in.", "Verified", MessageBoxButtons.OK, MessageBoxIcon.Information);


                Sessions.CurrentUserEmail = userEmail;

                using (SqlConnection conn = DBManager.GetConnection())
                {
                    try
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand("SELECT vusername FROM Users WHERE vemail = @email", conn);
                        cmd.Parameters.AddWithValue("@email", userEmail);

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            Sessions.CurrentUserName = result.ToString(); 
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error retrieving username: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

               
               
                this.Hide(); 

                if (userType == "User")
                {
                    UserDashboard userDashboard = new UserDashboard(Sessions.CurrentUserEmail); 
                    userDashboard.Show();
                }
                else if (userType == "Admin")
                {
                    AdminDashboard adminDashboard = new AdminDashboard();
                    adminDashboard.Show();
                }
            }
            else
            {
                MessageBox.Show("Invalid verification code. Please try again or resend a new code.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string GenerateVerificationCode()
        {
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString(); // Generates a 6-digit code
        }

        private void btnResendCode_Click(object sender, EventArgs e)
        {
            string newVerificationCode = GenerateVerificationCode(); 
            verificationCode = newVerificationCode; 

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");

                smtpServer.Port = 587;
                smtpServer.Credentials = new NetworkCredential("yourEmail@gmail.com", "yourAppPassword");
                smtpServer.EnableSsl = true;

                mail.From = new MailAddress("yourEmail@gmail.com");
                mail.To.Add(userEmail);
                mail.Subject = "Your New Verification Code";
                mail.Body = $"Your new verification code is: {newVerificationCode}";

                smtpServer.Send(mail);
                MessageBox.Show("A new verification code has been sent!", "Resend Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending email: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

    }
}
