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

namespace ZavrsniRadKIS
{
    /// <summary>
    /// Interaction logic for PrintPreviewWindow.xaml
    /// </summary>
    public partial class PrintPreviewWindow : Window
    {
        private FlowDocument _document;
        public PrintPreviewWindow(FlowDocument document)
        {
            InitializeComponent();
            _document = document;
            flowDocumentReader.Document = _document;
        }

        private void printButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDocument(_document);
        }

        private void PrintDocument(FlowDocument document)
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource idpSource = document;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Printing Document");
            }
        }
    }
}
