using Microsoft.Win32;
using pLawnModLoaderLauncher.Config;
using pLawnModLoaderLauncher.Helpers;
using pLawnModLoaderLauncher.Models;
using pLawnModLoaderLauncher.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace pLawnModLoaderLauncher
{
    public partial class MainWindow : Window
    {
        private readonly string _launcherBaseDir;
        private readonly string _sourcePatchDir;
        private readonly string _sourceModLoaderDir;

        private AppConfig _config;
        private PatchManager _patchManager;
        private bool _isSwitchingScheme = false;

        public MainWindow()
        {
            InitializeComponent();

            _launcherBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            _sourcePatchDir = Path.Combine(_launcherBaseDir, "pLMod");
            _sourceModLoaderDir = _launcherBaseDir;

            _config = AppConfig.Load();

            _patchManager = new PatchManager(_sourcePatchDir);
            _patchManager.ScanPatches();

            PatchesList.ItemsSource = _patchManager.Patches;

            RefreshSchemeComboBox();
            LoadCurrentSchemeState();
            UpdateToggleAllButtonText();

            if (!string.IsNullOrEmpty(_config.CurrentScheme.GamePath) && File.Exists(_config.CurrentScheme.GamePath))
                GamePathTextBox.Text = _config.CurrentScheme.GamePath;
        }

        private void RefreshSchemeComboBox()
        {
            _isSwitchingScheme = true;
            SchemeComboBox.ItemsSource = _config.Schemes.Select(s => s.SchemeName).ToList();
            SchemeComboBox.SelectedItem = _config.CurrentSchemeName;
            _isSwitchingScheme = false;
        }

        private void LoadCurrentSchemeState()
        {
            var scheme = _config.CurrentScheme;
            foreach (var patch in _patchManager.Patches)
            {
                if (scheme.PatchStates.TryGetValue(patch.PatchName, out bool enabled))
                    patch.IsEnabled = enabled;
                else
                    patch.IsEnabled = false;
            }
        }

        private void SaveCurrentSchemeState()
        {
            var scheme = _config.CurrentScheme;
            scheme.GamePath = GamePathTextBox.Text;
            scheme.PatchStates.Clear();
            foreach (var patch in _patchManager.Patches)
                scheme.PatchStates[patch.PatchName] = patch.IsEnabled;
            _config.Save();
        }

        private void UpdateToggleAllButtonText()
        {
            if (_patchManager.Patches.Count == 0)
            {
                ToggleAllButton.Content = "一键控制";
                return;
            }
            bool allEnabled = _patchManager.Patches.All(p => p.IsEnabled);
            ToggleAllButton.Content = allEnabled ? "一键关闭所有" : "一键开启所有";
            ToggleAllButton.Background = allEnabled
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEE, 0x00, 0x00))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0xCC, 0xFF));
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择 Lawn.exe"
            };
            if (dialog.ShowDialog() == true)
            {
                GamePathTextBox.Text = dialog.FileName;
                SaveCurrentSchemeState();
            }
        }

        private void SchemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSwitchingScheme || SchemeComboBox.SelectedItem == null) return;

            string newScheme = SchemeComboBox.SelectedItem.ToString();
            if (newScheme != _config.CurrentSchemeName)
            {
                SaveCurrentSchemeState();
                _config.SwitchScheme(newScheme);
                LoadCurrentSchemeState();
                GamePathTextBox.Text = _config.CurrentScheme.GamePath;
                UpdateToggleAllButtonText();
            }
        }

        private void SaveScheme_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentSchemeState();
            MessageBox.Show("当前方案已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NewScheme_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("新建方案", "请输入新方案名称：", "新方案");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                string name = dialog.Answer.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("名称不能为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (_config.Schemes.Any(s => s.SchemeName == name))
                {
                    MessageBox.Show("方案名称已存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SaveCurrentSchemeState();

                _config.AddScheme(name);
                _config.CurrentScheme.GamePath = GamePathTextBox.Text;
                _patchManager.DisableAllPatches();

                RefreshSchemeComboBox();
                UpdateToggleAllButtonText();
            }
        }

        private void ToggleAll_Click(object sender, RoutedEventArgs e)
        {
            if (_patchManager.Patches.Count == 0) return;
            bool allEnabled = _patchManager.Patches.All(p => p.IsEnabled);
            foreach (var p in _patchManager.Patches)
                p.IsEnabled = !allEnabled;
            UpdateToggleAllButtonText();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _patchManager.ScanPatches();
            LoadCurrentSchemeState();
            UpdateToggleAllButtonText();
        }

        private bool ValidateGamePath()
        {
            if (string.IsNullOrEmpty(GamePathTextBox.Text) || !File.Exists(GamePathTextBox.Text))
            {
                MessageBox.Show("请先选择有效的 Lawn.exe 文件", "路径无效", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateGamePath()) return;
            SaveCurrentSchemeState();

            string targetGameDir = Path.GetDirectoryName(GamePathTextBox.Text);
            if (string.IsNullOrEmpty(targetGameDir) || !Directory.Exists(targetGameDir))
            {
                MessageBox.Show("游戏目录无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                CopyModLoaderFiles(_sourceModLoaderDir, targetGameDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制 pLawnModLoader 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ApplyPatchesToGameDir(targetGameDir))
            {
                MessageBox.Show("更新完成。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool ApplyPatchesToGameDir(string gameDir)
        {
            try
            {
                _patchManager.ApplyPatches(gameDir);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用补丁失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void UpdateAndLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateGamePath()) return;
            SaveCurrentSchemeState();

            string targetGameDir = Path.GetDirectoryName(GamePathTextBox.Text);
            if (string.IsNullOrEmpty(targetGameDir) || !Directory.Exists(targetGameDir))
            {
                MessageBox.Show("游戏目录无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                CopyModLoaderFiles(_sourceModLoaderDir, targetGameDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制 pLawnModLoader 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!ApplyPatchesToGameDir(targetGameDir))
                return;

            string targetExe = Path.Combine(targetGameDir, "pLawnModLoader.exe");
            if (!File.Exists(targetExe))
            {
                MessageBox.Show("复制后未找到 pLawnModLoader.exe，请检查源文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetExe,
                    WorkingDirectory = targetGameDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyModLoaderFiles(string sourceDir, string targetDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string dest = Path.Combine(targetDir, fileName);
                File.Copy(file, dest, true);
            }
        }

        private void OpenModsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateGamePath()) return;

            string targetGameDir = Path.GetDirectoryName(GamePathTextBox.Text);
            if (string.IsNullOrEmpty(targetGameDir) || !Directory.Exists(targetGameDir))
            {
                MessageBox.Show("游戏目录无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string modsFolder = Path.Combine(targetGameDir, "pLMods");
            if (!Directory.Exists(modsFolder))
                Directory.CreateDirectory(modsFolder);

            try
            {
                Process.Start("explorer.exe", modsFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件夹失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPatchReadme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string patchPath)
            {
                ShowReadme(patchPath, "补丁");
            }
        }

        private void ShowReadme(string filePath, string type)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                string patchName = Path.GetFileNameWithoutExtension(filePath);

                string[] possibleReadmeFiles = new[]
                {
                    patchName + ".txt",
                    patchName + ".md",
                    "readme.txt",
                    "README.txt",
                    "Readme.txt",
                    "说明.txt",
                    "使用说明.txt",
                    "README.md"
                };

                string readmePath = null;
                foreach (var fileName in possibleReadmeFiles)
                {
                    var possiblePath = Path.Combine(directory, fileName);
                    if (File.Exists(possiblePath))
                    {
                        readmePath = possiblePath;
                        break;
                    }
                }

                if (readmePath != null)
                {
                    var readmeWindow = new ReadmeWindow();
                    readmeWindow.Owner = this;
                    readmeWindow.Title = $"{type}说明 - {patchName}";
                    readmeWindow.LoadReadme(readmePath);
                    readmeWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"未找到{type}说明文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开{type}说明时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPatchConfig_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string patchPath)
            {
                string gameDir = Path.GetDirectoryName(_config.CurrentScheme.GamePath);
                if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
                {
                    MessageBox.Show("游戏目录无效，请先选择有效的 Lawn.exe", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string patchName = Path.GetFileNameWithoutExtension(patchPath);
                var configWindow = new pLModsConfigWindow(patchName, gameDir);
                configWindow.Owner = this;
                configWindow.ShowDialog();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveCurrentSchemeState();
            base.OnClosed(e);
        }
    }
}