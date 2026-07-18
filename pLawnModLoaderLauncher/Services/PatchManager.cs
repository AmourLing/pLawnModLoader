using pLawnModLoaderLauncher.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using pLawnModLoader_Shared;

namespace pLawnModLoaderLauncher.Services
{
    public class PatchManager
    {
        private readonly string _sourceFolder;
        private readonly string _targetFolder = Constants.ModLoaderFolder;

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

            foreach (string subDir in Directory.GetDirectories(_sourceFolder))
            {
                string patchName = Path.GetFileName(subDir);
                string dllPath = Path.Combine(subDir, patchName + ".dll");
                if (!File.Exists(dllPath))
                    continue;
                Patches.Add(new PatchItem
                {
                    PatchName = patchName,
                    SourcePath = subDir,
                    IsEnabled = false
                });
            }
        }

        public bool ApplyPatches(string gameDir)
        {
            if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
                return false;

            string targetRoot = Path.Combine(gameDir, Constants.ModLoaderFolder, Constants.ModsFolder);
            Directory.CreateDirectory(targetRoot);

            // 清空目标 mods 目录（避免残留）
            foreach (var dir in Directory.GetDirectories(targetRoot))
                Directory.Delete(dir, true);

            var enabled = Patches.Where(p => p.IsEnabled).ToList();
            foreach (var patch in enabled)
            {
                string targetModDir = Path.Combine(targetRoot, patch.PatchName);
                Directory.CreateDirectory(targetModDir);

                foreach (string file in Directory.GetFiles(patch.SourcePath))
                {
                    string dest = Path.Combine(targetModDir, Path.GetFileName(file));
                    File.Copy(file, dest, true);
                }
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