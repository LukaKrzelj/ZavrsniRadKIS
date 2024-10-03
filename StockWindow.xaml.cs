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
    /// Interaction logic for StockWindow.xaml
    /// </summary>
    public partial class StockWindow : Window
    {
        public StockWindow()
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
                ShowStock(productBarcode);
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
                    ShowStock(productBarcode);
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
                    ShowStock(productBarcode);
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
                ShowStock(productBarcode);
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

        private void productSearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Down) && productsDataGrid.Items.Count > 0)
            {
                productsDataGrid.SelectedIndex = 0;
                productsDataGrid.Focus();
                DataRowView row = (DataRowView)productsDataGrid.SelectedItem;

                productBarcode = row["barcode"].ToString();
                ShowStock(productBarcode);
                e.Handled = true;
            }
        }

        private void ShowStock(string productBarcode)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = @"
                    SELECT 
                    p.barcode, 
                    p.name AS productName, 
                    z.name AS partnerName, 
                    d.stockLevel 
                    FROM 
                    product p 
                    LEFT JOIN 
                    stock d 
                    ON p.barcode = d.productId 
                    LEFT JOIN
                    partner z
                    ON d.partnerId = z.id 
                    WHERE 
                    p.barcode = @barcode AND z.isRetailPartner = 1;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@barcode", productBarcode);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    stockDataGrid.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private FlowDocument CreateFlowDocumentForStock(int retailPartnerId)
        {
            FlowDocument doc = new FlowDocument
            {
                // A4 (595 points x 842 points)
                PageWidth = 595,
                PageHeight = 842,
                ColumnWidth = 595,
                PagePadding = new Thickness(30)
            };

            string documentType = "Inventurna lista";
            string retailPartnerInfo = null;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string getPartnerDetailsQuery = @"
                    SELECT *  
                    FROM partner
                    WHERE id = @retailPartnerId";

                    MySqlCommand partnerCmd = new MySqlCommand(getPartnerDetailsQuery, conn);
                    partnerCmd.Parameters.AddWithValue("@retailPartnerId", retailPartnerId);

                    using (MySqlDataReader partnerReader = partnerCmd.ExecuteReader())
                    {
                        while (partnerReader.Read())
                        {
                            int currentPartnerId = Convert.ToInt32(partnerReader["id"]);
                            string partnerName = partnerReader["name"].ToString();
                            string address = $"Ulica: {partnerReader["street"]}, \nGrad: {partnerReader["city"]} {partnerReader["zip"]}, \nDržava: {partnerReader["country"]}";
                            string contact = $"Broj mobitela: {partnerReader["mobNum"]}\nBroj telefona: {partnerReader["telNum"]}";

                            retailPartnerInfo = $"Poslovna jedinica: {partnerName}\n{address}\n{contact}";
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

            Paragraph header = new Paragraph(new Run($"{documentType}"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(header);

            Paragraph details = new Paragraph(new Run($"{retailPartnerInfo}\nDate: {DateTime.Now.ToShortDateString()}"))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Center
            };
            doc.Blocks.Add(details);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string getStockQuery = @"
                    SELECT s.productId, s.stockLevel, p.name
                    FROM stock s
                    LEFT JOIN product p ON barcode = s.productId
                    WHERE s.partnerId = @retailPartnerId";

                    MySqlCommand stockCmd = new MySqlCommand(getStockQuery, conn);
                    stockCmd.Parameters.AddWithValue("@retailPartnerId", retailPartnerId);

                    using (MySqlDataReader stockReader = stockCmd.ExecuteReader())
                    {
                        if (stockReader.HasRows)
                        {
                            Table stockTable = new Table();
                            stockTable.CellSpacing = 5;
                            stockTable.Columns.Add(new TableColumn());
                            stockTable.Columns.Add(new TableColumn());
                            stockTable.Columns.Add(new TableColumn());
                            stockTable.RowGroups.Add(new TableRowGroup());

                            TableRow headerRow = new TableRow();
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Ime proizvoda"))) { TextAlignment = TextAlignment.Center });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Stanje zalihe"))) { TextAlignment = TextAlignment.Center });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Pravo stanje"))) { TextAlignment = TextAlignment.Center });
                            headerRow.FontWeight = FontWeights.Bold;
                            stockTable.RowGroups[0].Rows.Add(headerRow);

                            while (stockReader.Read())
                            {
                                string productId = stockReader["name"].ToString();
                                int stockLevel = Convert.ToInt32(stockReader["stockLevel"]);

                                TableRow row = new TableRow();
                                row.Cells.Add(new TableCell(new Paragraph(new Run(productId))) { TextAlignment = TextAlignment.Center });
                                row.Cells.Add(new TableCell(new Paragraph(new Run(stockLevel.ToString()))) { TextAlignment = TextAlignment.Center });

                                TableCell emptyBoxCell = new TableCell(new Paragraph(new Run("__________")))
                                {
                                    TextAlignment = TextAlignment.Center
                                };
                                row.Cells.Add(emptyBoxCell);

                                stockTable.RowGroups[0].Rows.Add(row);
                            }

                            // Add the table to the document
                            doc.Blocks.Add(stockTable);
                        }
                        else
                        {
                            Paragraph noStockParagraph = new Paragraph(new Run("No stock data found for this retail partner."));
                            doc.Blocks.Add(noStockParagraph);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Paragraph errorParagraph = new Paragraph(new Run($"Error fetching stock data: {ex.Message}"));
                    doc.Blocks.Add(errorParagraph);
                }
            }

            return doc;
        }

        private void inventoryListButton_Click(object sender, RoutedEventArgs e)
        {
            PrintPreviewWindow newPrintPreviewWindow = new PrintPreviewWindow(CreateFlowDocumentForStock(MyVariables.retailID));
            newPrintPreviewWindow.Show();
        }
    }
}
