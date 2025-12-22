using System.Collections.Generic;

namespace Core_FileManagement
{
    public enum EntityFlags : int
    {
        None = 0,
        IsDrive = 1,
        IsDirectory = 2,
        IsFile = 4,
        IsHidden = 8,
        IsReadOnly = 16,
        IsSystem = 32,
        IsMyComputer = 64,
        IsUnavailable = 128
    }

    public static class EntityFlagsHelper
    {
        public static string DecodeFlags(EntityFlags flags)
        {
            var result = new List<string>();

            if ((flags & EntityFlags.IsDrive) != 0) result.Add("IsDrive");
            if ((flags & EntityFlags.IsDirectory) != 0) result.Add("IsDirectory");
            if ((flags & EntityFlags.IsFile) != 0) result.Add("IsFile");
            if ((flags & EntityFlags.IsHidden) != 0) result.Add("IsHidden");
            if ((flags & EntityFlags.IsReadOnly) != 0) result.Add("IsReadOnly");
            if ((flags & EntityFlags.IsSystem) != 0) result.Add("IsSystem");
            if ((flags & EntityFlags.IsMyComputer) != 0) result.Add("IsMyComputer");
            if ((flags & EntityFlags.IsUnavailable) != 0) result.Add("IsUnavailable");

            return string.Join(", ", result);
        }
    }
}