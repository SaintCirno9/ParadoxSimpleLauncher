using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ParadoxSimpleLauncher
{
    public static class TransHelperFunc
    {
        public static void CreateSimpChineseVer(ref List<FileInfo> allFiles)
        {
            List<FileInfo> englishFiles = allFiles.FindAll(file => file.Name.Contains("english"));
            List<FileInfo> chineseFiles = allFiles.FindAll(file => file.Name.Contains("simp_chinese"));
            List<YmlContent> englishContents = new List<YmlContent>();
            List<YmlContent> chineseContents = new List<YmlContent>();

            foreach (var englishFile in englishFiles)
            {
                GetFileContent(englishFile.FullName, out string[,] fileContent);
                if (fileContent != null)
                {
                    for (int i = 0; i < fileContent.GetLength(0); i++)
                    {
                        englishContents.Add(new YmlContent(fileContent[i, 0], fileContent[i, 1], "English"));
                    }
                }
            }

            foreach (var chineseFile in chineseFiles)
            {
                GetFileContent(chineseFile.FullName, out string[,] fileContent);
                if (fileContent != null)
                {
                    for (int i = 0; i < fileContent.GetLength(0); i++)
                    {
                        chineseContents.Add(new YmlContent(fileContent[i, 0], fileContent[i, 1], "Chinese"));
                    }
                }
            }

            List<YmlContent> results = new List<YmlContent>();
            results.AddRange(englishContents);
            results.AddRange(chineseContents);
            results = results.Distinct(new YmlContentComparer()).ToList();

            var selectedResults = from ymlContent in results
                orderby ymlContent.key, ymlContent.type descending
                group ymlContent by ymlContent.key
                into keyGroups
                where keyGroups.First().type == "English"
                select keyGroups.Last();

            //Console.WriteLine(results.Count);
            FileStream fileStream =
                new FileStream(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\common_l_simp_chinese.yml",
                    FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream, new UTF8Encoding(true));
            streamWriter.WriteLine("l_simp_chinese:");
            foreach (var result in selectedResults)
            {
                streamWriter.WriteLine(result.key + ":" + result.value + "\n");
                streamWriter.Flush();
            }

            fileStream.Close();
            File.Move(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\common_l_simp_chinese.yml",
                @"C:\Users\Cirno\Documents\Paradox Interactive\Stellaris\mod\mega_engineering\localisation\simp_chinese\common_l_simp_chinese.yml",
                true);
        }

        public static void GetAllFiles(string rootPath, List<FileInfo> fList, string searchPattern)
        {
            DirectoryInfo[] childDirectories = new DirectoryInfo(rootPath).GetDirectories();
            FileInfo[] fileNames = new DirectoryInfo(rootPath).GetFiles(searchPattern);
            fList.AddRange(fileNames.ToList());
            foreach (var childDirectory in childDirectories)
            {
                GetAllFiles(childDirectory.FullName, fList, searchPattern);
            }
        }

        public static void GetFileContent(string filePath, out string[,] fileContent)
        {
            string rawContent = File.ReadAllText(filePath);
            Format(rawContent, out fileContent);
        }

        public static void Format(string fileContent, out string[,] content)
        {
            List<string> fileLines = fileContent.Split('\n').ToList();
            fileLines = fileLines.FindAll(s => !s.Contains("#") && s.Contains(":") && Regex.IsMatch(s, "[^\\s]+"));
            if (fileLines.Count == 0)
            {
                content = null;
                return;
            }

            fileLines.RemoveAt(0);

            content = new string[fileLines.Count, 2];
            for (int i = 0; i < fileLines.Count; i++)
            {
                string[] parts = fileLines[i].Split(':');
                string key = parts[0];
                string value = string.Empty;
                if (parts.Length > 2)
                {
                    for (int ii = 1; ii < parts.Length; ii++)
                    {
                        value += parts[ii];
                    }
                }
                else
                {
                    if (parts.Length == 1)
                    {
                        Console.WriteLine(key);
                    }
                    else
                    {
                        value = parts[1];
                    }
                }

                while (!char.IsLetterOrDigit(key.First()))
                {
                    key = key.Remove(0, 1);
                }

                key = " " + key;


                if (value.StartsWith("\""))
                {
                    value = " " + value;
                }


                if (value.StartsWith("0\""))
                {
                    value = value.Replace("0\"", "0 \"");
                }

                if (value.StartsWith("1\""))
                {
                    value = value.Replace("1\"", "1 \"");
                }

                content[i, 0] = key;
                content[i, 1] = value;
            }
        }
    }

    public class YmlContent
    {
        public string key;
        public string value;
        public string type;

        public YmlContent(string key, string value, string type)
        {
            this.key = key;
            this.value = value;
            this.type = type;
        }

        public YmlContent()
        {
            key = string.Empty;
            value = string.Empty;
            type = string.Empty;
        }
    }

    public class YmlContentComparer : IEqualityComparer<YmlContent>
    {
        public bool Equals(YmlContent x, YmlContent y)
        {
            return x.key == y.key && x.value == y.value;
        }

        public int GetHashCode(YmlContent obj)
        {
            return obj.key.GetHashCode();
        }
    }
}