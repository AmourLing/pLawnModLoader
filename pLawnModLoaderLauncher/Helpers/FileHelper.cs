using System.IO;

namespace pLawnModLoaderLauncher.Helpers
{
    public static class FileHelper
    {
        public static void CopyDirectory(string sourceDir, string targetDir, bool overwrite = true)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, dest, overwrite);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir, overwrite);
            }
        }
    }
}