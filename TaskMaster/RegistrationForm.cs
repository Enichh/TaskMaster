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
    public partial class RegistrationForm : Form
    {
        public RegistrationForm()
        {
            InitializeComponent();
            txtPassword.PasswordChar = '*'; 
            txtConfirmPassword.PasswordChar = '*';
        }
        
        private void RegistrationForm_Load(object sender, EventArgs e)
        {
            
            comboRole.Items.Add("Admin");
            comboRole.Items.Add("User ");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LoginForm logForm = new LoginForm();
            logForm.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
          
            if (txtUserName.Text == "" || txtPassword.Text == "" || txtConfirmPassword.Text == "" || txtEmail.Text == "" || comboRole.SelectedItem == null)
            {
                MessageBox.Show("Please fill all fields.", "Error");
                return;
            }

            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Passwords do not match.", "Error");
                return;
            }

            using (SqlConnection conn = DBManager.GetConnection())
            {
                try
                {
                    conn.Open();
                    string tableName = comboRole.SelectedItem.ToString() == "Admin" ? "ADMINS" : "USERS";

                    SqlCommand cmd = new SqlCommand($"INSERT INTO {tableName} (vuserName, vpassword, vemail) VALUES (@user, @pass, @email)", conn);
                    cmd.Parameters.AddWithValue("@user", txtUserName.Text);
                    cmd.Parameters.AddWithValue("@pass", txtPassword.Text); 
                    cmd.Parameters.AddWithValue("@email", txtEmail.Text);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Registration successful! You can now log in.", "Success");

                  
                    LoginForm loginForm = new LoginForm();
                    loginForm.Show();
                    this.Hide();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error");
                }

          }  }

        private void comboRole_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtConfirmPassword.Clear();
            txtPassword.Clear();
            txtEmail.Clear();
            txtUserName.Clear();
            comboRole.SelectedIndex = -1;
            txtUserName.Focus();

        }
    }
}

