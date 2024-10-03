using System;
using System.Windows;
using MySql.Data.MySqlClient;

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for NewPartnerWindow.xaml
    /// </summary>
    public partial class NewPartnerWindow : Window
    {
        private string connectionString = "Server=127.0.0.1;Database=kistest2;User Id=root;Password=;";

        private int? partnerId = null;

        public NewPartnerWindow()
        {
            InitializeComponent();
        }

        public NewPartnerWindow(int existingPartnerId)
        {
            InitializeComponent();
            partnerId = existingPartnerId;
            LoadPartnerData(existingPartnerId);
        }

        private void newPartnerButton_Click(object sender, RoutedEventArgs e)
        {
            string name = partnerNameTextBox.Text;
            string oib = partnerOIBTextBox.Text;
            string telNum = partnerTelNumTextBox.Text;
            string mobNum = partnerMobNumTextBox.Text;
            string iban = partnerIBANTextBox.Text;
            string street = partnerStreetTextBox.Text;
            string city = partnerCityTextBox.Text;
            string country = partnerCountryTextBox.Text;
            string zip = partnerZIPTextBox.Text;
            bool isRetailer = (bool)partnerIsRetailerRadioButton.IsChecked;

            if (AddOrUpdatePartner(name, oib, mobNum, telNum, iban, street, city, country, zip, isRetailer) == 0)
            {
                MessageBox.Show(partnerId.HasValue ? "Partner uspješno uređen!" : "Novi partner uspješno dodan!");
                this.Close();
            }
            else if (AddOrUpdatePartner(name, oib, mobNum, telNum, iban, street, city, country, zip, isRetailer) == -1)
            {
                MessageBox.Show("Unesite ime partnera.");
            }
            else if (AddOrUpdatePartner(name, oib, mobNum, telNum, iban, street, city, country, zip, isRetailer) == -2)
            {
                MessageBox.Show("Došlo je do pogreške prilikom spremanja podataka. Pokušajte ponovno");
            }
        }

        private int AddOrUpdatePartner(string name, string oib, string mobNum, string telNum, string iban, string street, string city, string country, string zip, bool isRetailer)
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

                    if (partnerId.HasValue)
                    {
                        query = "UPDATE partner SET name = @name, oib = @oib, mobNum = @mobNum, telNum = @telNum, iban = @iban, " +
                            "street = @street, city = @city, country = @country, zip = @zip, isRetailPartner = @isRetailer WHERE id = @partnerId";
                    }
                    else
                    {
                        query = "INSERT INTO partner (name, oib, mobNum, telNum, iban, street, city, country, zip, isRetailPartner) " +
                                "VALUES (@name, @oib, @mobNum, @telNum, @iban, @street, @city, @country, @zip, @isRetailer)";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@oib", oib);
                    cmd.Parameters.AddWithValue("@mobNum", mobNum);
                    cmd.Parameters.AddWithValue("@telNum", telNum);
                    cmd.Parameters.AddWithValue("@iban", iban);
                    cmd.Parameters.AddWithValue("@street", street);
                    cmd.Parameters.AddWithValue("@city", city);
                    cmd.Parameters.AddWithValue("@country", country);
                    cmd.Parameters.AddWithValue("@zip", zip);
                    cmd.Parameters.AddWithValue("@isRetailer", isRetailer);

                    if (partnerId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@partnerId", partnerId.Value);
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

        private void LoadPartnerData(int partnerId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM partner WHERE id = @partnerId";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@partnerId", partnerId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            partnerNameTextBox.Text = reader["name"].ToString();
                            partnerOIBTextBox.Text = reader["oib"].ToString();
                            partnerMobNumTextBox.Text = reader["mobNum"].ToString();
                            partnerTelNumTextBox.Text = reader["telNum"].ToString();
                            partnerIBANTextBox.Text = reader["iban"].ToString();
                            partnerStreetTextBox.Text = reader["street"].ToString();
                            partnerCityTextBox.Text = reader["city"].ToString();
                            partnerCountryTextBox.Text = reader["country"].ToString();
                            partnerZIPTextBox.Text = reader["zip"].ToString();
                            partnerIsRetailerRadioButton.IsChecked = Convert.ToBoolean(reader["isRetailPartner"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading partner data: {ex.Message}");
                }
            }
        }
    }
}
