using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApplication1
{
    public partial class Dashboard : Window
    {
        private string connectionString;
        private Dictionary<TabItem, DataTable> queryResults = new Dictionary<TabItem, DataTable>();

        public Dashboard(string serverName, string authType, string username, string password)
        {
            InitializeComponent();
            connectionString = BuildConnectionString(serverName, authType, username, password);
            //LoadDatabases();
            LoadDatabases3();
            txtQueryEditor.Text = "/****** Note:  Declare the Catalog First; Ex. Use MyDb; ******/";
        }

        private string BuildConnectionString(string serverName, string authType, string username, string password)
        {
            if (authType == "Windows Authentication")
            {
                return $"Server={serverName};Integrated Security=True;";
            }
            else
            {
                return $"Server={serverName};User Id={username};Password={password};";
            }
        }

        private List<string> GetViewList(string dbName)
{
    List<string> views = new List<string>();
    string connectionString = $"Server=.;Database={dbName};Integrated Security=True;";

    using (SqlConnection conn = new SqlConnection(connectionString))
    {
        conn.Open();
        string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.VIEWS"; 

        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                views.Add(reader.GetString(0));
            }
        }
    }

    return views;
}

        //private void lstDatabases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (lstDatabases.SelectedItem != null)
        //    {
        //        txtQueryEditor.Text = $"USE {lstDatabases.SelectedItem};\nSELECT * FROM INFORMATION_SCHEMA.TABLES;";
        //    }
        //}

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            TabItem selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab != null)
            {
                TextBox queryEditor = selectedTab.Content as TextBox;
                if (queryEditor != null)
                {
                    ExecuteQuery(queryEditor.Text);
                }
                else
                {
                    ExecuteQuery(txtQueryEditor.Text);
                }
            }
        }

        private async void ExecuteQuery(string query)
        {
            try
            {
                // Show loader overlay
                LoaderOverlay.Visibility = Visibility.Visible;

                await Task.Run(() =>
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand(query, con);
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // UI update must be done on the main thread
                        Dispatcher.Invoke(() =>
                        {
                            dgQueryResults.ItemsSource = dt.DefaultView;
                            dgQueryResults.IsReadOnly = true;
                            dgQueryResults.CanUserAddRows = false;
                            dgQueryResults.CanUserDeleteRows = false;
                            dgQueryResults.CanUserReorderColumns = false;
                            dgQueryResults.CanUserResizeRows = false;

                            // Store the result in the dictionary
                            TabItem selectedTab = tabControl.SelectedItem as TabItem;
                            if (selectedTab != null)
                            {
                                queryResults[selectedTab] = dt;
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Query Execution Failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Hide loader overlay
                LoaderOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab != null && queryResults.ContainsKey(selectedTab))
            {
                // Restore previous result
                dgQueryResults.ItemsSource = queryResults[selectedTab].DefaultView;
                dgQueryResults.IsReadOnly = true;  // Prevents editing
                dgQueryResults.CanUserAddRows = false;  // Prevents adding new rows
                dgQueryResults.CanUserDeleteRows = false;  // Prevents deleting rows
                dgQueryResults.CanUserReorderColumns = false;  // Optional: Prevents column reordering
                dgQueryResults.CanUserResizeRows = false;  // Optional: Prevents row resizing
            }
            else
            {
                // Clear DataGrid if no previous result exists
                dgQueryResults.ItemsSource = null;
            }
        }

        private void ExecuteQuery(string database,string query)
        {
            try
            {
                string connectionString = $"Server=.;Database={database};Integrated Security=True;";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgQueryResults.ItemsSource = dt.DefaultView;
                    dgQueryResults.IsReadOnly = true;  // Prevents editing
                    dgQueryResults.CanUserAddRows = false;  // Prevents adding new rows
                    dgQueryResults.CanUserDeleteRows = false;  // Prevents deleting rows
                    dgQueryResults.CanUserReorderColumns = false;  // Optional: Prevents column reordering
                    dgQueryResults.CanUserResizeRows = false;  // Optional: Prevents row resizing
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Query Execution Failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewQuery_Click(object sender, RoutedEventArgs e)
        {
            // Create a new TabItem
            TabItem newTab = new TabItem();

            // Create a header with text and a close button
            StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal };

            TextBlock tabText = new TextBlock
            {
                Text = $"SQLQuery{tabControl.Items.Count + 1}.sql",
                Margin = new Thickness(0, 0, 5, 0)
            };

            Button closeButton = new Button
            {
                Content = "X",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (s, ev) => tabControl.Items.Remove(newTab);

            headerPanel.Children.Add(tabText);
            headerPanel.Children.Add(closeButton);

            newTab.Header = headerPanel;

            // Create a TextBox for SQL query input
            TextBox queryEditor = new TextBox
            {
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Background = Brushes.White,
                Foreground = Brushes.Black
            };

            newTab.Content = queryEditor;

            // Add tab to TabControl
            tabControl.Items.Add(newTab);
            tabControl.SelectedItem = newTab; // Set focus to new tab
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                Title = "Open SQL Script"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;
                    string sqlScript = File.ReadAllText(filePath); // Read SQL file content

                    // Check if there's a selected tab
                    if (tabControl.SelectedItem is TabItem)
                    {
                        TabItem selectedTab = (TabItem)tabControl.SelectedItem; // ✅ Explicit declaration

                        if (selectedTab.Content is TextBox)
                        {
                            TextBox queryEditor = (TextBox)selectedTab.Content; // ✅ Explicit casting
                            queryEditor.Text = sqlScript; // Load script into the selected query tab
                        }
                        else
                        {
                            MessageBox.Show("The selected tab does not contain a valid query editor.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No tab is selected!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Function to create a new query tab
        private void CreateNewQueryTab(string content = "")
        {
            TabItem newTab = new TabItem();

            StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock tabText = new TextBlock
            {
                Text = $"SQLQuery{tabControl.Items.Count + 1}.sql",
                Margin = new Thickness(0, 0, 5, 0)
            };

            Button closeButton = new Button
            {
                Content = "X",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (s, ev) => tabControl.Items.Remove(newTab);

            headerPanel.Children.Add(tabText);
            headerPanel.Children.Add(closeButton);
            newTab.Header = headerPanel;

            // Create the query editor
            TextBox queryEditor = new TextBox
            {
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                Text = content // Load SQL script if provided
            };

            newTab.Content = queryEditor;

            // Add tab to TabControl
            tabControl.Items.Add(newTab);
            tabControl.SelectedItem = newTab; // Set focus to new tab
        }
        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            // Create a SaveFileDialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                Title = "Save SQL Query",
                FileName = "query.sql"
            };

            // Show the dialog and get the result
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sqlQuery = GetQueryFromActiveTab();

                    if (string.IsNullOrWhiteSpace(sqlQuery))
                    {
                        MessageBox.Show("No SQL query found in the active tab.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Save the content to the selected file
                    File.WriteAllText(saveFileDialog.FileName, sqlQuery);

                    MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetQueryFromActiveTab()
        {
            TabItem selectedTab = tabControl.SelectedItem as TabItem; // Declare selectedTab first
            if (selectedTab == null)
                return string.Empty;

            TextBox queryEditor = selectedTab.Content as TextBox; // Declare queryEditor separately
            if (queryEditor == null)
                return string.Empty;

            return queryEditor.Text;
        }


        // Function to get the SQL query from the active tab

        private void StopExecution_Click(object sender, RoutedEventArgs e)
        {
        }
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
        }
        private void OpenHelp_Click(object sender, RoutedEventArgs e)
        {
        }

        private TreeViewItem CreateTableNode(string tableName, string iconPath)
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            try
            {
                Image icon = new Image
                {
                    Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(0, 0, 5, 0)
                };

                stackPanel.Children.Add(icon);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}");
            }

            stackPanel.Children.Add(new TextBlock { Text = tableName });

            return new TreeViewItem
            {
                Header = stackPanel,
                Tag = tableName
            };
        }

        private TreeViewItem CreateTableNode1(string tableName, string iconPath)
        {
            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };

            Image icon = new Image
            {
                Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 5, 0),
                SnapsToDevicePixels = true
            };

            TextBlock text = new TextBlock { Text = tableName };

            stack.Children.Add(icon);
            stack.Children.Add(text);

            TreeViewItem tableNode = new TreeViewItem { Header = stack, Tag = tableName };

            // 🔹 Force Image Reload
            icon.Source.Freeze();

            return tableNode;
        }


        private void LoadDatabases3()
        {
            treeDatabases.Items.Clear();

            List<string> databases = GetDatabaseList();

            foreach (string dbName in databases)
            {
                TreeViewItem dbItem = new TreeViewItem
                {
                    Header = CreateTreeViewItem(dbName, "src/icons/database.png"),
                    Tag = dbName,
                    IsExpanded = false
                };

                TreeViewItem tablesCategory = CreateCategoryNode("Tables", "src/icons/folder.png");

                // Fetch tables dynamically
                List<string> tables = GetTableList(dbName);
                tablesCategory.Items.Clear();

                foreach (string table in tables)
                {
                    tablesCategory.Items.Add(CreateTableNode(table, "src/icons/tbl.png"));
                }

                dbItem.Items.Add(tablesCategory);
                dbItem.Items.Add(CreateCategoryNode("Views", "src/icons/folder.png"));
                dbItem.Items.Add(CreateCategoryNode("Procedures", "src/icons/folder.png"));

                dbItem.Expanded += DbItem_Expanded;
                treeDatabases.Items.Add(dbItem);
            }

            // 🔹 Force UI to refresh
            //treeDatabases.UpdateLayout();
        }

        private StackPanel CreateTreeViewItem(string header, string iconPath)
        {
            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };

            Image icon = new Image
            {
                Source = new BitmapImage(new Uri(iconPath, UriKind.Relative)),
                Width = 16,
                Height = 16,
                Margin = new Thickness(2)
            };

            TextBlock text = new TextBlock { Text = header, Margin = new Thickness(5, 0, 0, 0) };

            stack.Children.Add(icon);
            stack.Children.Add(text);

            return stack;
        }
        private TreeViewItem CreateCategoryNode(string categoryName, string iconPath)
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            try
            {
                Image icon = new Image
                {
                    Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(0, 0, 5, 0) // Adds spacing
                };

                stackPanel.Children.Add(icon);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}");
            }

            stackPanel.Children.Add(new TextBlock { Text = categoryName });

            TreeViewItem categoryItem = new TreeViewItem
            {
                Header = stackPanel,
                Tag = categoryName
            };

            categoryItem.Items.Add(new TreeViewItem { Header = "Loading..." });
            categoryItem.Expanded += CategoryItem_Expanded;

            return categoryItem;
        }
        private void DbItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem dbItem = sender as TreeViewItem;
            if (dbItem == null || dbItem.Items.Count == 0 || !(dbItem.Items[0] is TreeViewItem)) return;

            // Check if already loaded
            if (dbItem.Items.Count == 0 || !(dbItem.Items[0] is TreeViewItem)) return;
            if (dbItem.Header.ToString() != "Loading...") return;

            dbItem.Items.Clear(); // Remove placeholder

            // Add expandable categories
            dbItem.Items.Add(CreateCategoryNode("Tables","src/icons/folder.png"));
            dbItem.Items.Add(CreateCategoryNode("Views", "SRC/ICONS/FOLDER.png"));
            dbItem.Items.Add(CreateCategoryNode("Procedures", "SRC/ICONS/FOLDER.png"));
        }
        private TreeViewItem CreateTreeViewItem(string header, string iconPath, string tag)
        {
            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };

            Image icon = new Image
            {
                Source = new BitmapImage(new Uri(iconPath, UriKind.Relative)),
                Width = 16,
                Height = 16,
                Margin = new Thickness(2)
            };

            TextBlock text = new TextBlock { Text = header, Margin = new Thickness(5, 0, 0, 0) };

            stack.Children.Add(icon);
            stack.Children.Add(text);

            return new TreeViewItem
            {
                Header = stack,
                Tag = tag
            };

        }
        private void CategoryItem1_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem categoryItem = sender as TreeViewItem;
            if (categoryItem == null || categoryItem.Items.Count == 0) return;

            // Check if first item is "Loading..." and remove it
            if (categoryItem.Items.Count == 1 && categoryItem.Items[0] is TreeViewItem)
            {
                TreeViewItem firstItem = (TreeViewItem)categoryItem.Items[0];

                if (firstItem.Header.ToString() == "Loading...")
                {
                    categoryItem.Items.Clear();
                }
            }

            TreeViewItem dbItem = categoryItem.Parent as TreeViewItem;
            if (dbItem == null) return;

            string databaseName = dbItem.Tag.ToString();
            List<string> items = new List<string>();

            switch (categoryItem.Tag.ToString())
            {
                case "Tables":
                    items = GetTableList(databaseName);
                    break;
                case "Views":
                    items = GetViewList(databaseName);
                    break;
                case "Procedures":
                    //items = GetProcedureList(databaseName);
                    break;
            }

            foreach (string item in items)
            {
                TreeViewItem childItem = new TreeViewItem
                {
                    Header = item,
                    Tag = item
                };

                categoryItem.Items.Add(childItem);
            }
        }

        private void CategoryItem2_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem categoryItem = sender as TreeViewItem;
            if (categoryItem == null || categoryItem.Items.Count == 0) return;

            // 🔹 Check if first item is "Loading..." and remove it
            // Check if first item is "Loading..." and remove it
            if (categoryItem.Items.Count == 1 && categoryItem.Items[0] is TreeViewItem)
            {
                TreeViewItem firstItem = (TreeViewItem)categoryItem.Items[0];

                if (firstItem.Header.ToString() == "Loading...")
                {
                    categoryItem.Items.Clear();
                }
            }

            // 🔹 Prevent multiple loads (Only load if empty)
            if (categoryItem.Items.Count > 0) return;

            TreeViewItem dbItem = categoryItem.Parent as TreeViewItem;
            if (dbItem == null) return;

            string databaseName = dbItem.Tag.ToString();
            List<string> items = new List<string>();

            switch (categoryItem.Tag.ToString())
            {
                case "Tables":
                    items = GetTableList(databaseName);
                    break;
                case "Views":
                    items = GetViewList(databaseName);
                    break;
                case "Procedures":
                    //items = GetProcedureList(databaseName);
                    break;
            }

            foreach (string item in items)
            {
                // 🔹 Add table icon along with name
                TreeViewItem childItem = new TreeViewItem
                {
                    Header = CreateTreeViewItem(item, "SRC/ICONS/tbl.png"),
                    Tag = item
                };

                categoryItem.Items.Add(childItem);
            }
        }

        private void CategoryItem_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem categoryItem = sender as TreeViewItem;
            if (categoryItem == null) return;

            // ✅ Prevent reloading tables if already loaded
            if (categoryItem.Items.Count == 1 && categoryItem.Items[0] is TreeViewItem)
            {
                TreeViewItem firstItem = (TreeViewItem)categoryItem.Items[0];

                if (firstItem.Header.ToString() == "Loading...")
                {
                    categoryItem.Items.Clear();
                }
            }

            // Remove "Loading..." placeholder
            categoryItem.Items.Clear();

            TreeViewItem dbItem = categoryItem.Parent as TreeViewItem;
            if (dbItem == null) return;

            string databaseName = dbItem.Tag.ToString();
            List<string> items = new List<string>();

            switch (categoryItem.Tag.ToString())
            {
                case "Tables":
                    items = GetTableList(databaseName);
                    break;
                case "Views":
                    items = GetViewList(databaseName);
                    break;
            }

            foreach (string item in items)
            {
                // ✅ Create an icon-based item for tables
                StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };
                Image icon = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/src/icons/tbl.png")),
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                TextBlock text = new TextBlock { Text = item };

                stack.Children.Add(icon);
                stack.Children.Add(text);

                TreeViewItem childItem = new TreeViewItem
                {
                    Header = stack,
                    Tag = item
                };

                // ✅ Attach Right-Click Context Menu
                ContextMenu contextMenu = new ContextMenu();
                MenuItem selectTop1000 = new MenuItem { Header = "Select Top 1000 Rows" };
                selectTop1000.Click += (s, ev) => ExecuteSelectTop1000(databaseName, item);
                contextMenu.Items.Add(selectTop1000);

                // "View Table Columns" option
                MenuItem viewColumns = new MenuItem { Header = "View Table Columns" };
                viewColumns.Click += (s, ev) => ViewTableColumns(databaseName, item);
                contextMenu.Items.Add(viewColumns);

                // "View Row Count" option
                MenuItem viewRowCount = new MenuItem { Header = "View Row Count" };
                viewRowCount.Click += (s, ev) => ViewRowCount(databaseName, item);
                contextMenu.Items.Add(viewRowCount);

                // "Script Table as CREATE" option
                MenuItem scriptCreateTable = new MenuItem { Header = "Script Table as CREATE" };
                scriptCreateTable.Click += (s, ev) => ScriptTableAsCreate(databaseName, item);
                contextMenu.Items.Add(scriptCreateTable);

                // "Script Table as DROP" option
                MenuItem scriptDropTable = new MenuItem { Header = "Script Table as DROP" };
                scriptDropTable.Click += (s, ev) => ScriptTableAsDrop(databaseName, item);
                contextMenu.Items.Add(scriptDropTable);

                // "Script Table as DROP" option
                MenuItem ExportToCSV = new MenuItem { Header = "Save CSV" };
                ExportToCSV.Click += (s, ev) => ExportTableToCSV(databaseName, item);
                contextMenu.Items.Add(ExportToCSV);

                childItem.ContextMenu = contextMenu;
                categoryItem.Items.Add(childItem);
            }
        }

        private void ScriptTableAsDrop(string databaseName, string item)
        {
            ShowCustomMessageBox();
        }

        private void ExportTableToCSV(string databaseName, string tableName)
        {
            string query = $@"
        USE [{databaseName}];
        SELECT * FROM [{tableName}]";

            DataTable result = ExecuteQuery(query,1);
            SaveToCSV(result, $"{tableName}.csv");
        }

        private void SaveToCSV(DataTable result, string fileName)
        {
            try
            {
                // Ask user where to save the file
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = fileName,
                    Title = "Save CSV File"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    StringBuilder csvContent = new StringBuilder();

                    // Add column headers
                    string[] columnNames = result.Columns.Cast<DataColumn>()
                        .Select(column => "\"" + column.ColumnName.Replace("\"", "\"\"") + "\"")
                        .ToArray();
                    csvContent.AppendLine(string.Join(",", columnNames));

                    // Add row data
                    foreach (DataRow row in result.Rows)
                    {
                        string[] fields = row.ItemArray
                            .Select(field => field.ToString().Contains(",") || field.ToString().Contains("\"")
                                ? "\"" + field.ToString().Replace("\"", "\"\"") + "\""  // Escape quotes
                                : field.ToString())
                            .ToArray();
                        csvContent.AppendLine(string.Join(",", fields));
                    }

                    // Write to file
                    File.WriteAllText(saveFileDialog.FileName, csvContent.ToString(), Encoding.UTF8);

                    MessageBox.Show("CSV file saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving CSV file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void ShowCustomMessageBox()
        {
            Window window = new Window
            {
                Title = "Unauthorized Action",
                Width = 460,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20) };

            // Load Image
            Image image = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/src/icons/stop_hand.png")), // Add the image to Resources
                Width = 125,
                Height = 125,
                Margin = new Thickness(10)
            };

            TextBlock textBlock = new TextBlock
            {
                Text = "Stop, You are not authorized to drop the table!",
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Button okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(10)
            };
            okButton.Click += (s, e) => window.Close();

            panel.Children.Add(image);
            panel.Children.Add(textBlock);

            StackPanel mainPanel = new StackPanel();
            mainPanel.Children.Add(panel);
            mainPanel.Children.Add(okButton);

            window.Content = mainPanel;
            window.ShowDialog();
        }


        private void ScriptTableAsCreate(string databaseName, string tableName)
        {
            string query = $@"
    USE [{databaseName}];
    DECLARE @SQL NVARCHAR(MAX) = '';

    SELECT @SQL = @SQL + '[' + COLUMN_NAME + '] ' + DATA_TYPE + 
        CASE 
            WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL AND CHARACTER_MAXIMUM_LENGTH > 0
            THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')' 
            ELSE '' 
        END + ' ' +
        CASE 
            WHEN IS_NULLABLE = 'NO' THEN 'NOT NULL' 
            ELSE 'NULL' 
        END + ',' + CHAR(13) + CHAR(10)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = '{tableName}';

    -- Add USE statement at the top
    SET @SQL = 'USE [{databaseName}];' + CHAR(13) + CHAR(10) +
               'CREATE TABLE [{tableName}] (' + CHAR(13) + CHAR(10) + @SQL;
    
    -- Remove last comma and close the statement
    SET @SQL = LEFT(@SQL, LEN(@SQL) - 2) + CHAR(13) + CHAR(10) + ')';

    SELECT @SQL AS [Create Table Script];
";


            DataTable result = ExecuteQuery(query,1);

            if (result.Rows.Count > 0)
            {
                string createScript = result.Rows[0][0].ToString();
                txtQueryEditor.Text = createScript;
                //MessageBox.Show(createScript, "Create Table Script", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Could not generate script for table '{tableName}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ViewRowCount(string databaseName, string tableName)
        {
            string query = $"USE [{databaseName}]; SELECT '{tableName}' AS TableName, COUNT(*) AS [Row Count] FROM [{tableName}]";

            DataTable result = ExecuteQuery(query,1);

            if (result.Rows.Count > 0)
            {
                dgQueryResults.ItemsSource = result.DefaultView;
            }
            else
            {
                MessageBox.Show($"No data found for table '{tableName}'.", "Row Count", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void ViewTableColumns(string databaseName, string tableName)
        {
            try
            {
                string query = $@"
            USE [{databaseName}];
            SELECT COLUMN_NAME AS [Column Name], 
                   DATA_TYPE AS [Data Type], 
                   CHARACTER_MAXIMUM_LENGTH AS [Max Length], 
                   IS_NULLABLE AS [Nullable]
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = '{tableName}';
        ";

                DataTable resultTable = ExecuteQuery(query,1);

                if (resultTable.Rows.Count > 0)
                {
                    ShowResultsInGrid(resultTable, $"Columns for Table: {tableName}");
                }
                else
                {
                    MessageBox.Show("No columns found or table does not exist.", "Table Columns", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching table columns:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private DataTable ExecuteQuery(string query,int num)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing query:\n{ex.Message}", "Query Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return dataTable;
        }

        private void ShowResultsInGrid(DataTable table, string title)
        {
            TabItem newTab = new TabItem();
            StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal };

            TextBlock tabText = new TextBlock
            {
                Text = title,
                Margin = new Thickness(0, 0, 5, 0)
            };

            Button closeButton = new Button
            {
                Content = "X",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (s, ev) => tabControl.Items.Remove(newTab);

            headerPanel.Children.Add(tabText);
            headerPanel.Children.Add(closeButton);
            newTab.Header = headerPanel;

            DataGrid dataGrid = new DataGrid
            {
                AutoGenerateColumns = true,
                IsReadOnly = true,
                ItemsSource = table.DefaultView
            };

            newTab.Content = dataGrid;
            tabControl.Items.Add(newTab);
            tabControl.SelectedItem = newTab;
        }


        private void ExecuteSelectTop1000(string databaseName, string tableName)
        {
            string query = $"SELECT TOP 1000 * FROM {tableName}";
            //MessageBox.Show($"Executing Query:\n{query}", "SQL Query", MessageBoxButton.OK, MessageBoxImage.Information);

            // 🔹 You can now execute this query using your existing SQL connection.
            ExecuteQuery(databaseName, query);

            // 🔹 Show the result in a DataGrid or TextBox
            //ShowQueryResult(result);
        }

        //private void ShowQueryResult(DataTable result)
        //{
        //    // Assuming you have a DataGrid named "dataGridResults" in your XAML
        //    dgQueryResults.ItemsSource = result.DefaultView;
        //    dgQueryResults.IsReadOnly = true;  // Prevents editing
        //    dgQueryResults.CanUserAddRows = false;  // Prevents adding new rows
        //    dgQueryResults.CanUserDeleteRows = false;  // Prevents deleting rows
        //    dgQueryResults.CanUserReorderColumns = false;  // Optional: Prevents column reordering
        //    dgQueryResults.CanUserResizeRows = false;  // Optional: Prevents row resizing
        //}


        // Placeholder methods (replace with actual DB logic)
        private List<string> GetTableList(string dbName)
        {
            List<string> tables = new List<string>();

            string connectionString = $"Server=.;Database={dbName};Integrated Security=True;";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT DISTINCT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }

            return tables;
        }


        private List<string> GetDatabaseList()
        {
            List<string> databases = new List<string>();

            string cs = connectionString;

            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                string query = "SELECT name FROM sys.databases WHERE database_id > 4"; // Excludes system DBs

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        databases.Add(reader.GetString(0));
                    }
                }
            }

            return databases;
        }
        private void LoadTableData(string tableName)
        {
            // Fetch and display data from the selected table
            MessageBox.Show($"Loading data for table: {tableName}");

            // TODO: Replace this with actual database query logic
        }

        private void treeDatabases_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //TreeView treeView = sender as TreeView;
            //if (treeView == null) return;

            //TreeViewItem selectedItem = treeView.SelectedItem as TreeViewItem;
            //if (selectedItem == null) return;

            //string selectedName = selectedItem.Tag as string;

            //if (selectedItem.Parent is TreeView)
            //{
            //    // Database Selected
            //    MessageBox.Show($"Database Selected: {selectedName}");
            //}
            //else
            //{
            //    // Table Selected
            //    MessageBox.Show($"Table Selected: {selectedName}");
            //    LoadTableData(selectedName);
            //}
        }


    }
}
