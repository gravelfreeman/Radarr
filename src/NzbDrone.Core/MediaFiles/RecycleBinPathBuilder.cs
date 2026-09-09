using System.IO;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.MediaFiles
{
    public static class RecycleBinPathBuilder
    {
        public const string RecycleBinFolder = ".bin";

        public static string GetRecycleBinPath(string mountPath)
        {
            if (mountPath.IsNullOrWhiteSpace())
            {
                return null;
            }

            return Path.Combine(Path.GetFullPath(mountPath), RecycleBinFolder);
        }

        public static string GetRecycleBinDestination(string path, string mountPath)
        {
            if (path.IsNullOrWhiteSpace() || mountPath.IsNullOrWhiteSpace())
            {
                return null;
            }

            var fullMountPath = Path.GetFullPath(mountPath);
            var relativePath = Path.GetRelativePath(fullMountPath, Path.GetFullPath(path));

            if (relativePath.IsNullOrWhiteSpace() || IsOutsideMount(relativePath))
            {
                return null;
            }

            var recyclingBin = GetRecycleBinPath(fullMountPath);

            if (relativePath == ".")
            {
                return recyclingBin;
            }

            return Path.Combine(recyclingBin, relativePath);
        }

        private static bool IsOutsideMount(string relativePath)
        {
            return Path.IsPathRooted(relativePath) ||
                   relativePath == ".." ||
                   relativePath.StartsWith(".." + Path.DirectorySeparatorChar) ||
                   relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar);
        }
    }
}
