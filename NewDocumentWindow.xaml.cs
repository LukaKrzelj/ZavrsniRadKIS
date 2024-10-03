using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using ZavrsniRadKIS.Resources;

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for NewDocumentWindow.xaml
    /// </summary>
    public partial class NewDocumentWindow : Window
    {
        private DataTable documentItemsTable = null;

        int retailerID = MyVariables.retailID;
        int partnerID = MyVariables.partnerID;

        private string connectionString = "Server=127.0.0.1;Database=kistest2;User Id=root;Password=;";


        public NewDocumentWindow()
        {
            InitializeComponent();
            LoadProducts();
            documentTypeComboBox.Focus();
            documentItemsTable = new DataTable();
            InitializeDocumentItemsTable();

            documentItemsDataGrid.ItemsSource = documentItemsTable.DefaultView;
        }

        private void InitializeDocumentItemsTable()
        {
            documentItemsTable.Columns.Add("barcode", typeof(string));
            documentItemsTable.Columns.Add("name", typeof(string));
            documentItemsTable.Columns.Add("entryPrice", typeof(decimal));
            documentItemsTable.Columns.Add("VAT", typeof(decimal));
            documentItemsTable.Columns.Add("exitPrice", typeof(decimal));
            documentItemsTable.Columns.Add("amount", typeof(int));
        }



        public NewDocumentWindow(int documentID)
        {
            InitializeComponent();
            LoadProducts();
            LoadDocumentData(documentID);
        }

        private void LoadDocumentData(int documentID)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Step 1: Load the main document details
                    string documentQuery = @"
                SELECT userId, partnerId, receiptType, paymentMethod, documentType, details, retailPartner
                FROM document 
                WHERE id = @documentID";

                    MySqlCommand documentCmd = new MySqlCommand(documentQuery, conn);
                    documentCmd.Parameters.AddWithValue("@documentID", documentID);

                    using (MySqlDataReader reader = documentCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Populate the UI components with document data
                            partnerID = reader.GetInt32("partnerId");
                            MyVariables.userID = reader.GetInt32("userId");
                            MyVariables.receiptType = reader.GetString("receiptType");
                            string paymentMethod = reader.GetString("paymentMethod");
                            string documentType = reader.GetString("documentType");
                            string details = reader.GetString("details");

                            // Set the values to the corresponding UI controls
                            partnerIDTextBox.Text = partnerID.ToString();
                            detailsTextBox.Text = details;

                            // Assuming ComboBoxes have Tag properties set for each item
                            payingMethodComboBox.SelectedItem = payingMethodComboBox.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item => item.Tag.ToString() == paymentMethod);

                            documentTypeComboBox.SelectedItem = documentTypeComboBox.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item => item.Tag.ToString() == documentType);
                        }
                    }

                    // Step 2: Load the document items
                    string itemsQuery = @"
                SELECT p.barcode, p.name, p.entryPrice, p.VAT, p.exitPrice, i.amount
                FROM item i
                JOIN product p ON i.barcode = p.barcode
                WHERE i.documentId = @documentID";

                    MySqlCommand itemsCmd = new MySqlCommand(itemsQuery, conn);
                    itemsCmd.Parameters.AddWithValue("@documentID", documentID);

                    MySqlDataAdapter adapter = new MySqlDataAdapter(itemsCmd);
                    DataTable itemsTable = new DataTable();
                    adapter.Fill(itemsTable);

                    // Initialize the document items table and set its data source
                    documentItemsTable = itemsTable;
                    documentItemsDataGrid.ItemsSource = documentItemsTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading document data: {ex.Message}");
                }
            }
        }

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

        private void productSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if ((e.Key == Key.Enter || e.Key == Key.Down) && productsDataGrid.Items.Count > 0)
            {
                productsDataGrid.SelectedIndex = 0;
                productsDataGrid.Focus();
                e.Handled = true;
            }

        }

        private void ProductSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = productSearchTextBox.Text;

            if (!string.IsNullOrEmpty(searchText))
            {
                SearchProductsByName(searchText);
            }
            else
            {
                productsDataGrid.ItemsSource = null;
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
                    cmd.Parameters.AddWithValue("@searchText", $"{searchText}%");
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
                DataRowView selectedProduct = (DataRowView)productsDataGrid.SelectedItem;
                string barcode = selectedProduct["barcode"].ToString();

                AddProductToDocumentItems(barcode);
                e.Handled = true;

                documentItemsDataGrid.Focus();
                documentItemsDataGrid.CurrentCell = new DataGridCellInfo(documentItemsDataGrid.Items[documentItemsDataGrid.Items.Count - 2], documentItemsDataGrid.Columns[5]);
                documentItemsDataGrid.BeginEdit();
            }
            else if (e.Key == Key.Down && productsDataGrid.SelectedItem != null)
            {
                int currentIndex = productsDataGrid.SelectedIndex;
                if (currentIndex < productsDataGrid.Items.Count - 1)
                {
                    productsDataGrid.SelectedIndex = currentIndex + 1;
                    productsDataGrid.ScrollIntoView(productsDataGrid.SelectedItem);
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
                    e.Handled = true;
                }
            }
        }

        private void AddProductToDocumentItems(string barcode)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = @"
                SELECT 
                    p.barcode, 
                    p.name, 
                    p.entryPrice, 
                    p.VAT, 
                    p.exitPrice, 
                    d.amount 
                FROM 
                    product p 
                LEFT JOIN 
                    item d 
                ON 
                    p.barcode = d.barcode 
                WHERE 
                    p.barcode = @barcode";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@barcode", barcode);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (documentItemsTable == null)
                    {
                        documentItemsTable = dt;
                        documentItemsDataGrid.ItemsSource = documentItemsTable.DefaultView;
                    }
                    else
                    {
                        foreach (DataRow newRow in dt.Rows)
                        {
                            DataRow existingRow = documentItemsTable.Rows
                                .Cast<DataRow>()
                                .FirstOrDefault(r => r["barcode"].ToString() == newRow["barcode"].ToString());

                            if (existingRow == null)
                            {
                                documentItemsTable.ImportRow(newRow);
                            }
                            else
                            {
                                // Update the existing row if necessary
                                existingRow["amount"] = newRow["amount"]; // Assuming amount needs to be updated
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void documentItemsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                productSearchTextBox.Focus();
                productSearchTextBox.SelectAll();
                e.Handled = true;
            }
        }

        private void productsDataGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (productsDataGrid.SelectedItem != null)
            {
                productsDataGrid.Focus();
                e.Handled = true;
            }
        }

        private void partnerIDTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                partnerID = Convert.ToInt32(partnerIDTextBox.Text);
                MessageBox.Show($"{partnerID}");

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "SELECT name FROM partner WHERE id = @partnerID";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@partnerID", partnerID);

                    try
                    {
                        connection.Open();
                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            partnerTextBox.Text = result.ToString();
                        }
                        else
                        {
                            MessageBox.Show("Partner nije pronađen.");
                            partnerTextBox.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An error occurred: " + ex.Message);
                    }
                }
            }
        }

        private void partnerTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && string.IsNullOrWhiteSpace(partnerTextBox.Text))
            {
                PartnersWindow partnersWindow = new PartnersWindow();
                partnersWindow.Show();
                e.Handled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveDocument();
        }

        private void SaveDocument()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    ComboBoxItem selectedItemPayingMethod = (ComboBoxItem)payingMethodComboBox.SelectedItem;
                    string paymentMethod = selectedItemPayingMethod.Tag.ToString();
                    ComboBoxItem selectedItemDocumentType = (ComboBoxItem)documentTypeComboBox.SelectedItem;
                    string documentType = selectedItemDocumentType.Tag.ToString();
                    string details = detailsTextBox.Text.ToString();
                    int documentId = 0;

                    string checkDocumentExistsQuery = "SELECT COUNT(*) FROM document WHERE id = @documentId";
                    MySqlCommand checkCmd = new MySqlCommand(checkDocumentExistsQuery, conn);
                    checkCmd.Parameters.AddWithValue("@documentId", MyVariables.documentID);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        // Step 1a: Update the existing document
                        documentId = MyVariables.documentID;
                        string updateDocumentQuery = @"
                        UPDATE document 
                        SET userId = @userId, partnerId = @partnerId, dateTime = NOW(), receiptType = @receiptType, 
                            paymentMethod = @paymentMethod, documentType = @documentType, isCompleted = @isCompleted, 
                            details = @details, retailPartner = @retailPartner 
                        WHERE id = @documentId";

                        MySqlCommand updateCmd = new MySqlCommand(updateDocumentQuery, conn);
                        updateCmd.Parameters.AddWithValue("@documentId", MyVariables.documentID);
                        updateCmd.Parameters.AddWithValue("@userId", MyVariables.userID);
                        updateCmd.Parameters.AddWithValue("@partnerId", partnerID);
                        updateCmd.Parameters.AddWithValue("@receiptType", MyVariables.receiptType);
                        updateCmd.Parameters.AddWithValue("@paymentMethod", paymentMethod);
                        updateCmd.Parameters.AddWithValue("@documentType", documentType);
                        updateCmd.Parameters.AddWithValue("@isCompleted", 0);
                        updateCmd.Parameters.AddWithValue("@details", details);
                        updateCmd.Parameters.AddWithValue("@retailPartner", retailerID);

                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Step 1b: Insert a new document and retrieve its ID
                        string insertDocumentQuery = @"
                        INSERT INTO document (userId, partnerId, dateTime, receiptType, paymentMethod, documentType, isCompleted, details, retailPartner) 
                        VALUES (@userId, @partnerId, NOW(), @receiptType, @paymentMethod, @documentType, @isCompleted, @details, @retailPartner);
                        SELECT LAST_INSERT_ID();";

                        MySqlCommand insertCmd = new MySqlCommand(insertDocumentQuery, conn);
                        insertCmd.Parameters.AddWithValue("@userId", MyVariables.userID);
                        insertCmd.Parameters.AddWithValue("@partnerId", partnerID);
                        insertCmd.Parameters.AddWithValue("@receiptType", MyVariables.receiptType);
                        insertCmd.Parameters.AddWithValue("@paymentMethod", paymentMethod);
                        insertCmd.Parameters.AddWithValue("@documentType", documentType);
                        insertCmd.Parameters.AddWithValue("@isCompleted", 0);
                        insertCmd.Parameters.AddWithValue("@details", details);
                        insertCmd.Parameters.AddWithValue("@retailPartner", retailerID);

                        documentId = Convert.ToInt32(insertCmd.ExecuteScalar());
                    }
                

                    if (documentItemsTable != null && documentItemsTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in documentItemsTable.Rows)
                        {
                            string insertItemQuery = @"
                                                    INSERT INTO item (barcode, amount, documentId) 
                                                    VALUES (@barcode, @amount, @documentId)
                                                    ON DUPLICATE KEY UPDATE
                                                    amount = @amount";

                            MySqlCommand itemCmd = new MySqlCommand(insertItemQuery, conn);
                            itemCmd.Parameters.AddWithValue("@barcode", row["barcode"]);
                            itemCmd.Parameters.AddWithValue("@amount", row["amount"]);
                            itemCmd.Parameters.AddWithValue("@documentId", documentId);

                            itemCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Dokument i stavke uspješno spremljene.");
                    }
                    else
                    {
                        MessageBox.Show("Dokument spremljen bez stavki u njemu.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving document and items: {ex.Message}");
                }
            }
        }

    }

}
