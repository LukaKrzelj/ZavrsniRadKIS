using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Generic;
using ZavrsniRadKIS.Resources;
using System.Windows.Documents;
using System.Windows.Media;

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        private string connectionString = "Server=127.0.0.1;Database=kistest2;User Id=root;Password=;";

        int partnerID = MyVariables.partnerID;

        public Dashboard()
        {
            InitializeComponent();
            LoadRetailers();
        }

        private void PartnerWindowButton_Click(object sender, RoutedEventArgs e)
        {
            PartnersWindow newPartnersWindow = new PartnersWindow();
            newPartnersWindow.Show();
        }

        private void WholesaleTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem selectedItem = wholesaleTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null && (string)selectedItem.Header == "Zalihe")
            {
                //Otvara se novi prozor za pregled zaliha
                MyVariables.retailID = 1;
                StockWindow newStockWindow = new StockWindow();
                newStockWindow.Show();
            }
            else
            {
                MyVariables.retailID = 1;
                LoadDocuments((string)selectedItem.Header, 1);
            }
        }

        private void RetailerTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Dohvacamo ID poslovne jedinice
            KeyValuePair<int, string> selectedPair = (KeyValuePair<int, string>)retailersComboBox.SelectedItem;
            int selectedRetailerId = selectedPair.Key;
            //Dohvacamo sto se trazi (ulaz, izlaz, zalihe)
            TreeViewItem selectedItem = retailerTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null && (string)selectedItem.Header == "Zalihe")
            {
                //Otvara se novi prozor za pregled zaliha
                MyVariables.retailID = selectedRetailerId;
                StockWindow newStockWindow = new StockWindow();
                newStockWindow.Show();
            }
            else
            {
                MyVariables.retailID = selectedRetailerId;
                LoadDocuments((string)selectedItem.Header, selectedRetailerId);
            }

        }

        private void LoadDocuments(string type, int retailPartner)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = null;

                    if (type == "Ulaz")
                    {
                        query = "SELECT * FROM `document` WHERE retailPartner = @retailerID AND receiptType = \"inbound\"";
                        MyVariables.receiptType = "inbound";
                    }
                    else if (type == "Izlaz")
                    {
                        query = "SELECT * FROM `document` WHERE retailPartner = @retailerID AND receiptType = \"outbound\"";
                        MyVariables.receiptType = "outbound";

                    }
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@retailerID", retailPartner);

                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    receiptsDataGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void NewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyVariables.receiptType == null)
            {
                MessageBox.Show("Odaberite poslovnu jedinicu u kojoj želite napraviti račun.");
            }
            else
            {
                MyVariables.documentID = 0;
                NewDocumentWindow newDocumentWindow = new NewDocumentWindow();
                newDocumentWindow.Show();
            }

        }

        private void FinishTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Želite li fiskalizirati račun?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int documentID = SelectedDocumentId();
                if(FinishTransaction(documentID, MyVariables.receiptType, MyVariables.retailID) == 0)
                {
                    CreateOtherCopyOfTheDocument(documentID);
                }
            }
        }

        private int FinishTransaction(int documentID, string receiptType, int retailID)
        {
            MySqlTransaction transaction = null;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Check if the document exists and is already completed
                    string queryCheckCompleted = "SELECT isCompleted FROM document WHERE id = @documentId";
                    MySqlCommand cmdCheckCompleted = new MySqlCommand(queryCheckCompleted, conn);
                    cmdCheckCompleted.Parameters.AddWithValue("@documentId", documentID);

                    object result = cmdCheckCompleted.ExecuteScalar();

                    if (result == null)
                    {
                        MessageBox.Show("Traženi dokument ne postoji u bazi podataka.");
                        return -1;
                    }
                    else if (Convert.ToBoolean(result))
                    {
                        MessageBox.Show("Transakcija je već kompletirana.");
                        return -2;
                    }

                    string queryTestDocumentType = "SELECT documentType FROM document WHERE id = @documentId";
                    MySqlCommand cmdTestDocumentType = new MySqlCommand(queryTestDocumentType, conn);
                    cmdTestDocumentType.Parameters.AddWithValue("@documentId", documentID);
                    string testDocumentType = cmdTestDocumentType.ExecuteScalar().ToString();

                    if(!(testDocumentType == "invoice" || testDocumentType == "bill" || testDocumentType == "copy"))
                    {
                        MessageBox.Show("Nije moguće dovršiti transakciju na navedenom tipu dokumenta.");
                        return -3;
                    }

                    transaction = conn.BeginTransaction();

                    string queryUpdateDocument = "UPDATE document SET isCompleted = 1 WHERE id = @documentId";
                    MySqlCommand cmdUpdateDocument = new MySqlCommand(queryUpdateDocument, conn);
                    cmdUpdateDocument.Parameters.AddWithValue("@documentId", documentID);
                    cmdUpdateDocument.Transaction = transaction;
                    cmdUpdateDocument.ExecuteNonQuery();

                    string querySelectItems = "SELECT barcode, amount FROM item WHERE documentId = @documentId";
                    MySqlCommand cmdSelectItems = new MySqlCommand(querySelectItems, conn);
                    cmdSelectItems.Parameters.AddWithValue("@documentId", documentID);
                    cmdSelectItems.Transaction = transaction;

                    List<Tuple<string, int>> items = new List<Tuple<string, int>>();

                    using (MySqlDataReader reader = cmdSelectItems.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string barcode = reader.GetString("barcode");
                            int amount = reader.GetInt32("amount");
                            items.Add(new Tuple<string, int>(barcode, amount));
                        }
                    }

                    

                    foreach (var item in items)
                    {
                        string barcode = item.Item1;
                        int amount = item.Item2;
                        if (receiptType == "outbound")
                        {
                            amount = -amount;
                        }

                        string queryCheckStock = "SELECT COUNT(*) FROM stock WHERE productId = @barcode AND partnerId = @partnerId";
                        MySqlCommand cmdCheckStock = new MySqlCommand(queryCheckStock, conn);
                        cmdCheckStock.Parameters.AddWithValue("@barcode", barcode);
                        cmdCheckStock.Parameters.AddWithValue("@partnerId", retailID);
                        cmdCheckStock.Transaction = transaction;

                        int productExists = Convert.ToInt32(cmdCheckStock.ExecuteScalar());

                        if (productExists > 0)
                        {
                            string queryUpdateStock = "UPDATE stock SET stockLevel = stockLevel + @amount WHERE productId = @barcode AND partnerId = @partnerId";
                            MySqlCommand cmdUpdateStock = new MySqlCommand(queryUpdateStock, conn);
                            cmdUpdateStock.Parameters.AddWithValue("@amount", amount);
                            cmdUpdateStock.Parameters.AddWithValue("@barcode", barcode);
                            cmdUpdateStock.Parameters.AddWithValue("@partnerId", retailID);
                            cmdUpdateStock.Transaction = transaction;
                            cmdUpdateStock.ExecuteNonQuery();
                        }
                        else
                        {
                            try
                            {
                                string queryInsertStock = "INSERT INTO stock (partnerId, productId, stockLevel) VALUES (@partnerId, @barcode, @amount)";
                                MySqlCommand cmdInsertStock = new MySqlCommand(queryInsertStock, conn);
                                cmdInsertStock.Parameters.AddWithValue("@partnerId", retailID);
                                cmdInsertStock.Parameters.AddWithValue("@barcode", barcode);
                                cmdInsertStock.Parameters.AddWithValue("@amount", amount);
                                cmdInsertStock.Transaction = transaction;
                                cmdInsertStock.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error during stock insertion: {ex.Message}");
                            }
                        }
                    }

                    transaction.Commit();
                    
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Error: {exc.Message}");
                        }
                    }
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }

            return 0;
        }

        private void CreateOtherCopyOfTheDocument(int documentID)
        {
            int newDocumentID = 0;
            string receiptType = null;
            int userId = 0;
            int newPartnerId = 0;
            DateTime dateTime;
            string paymentMethod = null;
            string documentType = null;
            bool isCompleted = false;
            string details = null;
            int retailPartner = 0;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string getDocumentQuery = @"
                                             SELECT userId, partnerId, dateTime, receiptType, paymentMethod, documentType, isCompleted, details, retailPartner 
                                             FROM document 
                                             WHERE id = @documentID";

                    MySqlCommand getDocumentCmd = new MySqlCommand(getDocumentQuery, conn);
                    getDocumentCmd.Parameters.AddWithValue("@documentID", documentID);

                    using (MySqlDataReader reader = getDocumentCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if((string)reader["receiptType"] == "inbound")
                            {
                                receiptType = "outbound";
                            }
                            else
                            {
                                receiptType = "inbound";
                            }
                            userId = Convert.ToInt32(reader["userId"]);
                            newPartnerId = Convert.ToInt32(reader["partnerId"]);
                            dateTime = Convert.ToDateTime(reader["dateTime"]);
                            paymentMethod = reader["paymentMethod"].ToString();
                            documentType = reader["documentType"].ToString();
                            isCompleted = Convert.ToBoolean(reader["isCompleted"]);
                            details = reader["details"].ToString();
                            retailPartner = Convert.ToInt32(reader["retailPartner"]);

                            reader.Close();

                            string insertDocumentQuery = @"
                        INSERT INTO document (userId, partnerId, dateTime, receiptType, paymentMethod, documentType, isCompleted, details, retailPartner) 
                        VALUES (@userId, @partnerId, @dateTime, @receiptType, @paymentMethod, @documentType, @isCompleted, @details, @retailPartner);
                        SELECT LAST_INSERT_ID();";

                            MySqlCommand insertDocumentCmd = new MySqlCommand(insertDocumentQuery, conn);
                            insertDocumentCmd.Parameters.AddWithValue("@userId", userId);
                            insertDocumentCmd.Parameters.AddWithValue("@partnerId", retailPartner);
                            insertDocumentCmd.Parameters.AddWithValue("@dateTime", dateTime);
                            insertDocumentCmd.Parameters.AddWithValue("@receiptType", receiptType);
                            insertDocumentCmd.Parameters.AddWithValue("@paymentMethod", paymentMethod);
                            insertDocumentCmd.Parameters.AddWithValue("@documentType", documentType);
                            insertDocumentCmd.Parameters.AddWithValue("@isCompleted", 0);
                            insertDocumentCmd.Parameters.AddWithValue("@details", details);
                            insertDocumentCmd.Parameters.AddWithValue("@retailPartner", newPartnerId);

                            reader.Close();

                            newDocumentID = Convert.ToInt32(insertDocumentCmd.ExecuteScalar());

                            string getItemsQuery = "SELECT barcode, amount FROM item WHERE documentId = @documentID";
                            MySqlCommand getItemsCmd = new MySqlCommand(getItemsQuery, conn);
                            getItemsCmd.Parameters.AddWithValue("@documentID", documentID);

                            using (MySqlDataReader itemReader = getItemsCmd.ExecuteReader())
                            {
                                var items = new List<Tuple<string, int>>();

                                while (itemReader.Read())
                                {
                                    string barcode = itemReader["barcode"].ToString();
                                    int amount = Convert.ToInt32(itemReader["amount"]);
                                    items.Add(new Tuple<string, int>(barcode, amount));
                                }

                                itemReader.Close();

                                foreach (var item in items)
                                {
                                    string insertItemQuery = @"
                                                            INSERT INTO item (barcode, amount, documentId) 
                                                            VALUES (@barcode, @amount, @newDocumentID)";

                                    MySqlCommand insertItemCmd = new MySqlCommand(insertItemQuery, conn);
                                    insertItemCmd.Parameters.AddWithValue("@barcode", item.Item1);
                                    insertItemCmd.Parameters.AddWithValue("@amount", item.Item2);
                                    insertItemCmd.Parameters.AddWithValue("@newDocumentID", newDocumentID);

                                    insertItemCmd.ExecuteNonQuery();
                                }
                            }

                            MessageBox.Show("The document has been successfully copied to the new retail partner.");
                            
                        }
                        else
                        {
                            MessageBox.Show("The original document was not found.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying the document: {ex.Message}");
                }
            }

            FinishTransaction(newDocumentID, receiptType, newPartnerId);
        }

        private void LoadRetailers()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM partner WHERE isRetailPartner = 1 AND id != 1";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        string retailerName = row["name"].ToString();
                        int retailerId = Convert.ToInt32(row["id"]);

                        retailersComboBox.Items.Add(new KeyValuePair<int, string>(retailerId, retailerName));
                        retailersComboBox.DisplayMemberPath = "Value";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private int SelectedDocumentId()
        {
            int selectedId = -1;
            if (receiptsDataGrid.SelectedItem != null)
            {
                //Oznaceni red u DataGridu za dokumente
                DataRowView selectedRow = (DataRowView)receiptsDataGrid.SelectedItem;

                //Izvucemo iz njega ID
                selectedId = Convert.ToInt32(selectedRow["ID"]);
                MyVariables.documentID = selectedId;
            }
            else
            {
                MessageBox.Show("No row selected.");
            }

            return selectedId;
        }

        private void productsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ProductsWindow productsWindow = new ProductsWindow();
            productsWindow.Show();
        }

        private void editDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            NewDocumentWindow newDocumentWindow = new NewDocumentWindow(SelectedDocumentId());
            newDocumentWindow.Show();
        }

        private void receiptsDataGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SelectedDocumentId();
        }

        private void printDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            PrintPreviewWindow newPrintPreviewWindow = new PrintPreviewWindow(CreateFlowDocumentForPrintPreview(MyVariables.documentID));
            newPrintPreviewWindow.Show();
        }

        private FlowDocument CreateFlowDocumentForPrintPreview(int documentID)
        {
            decimal totalPrice = 0;

            FlowDocument doc = new FlowDocument
            {
                // A4 (595 points x 842 points)
                PageWidth = 595,
                PageHeight = 842,
                ColumnWidth = 595,
                PagePadding = new Thickness(30)
            };

            string documentType = null;
            DateTime documentDateTime = DateTime.Now;
            int partnerId = 0;
            int retailPartnerId = 0;
            string paymentMethod = null;
            string docDetails = null;
            string partnerInfo = "";
            string retailPartnerInfo = "";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string getDocumentDetailsQuery = @"
                    SELECT *  
                    FROM document
                    WHERE id = @documentID";

                    MySqlCommand docCmd = new MySqlCommand(getDocumentDetailsQuery, conn);
                    docCmd.Parameters.AddWithValue("@documentID", documentID);

                    using (MySqlDataReader docReader = docCmd.ExecuteReader())
                    {
                        if (docReader.Read())
                        {
                            documentType = docReader["documentType"].ToString();
                            paymentMethod = docReader["paymentMethod"].ToString();
                            docDetails = docReader["details"].ToString();
                            partnerId = Convert.ToInt32(docReader["partnerId"]);
                            retailPartnerId = Convert.ToInt32(docReader["retailPartner"]);
                            documentDateTime = Convert.ToDateTime(docReader["dateTime"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Paragraph errorParagraph = new Paragraph(new Run($"Error fetching document details: {ex.Message}"));
                    doc.Blocks.Add(errorParagraph);
                    return doc;
                }
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string getPartnerDetailsQuery = @"
                    SELECT *  
                    FROM partner
                    WHERE id = @partnerId OR id = @retailPartnerId";

                    MySqlCommand partnerCmd = new MySqlCommand(getPartnerDetailsQuery, conn);
                    partnerCmd.Parameters.AddWithValue("@partnerId", partnerId);
                    partnerCmd.Parameters.AddWithValue("@retailPartnerId", retailPartnerId);

                    using (MySqlDataReader partnerReader = partnerCmd.ExecuteReader())
                    {
                        while (partnerReader.Read())
                        {
                            int currentPartnerId = Convert.ToInt32(partnerReader["id"]);
                            string partnerName = partnerReader["name"].ToString();
                            string address = $"Ulica: {partnerReader["street"]}, \nGrad: {partnerReader["city"]} {partnerReader["zip"]}, \nDržava: {partnerReader["country"]}";
                            string contact = $"Broj mobitela: {partnerReader["mobNum"]}\nBroj telefona: {partnerReader["telNum"]}";

                            if (currentPartnerId == partnerId)
                            {
                                partnerInfo = $"Kupac: {partnerName}\n{address}\n{contact}";
                            }
                            else if (currentPartnerId == retailPartnerId)
                            {
                                retailPartnerInfo = $"Poslovna jedinica: {partnerName}\n{address}\n{contact}";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Paragraph errorParagraph = new Paragraph(new Run($"Error fetching partner details: {ex.Message}"));
                    doc.Blocks.Add(errorParagraph);
                    return doc;
                }
            }

            if(documentType == "invoice")
            {
                documentType = "Dostavnica";
            }
            else if(documentType == "bill")
            { 
                documentType = "Račun R1";
            }
            else if(documentType == "inventory")
            {
                documentType = "Inventura";
            }
            else if(documentType == "order")
            {
                documentType = "Narudžba";
            }
            else
            {
                documentType = "Ponuda";
            }

            Paragraph header = new Paragraph(new Run($"{documentType} {documentID}"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(header);

            Table infoTable = new Table();
            infoTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            infoTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            infoTable.RowGroups.Add(new TableRowGroup());

            TableRow infoRow = new TableRow();

            TableCell leftCell = new TableCell(new Paragraph(new Run(partnerInfo)))
            {
                TextAlignment = TextAlignment.Left,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10)
            };
            infoRow.Cells.Add(leftCell);

            TableCell rightCell = new TableCell(new Paragraph(new Run(retailPartnerInfo)))
            {
                TextAlignment = TextAlignment.Right,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10)
            };
            infoRow.Cells.Add(rightCell);

            infoTable.RowGroups[0].Rows.Add(infoRow);

            doc.Blocks.Add(infoTable);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string getItemsQuery = @"
                    SELECT i.barcode, i.amount, p.name, p.VAT, p.exitPrice 
                    FROM item i
                    JOIN product p ON i.barcode = p.barcode
                    WHERE i.documentId = @documentID";

                    MySqlCommand cmd = new MySqlCommand(getItemsQuery, conn);
                    cmd.Parameters.AddWithValue("@documentID", documentID);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            Table table = new Table();
                            table.CellSpacing = 5;
                            table.Columns.Add(new TableColumn());
                            table.Columns.Add(new TableColumn());
                            table.Columns.Add(new TableColumn());
                            table.Columns.Add(new TableColumn());
                            table.RowGroups.Add(new TableRowGroup());

                            TableRow headerRow = new TableRow();
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Ime proizvoda"))) { TextAlignment = TextAlignment.Center });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Količina"))) { TextAlignment = TextAlignment.Center });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("PDV (%)"))) { TextAlignment = TextAlignment.Center });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Cijena"))) { TextAlignment = TextAlignment.Center });
                            headerRow.FontWeight = FontWeights.Bold;
                            table.RowGroups[0].Rows.Add(headerRow);

                            int rowIndex = 0;
                            while (reader.Read())
                            {
                                string productName = reader["name"].ToString();
                                int amount = Convert.ToInt32(reader["amount"]);
                                int VAT = Convert.ToInt32(reader["VAT"]);
                                decimal exitPrice = Convert.ToDecimal(reader["exitPrice"]);
                                totalPrice += amount * exitPrice;

                                TableRow row = new TableRow();
                                row.Cells.Add(new TableCell(new Paragraph(new Run(productName))) { TextAlignment = TextAlignment.Center });
                                row.Cells.Add(new TableCell(new Paragraph(new Run(amount.ToString()))) { TextAlignment = TextAlignment.Center });
                                row.Cells.Add(new TableCell(new Paragraph(new Run(VAT.ToString()))) { TextAlignment = TextAlignment.Center });
                                row.Cells.Add(new TableCell(new Paragraph(new Run(exitPrice.ToString()))) { TextAlignment = TextAlignment.Center });

                                if (rowIndex % 2 == 1)
                                {
                                    row.Background = Brushes.LightGray;
                                }

                                table.RowGroups[0].Rows.Add(row);
                                rowIndex++;
                            }

                            doc.Blocks.Add(table);
                        }
                        else
                        {
                            Paragraph noItemsParagraph = new Paragraph(new Run("Dokument nema proizvoda."));
                            doc.Blocks.Add(noItemsParagraph);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Paragraph errorParagraph = new Paragraph(new Run($"Error fetching items: {ex.Message}"));
                    doc.Blocks.Add(errorParagraph);
                }
            }

            Paragraph price = new Paragraph(new Run($"Krajnja cijena: {totalPrice}"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };
            doc.Blocks.Add(price);

            return doc;
        }






    }
}
