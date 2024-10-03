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

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for NewProductWindow.xaml
    /// </summary>
    public partial class NewProductWindow : Window
    {
        private string connectionString = "Server=127.0.0.1;Database=kistest2;User Id=root;Password=;";

        private string productBarcode = null;

        public NewProductWindow()
        {
            InitializeComponent();
        }

        public NewProductWindow(string barcode)
        {
            InitializeComponent();
            productBarcode = barcode;
            LoadProductData(productBarcode);
        }

        private void newProductButton_Click(object sender, RoutedEventArgs e)
        {
            string name = productNameTextBox.Text;
            string barcode = productBarcodeTextBox.Text;
            decimal entryPrice = decimal.Parse(productEntryPriceTextBox.Text);
            int VAT = int.Parse(VATTextBox.Text);
            decimal exitPrice = decimal.Parse(productExitPriceTextBox.Text);

            if (AddOrUpdateProduct(name, barcode, VAT, entryPrice, exitPrice) == 0)
            {
                MessageBox.Show(!string.IsNullOrEmpty(productBarcode) ? "Proizvod uspješno uređen!" : "Novi proizvod uspješno dodan!");
                this.Close();
            }
            else if (AddOrUpdateProduct(name, barcode, VAT, entryPrice, exitPrice) == -1)
            {
                MessageBox.Show("Unesite ime proizvoda.");
            }
            else if (AddOrUpdateProduct(name, barcode, VAT, entryPrice, exitPrice) == -2)
            {
                MessageBox.Show("Došlo je do pogreške prilikom spremanja podataka. Pokušajte ponovno");
            }
        }

        private int AddOrUpdateProduct(string name, string barcode, int VAT, decimal entryPrice, decimal exitPrice)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return -1;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query;

                    if (!string.IsNullOrEmpty(productBarcode))
                    {
                        query = "UPDATE product SET name = @name, barcode = @barcode, entryPrice = @entryPrice, VAT = @VAT, exitPrice = @exitPrice WHERE barcode = @productBarcode";
                    }
                    else
                    {
                        query = "INSERT INTO product (name, barcode, entryPrice, VAT, exitPrice) VALUES (@name, @barcode, @entryPrice, @VAT, @exitPrice)";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@barcode", barcode);
                    cmd.Parameters.AddWithValue("@entryPrice", entryPrice);
                    cmd.Parameters.AddWithValue("@VAT", VAT);
                    cmd.Parameters.AddWithValue("@exitPrice", exitPrice);

                    if (!string.IsNullOrEmpty(productBarcode))
                    {
                        cmd.Parameters.AddWithValue("@productBarcode", productBarcode);
                    }

                    cmd.ExecuteNonQuery();

                    return 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                    return -2;
                }
            }
        }

        private void LoadProductData(string productBarcode)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM product WHERE barcode = @barcode";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@barcode", productBarcode);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            productNameTextBox.Text = reader["name"].ToString();
                            productBarcodeTextBox.Text = reader["barcode"].ToString();
                            productEntryPriceTextBox.Text = reader["entryPrice"].ToString();
                            VATTextBox.Text = reader["VAT"].ToString();
                            productExitPriceTextBox.Text = reader["exitPrice"].ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading product data: {ex.Message}");
                }
            }
        }
    }
}

