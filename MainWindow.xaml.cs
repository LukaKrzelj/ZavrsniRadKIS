using System;
using System.Windows;
using MySql.Data.MySqlClient;
using ZavrsniRadKIS.Resources;

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=127.0.0.1;Database=kistest2;User Id=root;Password=;";

        //int userID = MyVariables.userID;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = userNameTextBox.Text;
            string password = passwordBox.Password;

            if (AutheticateUser(username, password))
            {
                OpenDashboard();
            }
            else
            {
                MessageBox.Show("Netočno korisničko ime ili lozinka!");
            }
        }

        private bool AutheticateUser(string username, string password)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT id FROM User WHERE Name = @username AND Password = @password";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    MyVariables.userID = Convert.ToInt32(cmd.ExecuteScalar());
                    return MyVariables.userID > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                    return false;
                }
            }

            throw new NotImplementedException();
        }

        private void OpenDashboard()
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Close();
        }
    }
}
