using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using static System.String;

namespace ParadoxSimpleLauncher
{
    public static class LauncherFunc
    {
        public class GameMod
        {
            private string ModDir;
            private string JsonDir;

            private List<string> EnbModDirs;
            private List<string> DisModDirs;

            public GameMod(string gameName)
            {
                string gameInfoPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                      @"\Paradox Interactive\" + gameName;
                ModDir = gameInfoPath + "\\mod\\";
                JsonDir = gameInfoPath + "\\dlc_load.json";
                if (!Directory.Exists(ModDir) || !File.Exists(JsonDir))
                {
                    MessageBox.Show("Mod路径不存在或不是新版启动器");
                    Environment.Exit(0);
                }

                CopyModFile(gameName);

                string fileContent = File.ReadAllText(JsonDir);
                EnbModDirs = new List<string>();
                var results = Regex.Matches(fileContent, "/[^\"]+");
                foreach (var result in results)
                {
                    EnbModDirs.Add(result.ToString()?.TrimStart('/'));
                }

                EnbModDirs = EnbModDirs.Distinct().ToList();

                List<string> allModDecDirs = new List<string>();
                FileInfo[] fileInfos = new DirectoryInfo(ModDir).GetFiles("*.mod");
                foreach (var fileInfo in fileInfos)
                {
                    allModDecDirs.Add(fileInfo.Name);
                }

                DisModDirs = allModDecDirs.FindAll(s => !EnbModDirs.Contains(s));
            }

            private void CopyModFile(string gameName)
            {
                string workShopPath = GetWorkShopPath().Replace('/', '\\') + @"\steamapps\workshop\content";
                string gameId = Empty;
                switch (gameName)
                {
                    case "Europa Universalis IV":
                        gameId = "236850";
                        break;
                    case "Stellaris":
                        gameId = "281990";
                        break;
                }

                string modFilePath = workShopPath + "\\" + gameId;
                foreach (var directory in Directory.GetDirectories(modFilePath))
                {
                    string modId = directory.Split('\\').Last();
                    string descriptorPath = directory + "\\descriptor.mod";
                    if (File.Exists(descriptorPath))
                    {
                        string descContent = File.ReadAllText(descriptorPath);
                        if (Regex.IsMatch(descContent, "path=\".*\""))
                        {
                            descContent = Regex.Replace(descContent, "path=\".*\"", "path=" + directory);
                        }
                        else
                        {
                            descContent += "\r\npath=\"" + directory + "\"";
                        }

                        File.WriteAllText(ModDir + "ugc_" + modId + ".mod", descContent);
                    }
                }
            }

            public DataTable GetEnbMod()
            {
                return MakeTable(EnbModDirs);
            }

            public DataTable GetDisMod()
            {
                return MakeTable(DisModDirs);
            }

            private DataTable MakeTable(List<string> descriptorDirList)
            {
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("mod名", typeof(string));
                dataTable.Columns.Add("descriptor路径", typeof(string));
                foreach (var descriptor in descriptorDirList)
                {
                    var dataRow = dataTable.NewRow();
                    string descriptorPath = ModDir + descriptor;
                    if (!File.Exists(descriptorPath))
                    {
                        continue;
                    }

                    string descriptorContent = File.ReadAllText(descriptorPath);
                    var result = Regex.Match(descriptorContent, "name=\"[^\"]+\"");
                    string modName = result.ToString().Replace("name=\"", "");
                    modName = modName.Replace("\"", "");
                    dataRow["mod名"] = modName;
                    dataRow["descriptor路径"] = "mod/" + descriptor;
                    dataTable.Rows.Add(dataRow);
                }

                return dataTable;
            }

            public void WriteJson(string gameName, DataGrid dataGrid)
            {
                string jsonContent = "{\"disabled_dlcs\":[],\"enabled_mods\":[";
                List<string> enbList = new List<string>();
                if (dataGrid.ItemsSource is DataView dataView)
                {
                    foreach (DataRowView dataRow in dataView)
                    {
                        enbList.Add(dataRow[1].ToString());
                    }
                }

                enbList = enbList.Select(s => "\"" + s + "\"").ToList();
                foreach (var enb in enbList)
                {
                    jsonContent += enb;
                    jsonContent += ",";
                }

                jsonContent = jsonContent.TrimEnd(',') + "]}";
                File.WriteAllText(JsonDir, jsonContent);
            }
        }


        public static int GetRowIndex(DataGrid dataGrid, Point pos)
        {
            if (dataGrid.ItemsSource is DataView dataView)
            {
                var res = VisualTreeHelper.HitTest(dataGrid, pos).VisualHit;

                if (res is TextBlock textBlock)
                {
                    string text = textBlock.Text;
                    var rows = dataView.Table.Rows;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (rows[i].ItemArray.Contains(text))
                        {
                            return i;
                        }
                    }
                }
                else if (res is ScrollViewer)
                {
                    return dataView.Table.Rows.Count;
                }
            }

            return -1;
        }

        private static string GetWorkShopPath()
        {
            RegistryKey registryKey = Registry.CurrentUser;
            registryKey = registryKey.OpenSubKey(@"Software\Valve\Steam\", false);
            return registryKey?.GetValue("SteamPath").ToString();
        }
    }
}