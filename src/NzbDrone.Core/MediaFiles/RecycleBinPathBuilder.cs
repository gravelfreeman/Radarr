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
            var fullPath = Path.GetFullPath(path);
            var isMountPath = fullMountPath.PathEquals(fullPath);

            if (!isMountPath && !fullMountPath.IsParentPath(fullPath))
            {
                return null;
            }

            var recyclingBin = GetRecycleBinPath(fullMountPath);

            if (isMountPath)
            {
                return recyclingBin;
            }

            return Path.Combine(recyclingBin, fullMountPath.GetRelativePath(fullPath));
        }
    }
}
