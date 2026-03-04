using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace pLawnModLoaderLauncher
{
    public partial class JsonEditWindow : Window
    {
        public string EditedJson { get; private set; }

        private ObservableCollection<ExpandoObject> _dynamicItems;
        private List<string> _propertyNames;
        private bool _useTableMode = false;

        public JsonEditWindow(string initialJson, string type)
        {
            InitializeComponent();
            Title = $"编辑 {type}";

            // 尝试解析为数组（无论传入类型）
            if (TryParseAsArray(initialJson, out _dynamicItems, out _propertyNames))
            {
                _useTableMode = true;
                SetupDataGridColumns();
                DynamicDataGrid.ItemsSource = _dynamicItems;
                DynamicDataGrid.Visibility = Visibility.Visible;
                JsonTextBox.Visibility = Visibility.Collapsed;
                ModeText.Text = "表格编辑模式 (单击单元格即可编辑)";

                // 单击单元格立即进入编辑模式
                DynamicDataGrid.PreviewMouseLeftButtonDown += DataGrid_PreviewMouseLeftButtonDown;
            }
            else
            {
                JsonTextBox.Text = initialJson;
                JsonTextBox.Visibility = Visibility.Visible;
                DynamicDataGrid.Visibility = Visibility.Collapsed;
                ModeText.Text = "文本编辑模式 (直接编辑JSON)";
            }
        }

        private bool TryParseAsArray(string json, out ObservableCollection<ExpandoObject> items, out List<string> propertyNames)
        {
            items = null;
            propertyNames = null;
            JArray array = null;

            // 使用 JToken.Parse 尝试解析
            try
            {
                var token = JToken.Parse(json);
                if (token.Type == JTokenType.Array)
                    array = (JArray)token;
                else
                    return false; // 不是数组，无法表格编辑
            }
            catch
            {
                // 解析失败，尝试清理（如去除 BOM、空白）
                try
                {
                    string cleaned = json.Trim();
                    var token = JToken.Parse(cleaned);
                    if (token.Type == JTokenType.Array)
                        array = (JArray)token;
                    else
                        return false;
                }
                catch
                {
                    return false;
                }
            }

            if (array == null)
                return false;

            // 收集所有对象的属性名
            var allProps = new HashSet<string>();
            bool hasAnyObject = false;
            foreach (var token in array)
            {
                if (token.Type == JTokenType.Object)
                {
                    hasAnyObject = true;
                    foreach (var prop in ((JObject)token).Properties())
                        allProps.Add(prop.Name);
                }
            }

            // 如果没有对象，但数组不为空，且不是空数组，可能是原始值数组，不适合表格模式
            if (!hasAnyObject && array.Count > 0)
                return false;

            // 如果没有任何属性（例如空数组或全是空对象），设置一个默认列
            if (allProps.Count == 0)
                allProps.Add("Value");

            propertyNames = allProps.ToList();

            var list = new ObservableCollection<ExpandoObject>();
            foreach (var token in array)
            {
                var expando = new ExpandoObject();
                var dict = (IDictionary<string, object>)expando;

                if (token.Type == JTokenType.Object)
                {
                    var obj = (JObject)token;
                    foreach (var propName in propertyNames)
                    {
                        if (obj.TryGetValue(propName, out JToken value))
                            dict[propName] = value.ToString();
                        else
                            dict[propName] = "";
                    }
                }
                else
                {
                    // 非对象元素（如原始值），将其作为默认列的属性
                    foreach (var propName in propertyNames)
                    {
                        dict[propName] = token.ToString();
                    }
                }

                list.Add(expando);
            }

            items = list;
            return true;
        }

        private void SetupDataGridColumns()
        {
            DynamicDataGrid.Columns.Clear();
            foreach (var name in _propertyNames)
            {
                var column = new DataGridTextColumn
                {
                    Header = name,
                    Binding = new System.Windows.Data.Binding(name)
                    {
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                    },
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };
                DynamicDataGrid.Columns.Add(column);
            }
            DynamicDataGrid.InitializingNewItem += DataGrid_InitializingNewItem;
        }

        private void DataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
        {
            if (e.NewItem is ExpandoObject expando)
            {
                var dict = (IDictionary<string, object>)expando;
                foreach (var name in _propertyNames)
                {
                    if (!dict.ContainsKey(name))
                        dict[name] = "";
                }
            }
        }

        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 查找点击的单元格
            var cell = e.OriginalSource as DependencyObject;
            while (cell != null && !(cell is DataGridCell))
            {
                cell = System.Windows.Media.VisualTreeHelper.GetParent(cell);
            }

            if (cell is DataGridCell dataGridCell)
            {
                if (!dataGridCell.IsEditing)
                {
                    dataGridCell.Focus();
                    DynamicDataGrid.BeginEdit();
                }
                e.Handled = true;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_useTableMode)
            {
                var array = new JArray();
                foreach (ExpandoObject expando in _dynamicItems)
                {
                    var dict = (IDictionary<string, object>)expando;

                    // 检查是否所有属性都为空（空白字符串）
                    bool allEmpty = true;
                    foreach (var kv in dict)
                    {
                        string str = kv.Value?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            allEmpty = false;
                            break;
                        }
                    }
                    if (allEmpty)
                        continue; // 跳过全空行

                    var obj = new JObject();
                    foreach (var kv in dict)
                    {
                        string str = kv.Value?.ToString() ?? "";
                        // 尝试转换为合适的 JSON 类型
                        if (int.TryParse(str, out int intVal))
                            obj[kv.Key] = intVal;
                        else if (bool.TryParse(str, out bool boolVal))
                            obj[kv.Key] = boolVal;
                        else
                            obj[kv.Key] = str;
                    }
                    array.Add(obj);
                }
                EditedJson = array.ToString(Formatting.Indented);
            }
            else
            {
                EditedJson = JsonTextBox.Text;
            }
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}