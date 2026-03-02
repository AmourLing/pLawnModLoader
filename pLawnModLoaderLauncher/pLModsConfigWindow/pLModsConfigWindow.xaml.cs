using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace pLawnModLoaderLauncher
{
    public partial class pLModsConfigWindow : Window
    {
        public class ConfigItem
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
        }

        private readonly string _patchName;
        private readonly string _gameDir;
        private ObservableCollection<ConfigItem> _configItems = new ObservableCollection<ConfigItem>();

        public pLModsConfigWindow(string patchName, string gameDir)
        {
            InitializeComponent();
            _patchName = patchName;
            _gameDir = gameDir;
            Title = $"{patchName} 配置";
            ConfigDataGrid.DataContext = _configItems;
            Loaded += ConfigWindow_Loaded;
        }

        private void ConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            _configItems.Clear();

            string configFilePath = Path.Combine(_gameDir, "pLMods", "config", "pLawnModLoaderConfig.json");
            if (!File.Exists(configFilePath))
                return;

            try
            {
                string json = File.ReadAllText(configFilePath);
                var root = JObject.Parse(json);
                if (root[_patchName] is JObject patchConfig)
                {
                    foreach (var prop in patchConfig.Properties())
                    {
                        string key = prop.Name;
                        JToken value = prop.Value;
                        string type = DetermineType(value);
                        string stringValue = value.ToString();
                        _configItems.Add(new ConfigItem { Key = key, Value = stringValue, Type = type });
                    }
                }
            }
            catch { }
        }

        private string DetermineType(JToken token)
        {
            if (token.Type == JTokenType.Integer)
                return "int";
            else if (token.Type == JTokenType.Boolean)
                return "bool";
            else
                return "string";
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("添加配置项", "请输入键名：", "");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                string key = dialog.Answer.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    MessageBox.Show("键名不能为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (_configItems.Any(item => item.Key == key))
                {
                    MessageBox.Show("键名已存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _configItems.Add(new ConfigItem { Key = key, Value = "", Type = "string" });
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigDataGrid.SelectedItem is ConfigItem selected)
                _configItems.Remove(selected);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject patchConfig = new JObject();
                foreach (var item in _configItems)
                {
                    JToken valueToken;
                    switch (item.Type)
                    {
                        case "int":
                            if (int.TryParse(item.Value, out int intVal))
                                valueToken = new JValue(intVal);
                            else
                                valueToken = new JValue(item.Value);
                            break;
                        case "bool":
                            if (bool.TryParse(item.Value, out bool boolVal))
                                valueToken = new JValue(boolVal);
                            else
                                valueToken = new JValue(false);
                            break;
                        default:
                            valueToken = new JValue(item.Value);
                            break;
                    }
                    patchConfig[item.Key] = valueToken;
                }

                string configFilePath = Path.Combine(_gameDir, "pLMods", "config", "pLawnModLoaderConfig.json");
                JObject root;
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    root = JObject.Parse(json);
                }
                else
                {
                    root = new JObject();
                }

                root[_patchName] = patchConfig;

                string configDir = Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                File.WriteAllText(configFilePath, root.ToString(Formatting.Indented));

                MessageBox.Show("配置已保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}