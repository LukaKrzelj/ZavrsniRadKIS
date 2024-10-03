using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using ZavrsniRadKIS.Resources;
using System.Data;

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for PartnersWindow.xaml
    /// </summary>
    public partial class PartnersWindow : Window
    {
        public PartnersWindow()
        {
            InitializeComponent();
            LoadPartners();
            partnerSearchTextBox.Focus();
        }

        private string connectionString = "Server=127.0.0.1;Database=kistest2;User Id=root;Password=;";

        int partnerID = MyVariables.partnerID;

        private void LoadPartners()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM partner";


                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    partnersDataGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void partnersDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && partnersDataGrid.SelectedItem != null)
            {
                DataRowView row = (DataRowView)partnersDataGrid.SelectedItem;

                partnerID = Convert.ToInt32(row["id"]);

                e.Handled = true;
            }
            else if (e.Key == Key.Down && partnersDataGrid.SelectedItem != null)
            {
                int currentIndex = partnersDataGrid.SelectedIndex;
                if (currentIndex < partnersDataGrid.Items.Count - 1)
                {
                    partnersDataGrid.SelectedIndex = currentIndex + 1;
                    partnersDataGrid.ScrollIntoView(partnersDataGrid.SelectedItem);
                    DataRowView row = (DataRowView)partnersDataGrid.SelectedItem;

                    partnerID = Convert.ToInt32(row["id"]);

                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up && partnersDataGrid.SelectedItem != null)
            {
                int currentIndex = partnersDataGrid.SelectedIndex;
                if (currentIndex > 0)
                {
                    partnersDataGrid.SelectedIndex = currentIndex - 1;
                    partnersDataGrid.ScrollIntoView(partnersDataGrid.SelectedItem);
                    DataRowView row = (DataRowView)partnersDataGrid.SelectedItem;

                    partnerID = Convert.ToInt32(row["id"]);

                    e.Handled = true;
                }
            }
        }

        private void PartnersDataGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (partnersDataGrid.SelectedItem != null)
            {
                partnersDataGrid.Focus();
                DataRowView row = (DataRowView)partnersDataGrid.SelectedItem;

                partnerID = Convert.ToInt32(row["id"]);

                e.Handled = true;
            }
        }

        private void partnerSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = partnerSearchTextBox.Text;

            if (!string.IsNullOrEmpty(searchText))
            {
                SearchPartnersByName(searchText);
            }
            else
            {
                LoadPartners();
            }
        }


        private void SearchPartnersByName(string searchText)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM partner WHERE name LIKE @searchText";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@searchText", $"%{searchText}%");
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    partnersDataGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void partnerSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Down) && partnersDataGrid.Items.Count > 0)
            {
                partnersDataGrid.SelectedIndex = 0;
                partnersDataGrid.Focus();
                DataRowView row = (DataRowView)partnersDataGrid.SelectedItem;

                partnerID = Convert.ToInt32(row["id"]);

                e.Handled = true;
            }
        }

        private void newPartnerButton_Click(object sender, RoutedEventArgs e)
        {
            NewPartnerWindow newPartnerWindow = new NewPartnerWindow();
            newPartnerWindow.Show();
        }

        private void editPartnerButton_Click(object sender, RoutedEventArgs e)
        {
            NewPartnerWindow newPartnerWindow = new NewPartnerWindow(partnerID);
            newPartnerWindow.Show();
        }

        private void deletePartnerButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Želite li izbrisati partnera?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {

                        conn.Open();
                        string query = @"UPDATE document SET partnerId = NULL WHERE partnerId = @partnerId; 
                                        DELETE FROM partner WHERE id = @partnerId ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@partnerId", partnerID);

                        cmd.ExecuteNonQuery();
                        LoadPartners();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }
            }
        }
    }
}
