using System.IO;
using System.Security.Cryptography;

namespace Ryujinx.Actions
{
    public class ArtifactInformation
    {
        public OperatingSystem Os  { get; private set; }
        public Architecture Arch   { get; private set; }
        public BuildType BuildType { get; private set; }
        public string Url          { get; set; }
        public string FileHash     { get; private set; }
        public string FileName     { get; private set; }

        private static OperatingSystem GetOperatingSystemFromNamingPart(string part)
        {
            switch (part)
            {
                case "win":
                    return OperatingSystem.Windows;
                case "osx":
                    return OperatingSystem.MacOSX;
                case "linux":
                    return OperatingSystem.Linux;
                case "android":
                    return OperatingSystem.Android;
                case "ios":
                    return OperatingSystem.IOS;
                default:
                    return OperatingSystem.Unknown;
            }
        }

        private static Architecture GetArchitectureFromNamingPart(string part)
        {
            switch (part)
            {
                case "x64":
                    return Architecture.X64;
                case "aarch64":
                    return Architecture.AArch64;
                default:
                    return Architecture.Unknown;
            }
        }

        public static ArtifactInformation FromFileName(string fileName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string[] fileNameParts = fileNameWithoutExtension.Split("-");

            if (fileNameParts.Length != 3 && fileNameParts.Length != 4)
            {
                return null;
            }

            ArtifactInformation artifactInformation = new ArtifactInformation();

            artifactInformation.BuildType = BuildType.Release;

            bool isProfile = fileNameParts.Length == 4;

            if (isProfile)
            {
                artifactInformation.BuildType = BuildType.ProfileRelease;
            }

            string targetConfiguration        = isProfile ? fileNameParts[3] : fileNameParts[2];
            string[] targetConfigurationParts = targetConfiguration.Split("_");

            if (targetConfigurationParts.Length != 2)
            {
                return null;
            }

            artifactInformation.Os   = GetOperatingSystemFromNamingPart(targetConfigurationParts[0]);
            artifactInformation.Arch = GetArchitectureFromNamingPart(targetConfigurationParts[1]);

            return artifactInformation;
        }

        public static ArtifactInformation FromFile(string path)
        {
            ArtifactInformation artifactInformation = FromFileName(path);

            if (artifactInformation == null)
            {
                return null;
            }

            string fileHashString = "";

            using (SHA256 hasher = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    byte[] fileHash = hasher.ComputeHash(fileStream);
                    foreach (byte x in fileHash)
                    {
                        fileHashString += string.Format("{0:x2}", x);
                    }
                }
            }

            artifactInformation.FileName = Path.GetFileName(path);
            artifactInformation.FileHash = fileHashString;

            return artifactInformation;
        }
    }
}
