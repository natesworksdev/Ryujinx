using LibHac;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.Loaders.Npdm;
using Ryujinx.Ui.Common.Configuration;
using Ryujinx.Ui.Common.Configuration.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Path = System.IO.Path;

namespace Ryujinx.Ui.App.Common
{
    public class ApplicationLibrary
    {
        private static readonly double OneGib = 0.000000000931;

        public event EventHandler<ApplicationAddedEventArgs>        ApplicationAdded;
        public event EventHandler<ApplicationCountUpdatedEventArgs> ApplicationCountUpdated;

        private readonly Dictionary<string, ApplicationMetadata> _metadataCache;

        private readonly byte[] _nspIcon;
        private readonly byte[] _xciIcon;
        private readonly byte[] _ncaIcon;
        private readonly byte[] _nroIcon;
        private readonly byte[] _nsoIcon;

        private readonly VirtualFileSystem _virtualFileSystem;
        private Language                   _desiredTitleLanguage;
        private CancellationTokenSource    _cancellationToken;

        private static readonly ApplicationJsonSerializerContext SerializerContext = new(JsonHelper.GetDefaultSerializerOptions());
        private static readonly TitleUpdateMetadataJsonSerializerContext TitleSerializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public ApplicationLibrary(VirtualFileSystem virtualFileSystem)
        {
            _virtualFileSystem = virtualFileSystem;

            _metadataCache = new();

            _nspIcon = GetResourceBytes("Ryujinx.Ui.Common.Resources.Icon_NSP.png");
            _xciIcon = GetResourceBytes("Ryujinx.Ui.Common.Resources.Icon_XCI.png");
            _ncaIcon = GetResourceBytes("Ryujinx.Ui.Common.Resources.Icon_NCA.png");
            _nroIcon = GetResourceBytes("Ryujinx.Ui.Common.Resources.Icon_NRO.png");
            _nsoIcon = GetResourceBytes("Ryujinx.Ui.Common.Resources.Icon_NSO.png");
        }

        private static byte[] GetResourceBytes(string resourceName)
        {
            Stream resourceStream    = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
            byte[] resourceByteArray = new byte[resourceStream.Length];

            resourceStream.Read(resourceByteArray);

            return resourceByteArray;
        }

        public void CancelLoading()
        {
            _cancellationToken?.Cancel();
        }

        public static void ReadControlData(IFileSystem controlFs, Span<byte> outProperty)
        {
            using UniqueRef<IFile> controlFile = new();

            controlFs.OpenFile(ref controlFile.Ref, "/control.nacp".ToU8Span(), OpenMode.Read).ThrowIfFailure();
            controlFile.Get.Read(out _, 0, outProperty, ReadOption.None).ThrowIfFailure();
        }

        public void LoadApplications(List<string> appDirs, Language desiredTitleLanguage, bool readFromDisk)
        {
            _desiredTitleLanguage = desiredTitleLanguage;

            _cancellationToken = new CancellationTokenSource();
            
            int numApplicationsFound = 0;

            try
            {
                if (!readFromDisk && TryLoadApplicationsFromGamesCache())
                {
                    return;
                }

                _metadataCache.Clear();

                Logger.Debug?.Print(LogClass.Application, "Loading applications from disk");

                // Builds the applications list with fileinfo descriptors of found applications
                IEnumerable<FileInfo> applications = appDirs.SelectMany(appDir => FindApplications(appDir, ref numApplicationsFound));

                if (_cancellationToken.Token.IsCancellationRequested)
                {
                    return;
                }

                int numApplicationsLoaded = 0;

                // Loops through applications list, creating a struct and then firing an event containing the struct for each application
                foreach (FileInfo fileInfo in applications)
                {
                    bool isValid = VerifyAndLoadApplicationFileFromDisk(fileInfo, out ApplicationData appData);

                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    numApplicationsFound -= isValid ? 0 : 1;
                    numApplicationsLoaded += isValid ? 1 : 0;

                    OnApplicationCountUpdated(new ApplicationCountUpdatedEventArgs()
                    {
                        NumAppsFound = numApplicationsFound,
                        NumAppsLoaded = numApplicationsLoaded
                    });

                    if (isValid)
                    {
                        OnApplicationAdded(new ApplicationAddedEventArgs()
                        {
                            AppData = appData
                        });
                    }
                }

                OnApplicationCountUpdated(new ApplicationCountUpdatedEventArgs()
                {
                    NumAppsFound = numApplicationsFound,
                    NumAppsLoaded = numApplicationsLoaded
                });
            }
            finally
            {
                _cancellationToken.Dispose();
                _cancellationToken = null;
            }
        }

        private IEnumerable<FileInfo> FindApplications(string appDir, ref int numApplicationsFound)
        {
            if (_cancellationToken.Token.IsCancellationRequested)
            {
                return Enumerable.Empty<FileInfo>();
            }

            if (!Directory.Exists(appDir))
            {
                Logger.Warning?.Print(LogClass.Application, $"The \"game_dirs\" section in \"Config.json\" contains an invalid directory: \"{appDir}\"");

                return Enumerable.Empty<FileInfo>();
            }

            try
            {
                List<FileInfo> result = new();

                foreach (string file in Directory.EnumerateFiles(appDir, "*", SearchOption.AllDirectories))
                {
                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return result;
                    }

                    var fileInfo = new FileInfo(file);

                    string ext = fileInfo.Extension.ToLower();

                    bool isApplication =
                    !fileInfo.Attributes.HasFlag(FileAttributes.Hidden) &&
                    (ext is ".nsp" && ConfigurationState.Instance.Ui.ShownFileTypes.NSP.Value) ||
                    (ext is ".pfs0" && ConfigurationState.Instance.Ui.ShownFileTypes.PFS0.Value) ||
                    (ext is ".xci" && ConfigurationState.Instance.Ui.ShownFileTypes.XCI.Value) ||
                    (ext is ".nca" && ConfigurationState.Instance.Ui.ShownFileTypes.NCA.Value) ||
                    (ext is ".nro" && ConfigurationState.Instance.Ui.ShownFileTypes.NRO.Value) ||
                    (ext is ".nso" && ConfigurationState.Instance.Ui.ShownFileTypes.NSO.Value);

                    if (isApplication)
                    {
                        result.Add(fileInfo);

                        OnApplicationCountUpdated(new ApplicationCountUpdatedEventArgs()
                        {
                            NumAppsFound = ++numApplicationsFound,
                            NumAppsLoaded = 0
                        });
                    }
                }

                return result;
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Warning?.Print(LogClass.Application, $"Failed to get access to directory: \"{appDir}\"");
            }

            return Enumerable.Empty<FileInfo>();
        }

        private bool TryLoadApplicationsFromGamesCache()
        {
            List<string> existingApplications = Directory.EnumerateDirectories(AppDataManager.GamesDirPath)
                .Select(Path.GetFileName)
                .ToList();

            int cachedAppsFound = existingApplications.Count;

            OnApplicationCountUpdated(new ApplicationCountUpdatedEventArgs()
            {
                NumAppsFound = cachedAppsFound,
                NumAppsLoaded = 0
            });

            int loaded = 0;

            foreach (string titleId in existingApplications)
            {
                ApplicationMetadata appMetadata = LoadAndSaveMetaData(titleId);

                if (string.IsNullOrWhiteSpace(appMetadata.TitleId)) 
                {
                    Logger.Warning?.Print(LogClass.Application, $"Outdated cached metadata file found for: {titleId}");

                    continue;
                }

                ApplicationData data = new()
                {
                    Favorite = appMetadata.Favorite,
                    Icon = appMetadata.AppIcon,
                    TitleName = appMetadata.Title,
                    TitleId = titleId,
                    Developer = appMetadata.Developer,
                    Version = appMetadata.Version,
                    TimePlayed = ConvertSecondsToFormattedString(appMetadata.TimePlayed),
                    TimePlayedNum = appMetadata.TimePlayed,
                    LastPlayed = appMetadata.LastPlayed,
                    FileExtension = appMetadata.FileType,
                    FileSize = (appMetadata.FileSize < 1) ? (appMetadata.FileSize * 1024).ToString("0.##") + " MiB" : appMetadata.FileSize.ToString("0.##") + " GiB",
                    FileSizeBytes = appMetadata.FileSize,
                    Path = appMetadata.HostPath,

                    // TODO: must init on gamelaunch or force user to reload from disk?
                    // user must force a reload to get this struct used for save/device directory
                    //ControlHolder = controlHolder
                };

                OnApplicationAdded(new ApplicationAddedEventArgs()
                {
                    AppData = data
                });

                OnApplicationCountUpdated(new ApplicationCountUpdatedEventArgs()
                {
                    NumAppsFound = cachedAppsFound,
                    NumAppsLoaded = ++loaded
                });
            }

            return loaded > 0;
        }

        private bool VerifyAndLoadApplicationFileFromDisk(FileInfo fileInfo, out ApplicationData appData)
        {
            appData = null!;

            if (_cancellationToken.Token.IsCancellationRequested)
            {
                return false;
            }

            string applicationPath = fileInfo.ResolveLinkTarget(true)?.FullName ?? fileInfo.FullName;

            double fileSize = fileInfo.Length * OneGib;
            string titleName = Path.GetFileNameWithoutExtension(applicationPath);
            string titleId = "0000000000000000";
            string developer = "Unknown";
            string version = "0";
            byte[] applicationIcon = null;

            BlitStruct<ApplicationControlProperty> controlHolder = new(1);

            try
            {
                string extension = fileInfo.Extension.ToLower();

                using FileStream file = new(applicationPath, FileMode.Open, FileAccess.Read);

                if (extension is ".nsp" or ".pfs0" or ".xci")
                {
                    try
                    {
                        PartitionFileSystem pfs;

                        bool isExeFs = false;

                        if (extension is ".xci")
                        {
                            Xci xci = new(_virtualFileSystem.KeySet, file.AsStorage());

                            pfs = xci.OpenPartition(XciPartitionType.Secure);
                        }
                        else
                        {
                            pfs = new PartitionFileSystem(file.AsStorage());

                            // If the NSP doesn't have a main NCA, decrement the number of applications found and then continue to the next application.
                            bool hasMainNca = false;

                            foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*"))
                            {
                                if (Path.GetExtension(fileEntry.FullPath).ToLower() == ".nca")
                                {
                                    using UniqueRef<IFile> ncaFile = new();

                                    pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                    Nca nca = new(_virtualFileSystem.KeySet, ncaFile.Get.AsStorage());
                                    int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

                                    // Some main NCAs don't have a data partition, so check if the partition exists before opening it
                                    if (nca.Header.ContentType == NcaContentType.Program && !(nca.SectionExists(NcaSectionType.Data) && nca.Header.GetFsHeader(dataIndex).IsPatchSection()))
                                    {
                                        hasMainNca = true;

                                        break;
                                    }
                                }
                                else if (Path.GetFileNameWithoutExtension(fileEntry.FullPath) == "main")
                                {
                                    isExeFs = true;
                                }
                            }

                            if (!hasMainNca && !isExeFs)
                            {
                                return false;
                            }
                        }

                        if (isExeFs)
                        {
                            applicationIcon = _nspIcon;

                            using UniqueRef<IFile> npdmFile = new();

                            Result result = pfs.OpenFile(ref npdmFile.Ref, "/main.npdm".ToU8Span(), OpenMode.Read);

                            if (ResultFs.PathNotFound.Includes(result))
                            {
                                Npdm npdm = new(npdmFile.Get.AsStream());

                                titleName = npdm.TitleName;
                                titleId = npdm.Aci0.TitleId.ToString("x16");
                            }
                        }
                        else
                        {
                            GetControlFsAndTitleId(pfs, out IFileSystem controlFs, out titleId);

                            // Check if there is an update available.
                            if (IsUpdateApplied(titleId, out IFileSystem updatedControlFs))
                            {
                                // Replace the original ControlFs by the updated one.
                                controlFs = updatedControlFs;
                            }

                            ReadControlData(controlFs, controlHolder.ByteSpan);

                            GetGameInformation(ref controlHolder.Value, out titleName, out _, out developer, out version);

                            // Read the icon from the ControlFS and store it as a byte array
                            try
                            {
                                using UniqueRef<IFile> icon = new();

                                controlFs.OpenFile(ref icon.Ref, $"/icon_{_desiredTitleLanguage}.dat".ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                using MemoryStream stream = new();

                                icon.Get.AsStream().CopyTo(stream);
                                applicationIcon = stream.ToArray();
                            }
                            catch (HorizonResultException)
                            {
                                foreach (DirectoryEntryEx entry in controlFs.EnumerateEntries("/", "*"))
                                {
                                    if (entry.Name == "control.nacp")
                                    {
                                        continue;
                                    }

                                    using var icon = new UniqueRef<IFile>();

                                    controlFs.OpenFile(ref icon.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                    using MemoryStream stream = new();

                                    icon.Get.AsStream().CopyTo(stream);
                                    applicationIcon = stream.ToArray();

                                    if (applicationIcon != null)
                                    {
                                        break;
                                    }
                                }

                                applicationIcon ??= extension == ".xci" ? _xciIcon : _nspIcon;
                            }
                        }
                    }
                    catch (MissingKeyException exception)
                    {
                        applicationIcon = extension == ".xci" ? _xciIcon : _nspIcon;

                        Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}");
                    }
                    catch (InvalidDataException)
                    {
                        applicationIcon = extension == ".xci" ? _xciIcon : _nspIcon;

                        Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Errored File: {applicationPath}");
                    }
                    catch (Exception exception)
                    {
                        Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. File: '{applicationPath}' Error: {exception}");

                        return false;
                    }
                }
                else if (extension is ".nro")
                {
                    BinaryReader reader = new(file);

                    byte[] Read(long position, int size)
                    {
                        file.Seek(position, SeekOrigin.Begin);

                        return reader.ReadBytes(size);
                    }

                    try
                    {
                        file.Seek(24, SeekOrigin.Begin);

                        int assetOffset = reader.ReadInt32();

                        if (Encoding.ASCII.GetString(Read(assetOffset, 4)) == "ASET")
                        {
                            byte[] iconSectionInfo = Read(assetOffset + 8, 0x10);

                            long iconOffset = BitConverter.ToInt64(iconSectionInfo, 0);
                            long iconSize = BitConverter.ToInt64(iconSectionInfo, 8);

                            ulong nacpOffset = reader.ReadUInt64();
                            ulong nacpSize = reader.ReadUInt64();

                            // Reads and stores game icon as byte array
                            applicationIcon = Read(assetOffset + iconOffset, (int)iconSize);

                            // Read the NACP data
                            Read(assetOffset + (int)nacpOffset, (int)nacpSize).AsSpan().CopyTo(controlHolder.ByteSpan);

                            GetGameInformation(ref controlHolder.Value, out titleName, out titleId, out developer, out version);
                        }
                        else
                        {
                            applicationIcon = _nroIcon;
                        }
                    }
                    catch
                    {
                        Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. Errored File: {applicationPath}");

                        return false;
                    }
                }
                else if (extension is ".nca")
                {
                    try
                    {
                        Nca nca = new(_virtualFileSystem.KeySet, file.AsStorage());
                        int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

                        if (nca.Header.ContentType != NcaContentType.Program || (nca.SectionExists(NcaSectionType.Data) && nca.Header.GetFsHeader(dataIndex).IsPatchSection()))
                        {
                            return false;
                        }
                    }
                    catch (InvalidDataException)
                    {
                        Logger.Warning?.Print(LogClass.Application, $"The NCA header content type check has failed. This is usually because the header key is incorrect or missing. Errored File: {applicationPath}");
                    }
                    catch
                    {
                        Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. Errored File: {applicationPath}");

                        return false;
                    }

                    applicationIcon = _ncaIcon;
                }
                // If its an NSO we just set defaults
                else if (extension is ".nso")
                {
                    applicationIcon = _nsoIcon;
                }
            }
            catch (IOException exception)
            {
                Logger.Warning?.Print(LogClass.Application, exception.Message);

                return false;
            }

            string formattedFileSize = (fileSize < 1) ? (fileSize * 1024).ToString("0.##") + " MiB" : fileSize.ToString("0.##") + " GiB";

            ApplicationMetadata appMetadata = LoadAndSaveMetaData(titleId, appMetadata =>
            {
                appMetadata.Title = titleName;
                appMetadata.Version = version;
                appMetadata.Developer = developer;
                appMetadata.TitleId = titleId;
                appMetadata.AppIcon = applicationIcon; // TODO: scale down?
                appMetadata.FileType = fileInfo.Extension.ToUpperInvariant()[1..];
                appMetadata.HostPath = applicationPath;
                appMetadata.FileSize = fileSize;

                if (appMetadata.LastPlayedOld == default || appMetadata.LastPlayed.HasValue)
                {
                    // Don't do the migration if last_played doesn't exist or last_played_utc already has a value.
                    return;
                }

                // Migrate from string-based last_played to DateTime-based last_played_utc.
                if (DateTime.TryParse(appMetadata.LastPlayedOld, out DateTime lastPlayedOldParsed))
                {
                    Logger.Info?.Print(LogClass.Application, $"last_played found: \"{appMetadata.LastPlayedOld}\", migrating to last_played_utc");
                    appMetadata.LastPlayed = lastPlayedOldParsed;

                    // Migration successful: deleting last_played from the metadata file.
                    appMetadata.LastPlayedOld = default;
                }
                else
                {
                    // Migration failed: emitting warning but leaving the unparsable value in the metadata file so the user can fix it.
                    Logger.Warning?.Print(LogClass.Application, $"Last played string \"{appMetadata.LastPlayedOld}\" is invalid for current system culture, skipping (did current culture change?)");
                }
            });

            appData = new()
            {
                Favorite = appMetadata.Favorite,
                Icon = applicationIcon,
                TitleName = titleName,
                TitleId = titleId,
                Developer = developer,
                Version = version,
                TimePlayed = ConvertSecondsToFormattedString(appMetadata.TimePlayed),
                TimePlayedNum = appMetadata.TimePlayed,
                LastPlayed = appMetadata.LastPlayed,
                FileExtension = fileInfo.Extension.ToUpperInvariant().Remove(0, 1),
                FileSize = formattedFileSize,
                FileSizeBytes = fileSize,
                Path = applicationPath,
                ControlHolder = controlHolder
            };

            return true;
        }

        protected void OnApplicationAdded(ApplicationAddedEventArgs e)
        {
            ApplicationAdded?.Invoke(null, e);
        }

        protected void OnApplicationCountUpdated(ApplicationCountUpdatedEventArgs e)
        {
            ApplicationCountUpdated?.Invoke(null, e);
        }

        private void GetControlFsAndTitleId(PartitionFileSystem pfs, out IFileSystem controlFs, out string titleId)
        {
            (_, _, Nca controlNca) = GetGameData(_virtualFileSystem, pfs, 0);

            // Return the ControlFS
            controlFs = controlNca?.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);
            titleId   = controlNca?.Header.TitleId.ToString("x16");
        }

        public ApplicationMetadata LoadAndSaveMetaData(string titleId, Action<ApplicationMetadata> modifyFunction = null)
        {
            string metadataFolder = Path.Combine(AppDataManager.GamesDirPath, titleId, "gui");
            string metadataFile   = Path.Combine(metadataFolder, "metadata.json");

            bool isCached = _metadataCache.TryGetValue(metadataFile, out ApplicationMetadata appMetadata);

            if (!isCached)
            {
                try
                {
                    appMetadata = JsonHelper.DeserializeFromFile(metadataFile, SerializerContext.ApplicationMetadata);
                }
                catch (Exception ex) when (ex is JsonException || ex is DirectoryNotFoundException)
                {
                    if (ex is DirectoryNotFoundException)
                    {
                        Directory.CreateDirectory(metadataFolder);
                    }

                    Logger.Warning?.Print(LogClass.Application, $"Failed to parse metadata json for {titleId}. Loading defaults.");
                    
                    appMetadata = new ApplicationMetadata();
                }

                _metadataCache.Add(metadataFile, appMetadata);
            }

            if (modifyFunction != null)
            {
                modifyFunction(appMetadata);

                JsonHelper.SerializeToFile(metadataFile, appMetadata, SerializerContext.ApplicationMetadata);
            }

            return appMetadata;
        }

        public byte[] GetApplicationIcon(string applicationPath)
        {
            byte[] applicationIcon = null;

            try
            {
                // Look for icon only if applicationPath is not a directory
                if (!Directory.Exists(applicationPath))
                {
                    string extension = Path.GetExtension(applicationPath).ToLower();

                    using FileStream file = new(applicationPath, FileMode.Open, FileAccess.Read);

                    if (extension == ".nsp" || extension == ".pfs0" || extension == ".xci")
                    {
                        try
                        {
                            PartitionFileSystem pfs;

                            bool isExeFs = false;

                            if (extension == ".xci")
                            {
                                Xci xci = new(_virtualFileSystem.KeySet, file.AsStorage());

                                pfs = xci.OpenPartition(XciPartitionType.Secure);
                            }
                            else
                            {
                                pfs = new PartitionFileSystem(file.AsStorage());

                                foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*"))
                                {
                                    if (Path.GetFileNameWithoutExtension(fileEntry.FullPath) == "main")
                                    {
                                        isExeFs = true;
                                    }
                                }
                            }

                            if (isExeFs)
                            {
                                applicationIcon = _nspIcon;
                            }
                            else
                            {
                                // Store the ControlFS in variable called controlFs
                                GetControlFsAndTitleId(pfs, out IFileSystem controlFs, out _);

                                // Read the icon from the ControlFS and store it as a byte array
                                try
                                {
                                    using var icon = new UniqueRef<IFile>();

                                    controlFs.OpenFile(ref icon.Ref, $"/icon_{_desiredTitleLanguage}.dat".ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                    using MemoryStream stream = new();

                                    icon.Get.AsStream().CopyTo(stream);
                                    applicationIcon = stream.ToArray();
                                }
                                catch (HorizonResultException)
                                {
                                    foreach (DirectoryEntryEx entry in controlFs.EnumerateEntries("/", "*"))
                                    {
                                        if (entry.Name == "control.nacp")
                                        {
                                            continue;
                                        }

                                        using var icon = new UniqueRef<IFile>();

                                        controlFs.OpenFile(ref icon.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                        using (MemoryStream stream = new())
                                        {
                                            icon.Get.AsStream().CopyTo(stream);
                                            applicationIcon = stream.ToArray();
                                        }

                                        if (applicationIcon != null)
                                        {
                                            break;
                                        }
                                    }

                                    applicationIcon ??= extension == ".xci" ? _xciIcon : _nspIcon;
                                }
                            }
                        }
                        catch (MissingKeyException)
                        {
                            applicationIcon = extension == ".xci" ? _xciIcon : _nspIcon;
                        }
                        catch (InvalidDataException)
                        {
                            applicationIcon = extension == ".xci" ? _xciIcon : _nspIcon;
                        }
                        catch (Exception exception)
                        {
                            Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. File: '{applicationPath}' Error: {exception}");
                        }
                    }
                    else if (extension == ".nro")
                    {
                        BinaryReader reader = new(file);

                        byte[] Read(long position, int size)
                        {
                            file.Seek(position, SeekOrigin.Begin);

                            return reader.ReadBytes(size);
                        }

                        try
                        {
                            file.Seek(24, SeekOrigin.Begin);

                            int assetOffset = reader.ReadInt32();

                            if (Encoding.ASCII.GetString(Read(assetOffset, 4)) == "ASET")
                            {
                                byte[] iconSectionInfo = Read(assetOffset + 8, 0x10);

                                long iconOffset = BitConverter.ToInt64(iconSectionInfo, 0);
                                long iconSize = BitConverter.ToInt64(iconSectionInfo, 8);

                                // Reads and stores game icon as byte array
                                applicationIcon = Read(assetOffset + iconOffset, (int)iconSize);
                            }
                            else
                            {
                                applicationIcon = _nroIcon;
                            }
                        }
                        catch
                        {
                            Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. Errored File: {applicationPath}");
                        }
                    }
                    else if (extension == ".nca")
                    {
                        applicationIcon = _ncaIcon;
                    }
                    // If its an NSO we just set defaults
                    else if (extension == ".nso")
                    {
                        applicationIcon = _nsoIcon;
                    }
                }
            }
            catch(Exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"Could not retrieve a valid icon for the app. Default icon will be used. Errored File: {applicationPath}");
            }

            return applicationIcon ?? _ncaIcon;
        }

        private static string ConvertSecondsToFormattedString(double seconds)
        {
            System.TimeSpan time = System.TimeSpan.FromSeconds(seconds);

            string timeString;
            if (time.Days != 0)
            {
                timeString = $"{time.Days}d {time.Hours:D2}h {time.Minutes:D2}m";
            }
            else if (time.Hours != 0)
            {
                timeString = $"{time.Hours:D2}h {time.Minutes:D2}m";
            }
            else if (time.Minutes != 0)
            {
                timeString = $"{time.Minutes:D2}m";
            }
            else
            {
                timeString = "Never";
            }

            return timeString;
        }

        private void GetGameInformation(ref ApplicationControlProperty controlData, out string titleName, out string titleId, out string publisher, out string version)
        {
            _ = Enum.TryParse(_desiredTitleLanguage.ToString(), out TitleLanguage desiredTitleLanguage);

            if (controlData.Title.ItemsRo.Length > (int)desiredTitleLanguage)
            {
                titleName = controlData.Title[(int)desiredTitleLanguage].NameString.ToString();
                publisher = controlData.Title[(int)desiredTitleLanguage].PublisherString.ToString();
            }
            else
            {
                titleName = null;
                publisher = null;
            }

            if (string.IsNullOrWhiteSpace(titleName))
            {
                foreach (ref readonly var controlTitle in controlData.Title.ItemsRo)
                {
                    if (!controlTitle.NameString.IsEmpty())
                    {
                        titleName = controlTitle.NameString.ToString();

                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(publisher))
            {
                foreach (ref readonly var controlTitle in controlData.Title.ItemsRo)
                {
                    if (!controlTitle.PublisherString.IsEmpty())
                    {
                        publisher = controlTitle.PublisherString.ToString();

                        break;
                    }
                }
            }

            if (controlData.PresenceGroupId != 0)
            {
                titleId = controlData.PresenceGroupId.ToString("x16");
            }
            else if (controlData.SaveDataOwnerId != 0)
            {
                titleId = controlData.SaveDataOwnerId.ToString();
            }
            else if (controlData.AddOnContentBaseId != 0)
            {
                titleId = (controlData.AddOnContentBaseId - 0x1000).ToString("x16");
            }
            else
            {
                titleId = "0000000000000000";
            }

            version = controlData.DisplayVersionString.ToString();
        }

        private bool IsUpdateApplied(string titleId, out IFileSystem updatedControlFs)
        {
            updatedControlFs = null;

            string updatePath = "(unknown)";

            try
            {
                (Nca patchNca, Nca controlNca) = GetGameUpdateData(_virtualFileSystem, titleId, 0, out updatePath);

                if (patchNca != null && controlNca != null)
                {
                    updatedControlFs = controlNca?.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);

                    return true;
                }
            }
            catch (InvalidDataException)
            {
                Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Errored File: {updatePath}");
            }
            catch (MissingKeyException exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}. Errored File: {updatePath}");
            }

            return false;
        }

        public static (Nca main, Nca patch, Nca control) GetGameData(VirtualFileSystem fileSystem, PartitionFileSystem pfs, int programIndex)
        {
            Nca mainNca = null;
            Nca patchNca = null;
            Nca controlNca = null;

            fileSystem.ImportTickets(pfs);

            foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
            {
                using var ncaFile = new UniqueRef<IFile>();

                pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                Nca nca = new Nca(fileSystem.KeySet, ncaFile.Release().AsStorage());

                int ncaProgramIndex = (int)(nca.Header.TitleId & 0xF);

                if (ncaProgramIndex != programIndex)
                {
                    continue;
                }

                if (nca.Header.ContentType == NcaContentType.Program)
                {
                    int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

                    if (nca.SectionExists(NcaSectionType.Data) && nca.Header.GetFsHeader(dataIndex).IsPatchSection())
                    {
                        patchNca = nca;
                    }
                    else
                    {
                        mainNca = nca;
                    }
                }
                else if (nca.Header.ContentType == NcaContentType.Control)
                {
                    controlNca = nca;
                }
            }

            return (mainNca, patchNca, controlNca);
        }

        public static (Nca patch, Nca control) GetGameUpdateDataFromPartition(VirtualFileSystem fileSystem, PartitionFileSystem pfs, string titleId, int programIndex)
        {
            Nca patchNca = null;
            Nca controlNca = null;

            fileSystem.ImportTickets(pfs);

            foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
            {
                using var ncaFile = new UniqueRef<IFile>();

                pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                Nca nca = new Nca(fileSystem.KeySet, ncaFile.Release().AsStorage());

                int ncaProgramIndex = (int)(nca.Header.TitleId & 0xF);

                if (ncaProgramIndex != programIndex)
                {
                    continue;
                }

                if ($"{nca.Header.TitleId.ToString("x16")[..^3]}000" != titleId)
                {
                    break;
                }

                if (nca.Header.ContentType == NcaContentType.Program)
                {
                    patchNca = nca;
                }
                else if (nca.Header.ContentType == NcaContentType.Control)
                {
                    controlNca = nca;
                }
            }

            return (patchNca, controlNca);
        }

        public static (Nca patch, Nca control) GetGameUpdateData(VirtualFileSystem fileSystem, string titleId, int programIndex, out string updatePath)
        {
            updatePath = null;

            if (ulong.TryParse(titleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong titleIdBase))
            {
                // Clear the program index part.
                titleIdBase &= ~0xFUL;

                // Load update information if exists.
                string titleUpdateMetadataPath = Path.Combine(AppDataManager.GamesDirPath, titleIdBase.ToString("x16"), "updates.json");

                if (File.Exists(titleUpdateMetadataPath))
                {
                    updatePath = JsonHelper.DeserializeFromFile(titleUpdateMetadataPath, TitleSerializerContext.TitleUpdateMetadata).Selected;

                    if (File.Exists(updatePath))
                    {
                        FileStream file = new FileStream(updatePath, FileMode.Open, FileAccess.Read);
                        PartitionFileSystem nsp = new PartitionFileSystem(file.AsStorage());

                        return GetGameUpdateDataFromPartition(fileSystem, nsp, titleIdBase.ToString("x16"), programIndex);
                    }
                }
            }

            return (null, null);
        }
    }
}

