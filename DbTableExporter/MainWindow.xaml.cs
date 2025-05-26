using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace DbTableExporter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    folderBox.Text = dialog.SelectedPath;
            }
        }

        private async void exportBtn_Click(object sender, RoutedEventArgs e)
        {
            logBox.Clear();
            string dbType = ((ComboBoxItem)dbTypeCombo.SelectedItem).Content.ToString();
            string connStr = connStrBox.Text.Trim();
            string folder = folderBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(connStr) || string.IsNullOrWhiteSpace(folder))
            {
                logBox.Text = "Please enter connection string and select output folder.";
                return;
            }

            exportBtn.IsEnabled = false;
            logBox.AppendText("Export started..." + Environment.NewLine);

            try
            {
                int tableCount = await Task.Run(() =>
                    DatabaseExporter.ExportAllTables(connStr, dbType, folder, (msg) =>
                    {
                        Dispatcher.Invoke(() => logBox.AppendText(msg + Environment.NewLine));
                    })
                );
                logBox.AppendText($"Export complete! {tableCount} tables exported.\n");
            }
            catch (Exception ex)
            {
                logBox.AppendText($"Error: {ex.Message}\n");
            }
            finally
            {
                exportBtn.IsEnabled = true;
            }
        }
    }
}