using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace TaskMaster
{
    public partial class LoginForm : Form
    {
        private string currentVerificationCode;
        public LoginForm()
        {
           
           
            InitializeComponent();
        }
        private string GenerateVerificationCode()
        {
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString(); // Generates a 6-digit code
        }
        private void LoginForm_Load(object sender, EventArgs e)
        {

        }

        private void btnSignIn_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = DBManager.GetConnection())
            { 

                try
                {
                    conn.Open();

                 
                    SqlCommand cmdUser = new SqlCommand("SELECT vemail FROM USERS WHERE vuserName=@user AND vpassword=@pass", conn);
                    cmdUser.Parameters.AddWithValue("@user", txtUsername.Text);
                    cmdUser.Parameters.AddWithValue("@pass", txtPassword.Text);

                    object userEmail = cmdUser.ExecuteScalar();

                    if (userEmail != null) 
                    {
                        HandleLogin(userEmail.ToString(), "User");
                        return;
                    }

                    
                    SqlCommand cmdAdmin = new SqlCommand("SELECT vemail FROM ADMINS WHERE vuserName=@user AND vpassword=@pass", conn);
                    cmdAdmin.Parameters.AddWithValue("@user", txtUsername.Text);
                    cmdAdmin.Parameters.AddWithValue("@pass", txtPassword.Text);

                    object adminEmail = cmdAdmin.ExecuteScalar();

                    if (adminEmail != null) // Admin found
                    {
                        HandleLogin(adminEmail.ToString(), "Admin");
                        return;
                    }

                  
                    MessageBox.Show("Invalid username or password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void HandleLogin(string userEmail, string userType)
        {
            string verificationCode = GenerateVerificationCode();
            Console.WriteLine($"Verification code: {verificationCode}"); // For debugging purposes


            SendVerificationEmail(userEmail, verificationCode);

         
            currentVerificationCode = verificationCode;

            MessageBox.Show($"A verification code has been sent to your email. Please enter it to proceed.", "Verification", MessageBoxButtons.OK, MessageBoxIcon.Information);

            VerificationForm verificationForm = new VerificationForm(userType, userEmail, verificationCode);
            verificationForm.Show();

            this.Hide();
        }




        private void SendVerificationEmail(string recipientEmail, string verificationCode)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com"); // Gmail SMTP
                smtpServer.Port = 587;
                smtpServer.Credentials = new NetworkCredential("enochastor0@gmail.com", "osfh hyzb idep mvfd"); // Use App Passwords
                smtpServer.EnableSsl = true;

                mail.From = new MailAddress("enochastor0@gmail.com", "TaskMaster Support");
                mail.To.Add(recipientEmail);
                mail.Subject = "Your Login Verification Code";
                mail.IsBodyHtml = true; 
                mail.Body = $"<h2 style='color:blue;'>Your Login Verification Code:</h2><h1 style='font-size:30px;'>{verificationCode}</h1>";


                smtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending email: " + ex.Message, "Error");
            }
        }
        private void lblSignUp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RegistrationForm regForm = new RegistrationForm(); 
            regForm.Show(); 
            this.Hide(); 
        }
    }
}
