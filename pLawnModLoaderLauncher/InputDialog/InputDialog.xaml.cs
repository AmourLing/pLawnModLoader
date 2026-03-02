using System.Windows;

namespace pLawnModLoaderLauncher
{
    public partial class InputDialog : Window
    {
        public string Answer { get; set; }

        public InputDialog(string title, string question, string defaultAnswer = "")
        {
            InitializeComponent();
            this.Title = title;
            lblQuestion.Text = question;
            txtAnswer.Text = defaultAnswer;

            this.Loaded += (s, e) =>
            {
                txtAnswer.Focus();
                txtAnswer.SelectAll();
            };
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            Answer = txtAnswer.Text;
            this.DialogResult = true;
        }
    }
}