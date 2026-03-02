using pLawnModLoaderLauncher.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace pLawnModLoaderLauncher.Services
{
    public class PatchManager
    {
        private readonly string _sourceFolder;
        private readonly string _targetFolder = "pLMods";

        public ObservableCollection<PatchItem> Patches { get; } = new();

        public PatchManager(string sourceFolder)
        {
            _sourceFolder = sourceFolder;
        }

        public void ScanPatches()
        {
            Patches.Clear();

            if (!Directory.Exists(_sourceFolder))
            {
                Directory.CreateDirectory(_sourceFolder);
                return;
            }

            var dllFiles = Directory.GetFiles(_sourceFolder, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dll in dllFiles)
            {
                var name = Path.GetFileNameWithoutExtension(dll);
                Patches.Add(new PatchItem
                {
                    PatchName = name,
                    SourcePath = dll,
                    IsEnabled = false
                });
            }
        }

        public bool ApplyPatches(string gameDir)
        {
            if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
                return false;

            string targetDir = Path.Combine(gameDir, _targetFolder);
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(targetDir))
                File.Delete(file);

            var enabled = Patches.Where(p => p.IsEnabled).ToList();
            foreach (var patch in enabled)
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(patch.SourcePath));
                File.Copy(patch.SourcePath, dest, true);
            }

            return true;
        }

        public void DisableAllPatches()
        {
            foreach (var p in Patches)
                p.IsEnabled = false;
        }
    }
}