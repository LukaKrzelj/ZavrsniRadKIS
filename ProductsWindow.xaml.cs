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
using System.Data;
using ZavrsniRadKIS.Resources;

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for ProductsWindow.xaml
    /// </summary>
    public partial class ProductsWindow : Window
    {
        public ProductsWindow()
        {
            InitializeComponent();
            LoadProducts();
            productSearchTextBox.Focus();
        }

        private string connectionString = "Server=127.0.0.1;Database=kistest2;User Id=root;Password=;";

        string productBarcode = null;

        private void LoadProducts()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM product";


                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    productsDataGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void productsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && productsDataGrid.SelectedItem != null)
            {
                DataRowView row = (DataRowView)productsDataGrid.SelectedItem;

                productBarcode = row["barcode"].ToString();

                e.Handled = true;
            }
            else if (e.Key == Key.Down && productsDataGrid.SelectedItem != null)
            {
                int currentIndex = productsDataGrid.SelectedIndex;
                if (currentIndex < productsDataGrid.Items.Count - 2)
                {
                    productsDataGrid.SelectedIndex = currentIndex + 1;
                    productsDataGrid.ScrollIntoView(productsDataGrid.SelectedItem);
                    DataRowView row = (DataRowView)productsDataGrid.SelectedItem;

                    productBarcode = row["barcode"].ToString();

                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up && productsDataGrid.SelectedItem != null)
            {
                int currentIndex = productsDataGrid.SelectedIndex;
                if (currentIndex > 0)
                {
                    productsDataGrid.SelectedIndex = currentIndex - 1;
                    productsDataGrid.ScrollIntoView(productsDataGrid.SelectedItem);
                    DataRowView row = (DataRowView)productsDataGrid.SelectedItem;

                    productBarcode = row["barcode"].ToString();

                    e.Handled = true;
                }
            }
        }

        private void ProductsDataGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (productsDataGrid.SelectedItem != null)
            {
                productsDataGrid.Focus();
                DataRowView row = (DataRowView)productsDataGrid.SelectedItem;

                productBarcode = row["barcode"].ToString();

                e.Handled = true;
            }
        }

        private void productSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = productSearchTextBox.Text;

            if (!string.IsNullOrEmpty(searchText))
            {
                SearchProductsByName(searchText);
            }
            else
            {
                LoadProducts();
            }
        }


        private void SearchProductsByName(string searchText)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM product WHERE name LIKE @searchText";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@searchText", $"%{searchText}%");
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    productsDataGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void productSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Down) && productsDataGrid.Items.Count > 0)
            {
                productsDataGrid.SelectedIndex = 0;
                productsDataGrid.Focus();
                DataRowView row = (DataRowView)productsDataGrid.SelectedItem;

                productBarcode = row["barcode"].ToString();

                e.Handled = true;
            }
        }

        private void newProductButton_Click(object sender, RoutedEventArgs e)
        {
            NewProductWindow newProductWindow = new NewProductWindow();
            newProductWindow.Show();
        }

        private void editProductButton_Click(object sender, RoutedEventArgs e)
        {
            NewProductWindow newProductWindow = new NewProductWindow(productBarcode);
            newProductWindow.Show();
        }

        private void deleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Želite li izbrisati proizvod?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {

                        conn.Open();
                        string query = @"UPDATE document SET barcode = NULL WHERE barcode = @productBarcode; 
                                        DELETE FROM product WHERE id = @productBarcode ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@productBarcode", productBarcode);

                        cmd.ExecuteNonQuery();
                        LoadProducts();
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
