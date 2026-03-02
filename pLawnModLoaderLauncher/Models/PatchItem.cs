using System.ComponentModel;

namespace pLawnModLoaderLauncher.Models
{
    public class PatchItem : INotifyPropertyChanged
    {
        private string _name;
        public string PatchName
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(PatchName)); }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public string SourcePath { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}