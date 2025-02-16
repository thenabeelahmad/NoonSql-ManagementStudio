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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;
using System.Data.Sql;
using Microsoft.Win32;
using Microsoft.SqlServer.Server;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded; // Ensure UI is fully loaded before modifying controls
        }

        private List<string> LoadSqlServerInstances1()
        {
            List<string> servers = new List<string>();

            try
            {
                // 1️⃣ Get local SQL Server instances from registry
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"))
                {
                    if (key != null)
                    {
                        foreach (var instanceName in key.GetValueNames())
                        {
                            string serverName = Environment.MachineName;
                            string fullServerName = instanceName == "MSSQLSERVER" ? serverName : $"{serverName}\\{instanceName}";
                            if (!servers.Contains(fullServerName))
                            {
                                servers.Add(fullServerName);
                            }
                        }
                    }
                }

                // 2️⃣ Get network SQL Server instances using SqlDataSourceEnumerator
                DataTable serverTable = SqlDataSourceEnumerator.Instance.GetDataSources();
                foreach (DataRow row in serverTable.Rows)
                {
                    string serverName = row["ServerName"].ToString();
                    string instanceName = row["InstanceName"].ToString();
                    string fullServerName = string.IsNullOrEmpty(instanceName) ? serverName : $"{serverName}\\{instanceName}";
                    if (!servers.Contains(fullServerName))
                    {
                        servers.Add(fullServerName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching SQL Server instances:\n{ex.Message}", "Error", MessageBoxButton.OK);
            }

            return servers;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            comboBoxAuth.SelectedIndex = 0; // Set default selection
        }


        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string serverName = comboBoxServer.Text;
            string authType = ((ComboBoxItem)comboBoxAuth.SelectedItem).Content.ToString();
            string username = textBoxUsername.Text;
            string password = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(serverName))
            {
                MessageBox.Show("Please select a SQL Server instance.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connectionString = (authType == "Windows Authentication")
                ? $"Server={serverName};Integrated Security=True;"
                : $"Server={serverName};User Id={username};Password={password};";

            if (TestSqlConnection(connectionString))
            {
                Dashboard dashboard = new Dashboard(serverName, authType, username, password);
                dashboard.Show();
                this.Close();
            }
        }

        private bool TestSqlConnection(string connectionString)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        
        private void comboBoxAuth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (textBoxUsername != null && passwordBox != null)
            {
                bool isWindowsAuth = comboBoxAuth.SelectedIndex == 0;
                textBoxUsername.IsEnabled = !isWindowsAuth;
                passwordBox.IsEnabled = !isWindowsAuth;
            }
        }


    }
}
