using System.IO;

namespace EntryScriptGenerator.Editor
{
    public static class FileUtility
    {
        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }
            
            Directory.CreateDirectory(path);
            var dirInfo = new DirectoryInfo(path);
            while (!dirInfo.Exists)
            {
                dirInfo.Refresh();
            }
        }

        public static void DeleteDirectory(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
            var dirInfo = new DirectoryInfo(path);
            while (dirInfo.Exists)
            {
                dirInfo.Refresh();
            }
        }
    }
}