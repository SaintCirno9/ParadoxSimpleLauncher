using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

namespace ParadoxSimpleLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DisGrid.MouseMove += OnMouseMove;
            DisGrid.Drop += OnDrop;
            EnbGrid.MouseMove += OnMouseMove;
            EnbGrid.Drop += OnDrop;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var data = e.Data;
            if (data.GetDataPresent(typeof(DataRowView)) && sender is DataGrid dataGrid)
            {
                if (data.GetData(typeof(DataRowView)) is DataRowView dataRowView &&
                    dataGrid.ItemsSource is DataView dataView)
                {
                    int index = LauncherFunc.GetRowIndex(dataGrid, e.GetPosition(dataGrid));
                    if (index == -1)
                    {
                        e.Effects = DragDropEffects.None;
                        return;
                    }

                    DataTable dataTable = dataView.Table;
                    var newRow = dataTable.NewRow();
                    newRow.ItemArray = dataRowView.Row.ItemArray;
                    dataTable.Rows.InsertAt(newRow, index);
                    Save.Visibility = Visibility.Visible;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && Mouse.DirectlyOver.GetType() == typeof(TextBlock))
            {
                if (sender is DataGrid dataGrid)
                {
                    var selectItem = dataGrid.SelectedItem;
                    if (selectItem == null)
                    {
                        return;
                    }

                    var res = DragDrop.DoDragDrop(DisGrid, selectItem, DragDropEffects.Move);
                    if (!res.Equals(DragDropEffects.None))
                    {
                        if (selectItem is DataRowView dataRowView && dataGrid.ItemsSource is DataView dataView)
                        {
                            DataRow dataRow = dataRowView.Row;
                            DataTable dataTable = dataView.Table;
                            dataTable.Rows.Remove(dataRow);
                        }
                    }
                }
            }
        }

        private static LauncherFunc.GameMod GameMod;

        private void MenuItem_OnClick1(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                InitializeComponent();
                string gameName = menuItem.Header.ToString();

                GameMod = new LauncherFunc.GameMod(gameName);

                DataTable enbTable = GameMod.GetEnbMod();
                EnbGrid.ItemsSource = enbTable.DefaultView;
                EnbGrid.Visibility = Visibility.Visible;

                DataTable disTable = GameMod.GetDisMod();
                DisGrid.ItemsSource = disTable.DefaultView;
                DisGrid.Visibility = Visibility.Visible;

                Save.Header = "保存" + gameName;

                // Start.Header = "启动" + gameName;
                // Start.Visibility = Visibility.Visible;
            }
        }

        private void MenuItem_OnClick2(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string gameName = menuItem.Header.ToString()?.Replace("保存", "");
                GameMod.WriteJson(gameName, EnbGrid);
                Save.Visibility = Visibility.Collapsed;
            }
        }

        private void MenuItem_OnClick3(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string gameName = menuItem.Header.ToString()?.Replace("启动", "");
                string gameExePath = @"D:\Games\Steam\steamapps\common\" + gameName;
                switch (gameName)
                {
                    case "Europa Universalis IV":
                        gameExePath += "\\eu4.exe";
                        break;
                    case "Stellaris":
                        gameExePath += "\\stellaris.exe";
                        break;
                    case "Hearts of Iron IV":
                        gameExePath += "\\hoi4.exe";
                        break;
                }

                Process.Start(gameExePath);
                Process.GetCurrentProcess().Kill();
            }
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && EnbGrid.ItemsSource is DataView enbView &&
                DisGrid.ItemsSource is DataView disView)
            {
                DataTable enbTable = enbView.Table;
                DataTable disTable = disView.Table;
                string op = button.Content.ToString();
                if (op == null)
                {
                    return;
                }

                if (op.Equals("全部取消"))
                {
                    foreach (DataRow enbRow in enbTable.Rows)
                    {
                        disTable.Rows.Add(enbRow.ItemArray);
                    }

                    enbTable.Clear();
                }
                else if (op.Equals("全部激活"))
                {
                    foreach (DataRow disRow in disTable.Rows)
                    {
                        enbTable.Rows.Add(disRow.ItemArray);
                    }

                    disTable.Clear();
                }

                Save.Visibility = Visibility.Visible;
            }
        }

        private void CreateChiYml(object sender, RoutedEventArgs e)
        {
            string workShopPath = @"D:\Games\Steam\steamapps\workshop\content\281990";
            List<FileInfo> allFiles = new List<FileInfo>();
            TransHelperFunc.GetAllFiles(workShopPath, allFiles, "*.yml");
            TransHelperFunc.CreateSimpChineseVer(ref allFiles);
        }
    }
}