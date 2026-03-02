using System.IO;
using System.Text;
using System.Windows;

namespace pLawnModLoaderLauncher
{
    public partial class ReadmeWindow : Window
    {
        public ReadmeWindow()
        {
            InitializeComponent();
        }

        public void LoadReadme(string readmePath)
        {
            try
            {
                if (File.Exists(readmePath))
                {
                    string content = File.ReadAllText(readmePath, Encoding.UTF8);
                    ReadmeText.Text = content;
                }
                else
                {
                    ReadmeText.Text = "找不到说明文件。";
                }
            }
            catch
            {
                ReadmeText.Text = "读取说明文件时出错。";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ReadmeText.Text))
            {
                Clipboard.SetText(ReadmeText.Text);
                var originalContent = CopyButton.Content;
                CopyButton.Content = "已复制!";
                CopyButton.IsEnabled = false;
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = System.TimeSpan.FromMilliseconds(800)
                };
                timer.Tick += (s, _) =>
                {
                    CopyButton.Content = originalContent;
                    CopyButton.IsEnabled = true;
                    timer.Stop();
                };
                timer.Start();
            }
        }
    }
}