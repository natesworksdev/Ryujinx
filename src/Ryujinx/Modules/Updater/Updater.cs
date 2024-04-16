using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Ryujinx.Ava;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.UI.Common.Helper;
using Ryujinx.UI.Common.Models.Github;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Modules
{
    internal static class Updater
    {
        private const string GitHubApiUrl = "https://api.github.com";
        private static readonly GithubReleasesJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        private static readonly string _homeDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string _updateDir = Path.Combine(Path.GetTempPath(), "Ryujinx", "update");
        private static readonly string _updatePublishDir = Path.Combine(_updateDir, "publish");
        private const int ConnectionCount = 4;

        private static string _buildVer;
        private static string _platformExt;
        private static string _buildUrl;
        private static long _buildSize;
        private static bool _updateSuccessful;
        private static bool _running;

        private static readonly string[] _windowsDependencyDirs = Array.Empty<string>();

        private static readonly HttpClient httpClient = new HttpClient
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "Ryujinx-Updater/1.0.0" }
            }
        };

        public static async Task BeginParse(Window mainWindow, bool showVersionUpToDate)
        {
            if (_running)
            {
                return;
            }

            _running = true;

            DetectPlatform();

            Version currentVersion = GetCurrentVersion();
            if (currentVersion == null)
            {
                return;
            }

            string buildInfoUrl = $"{GitHubApiUrl}/repos/{ReleaseInformation.ReleaseChannelOwner}/{ReleaseInformation.ReleaseChannelRepo}/releases/latest";
            if (!await TryUpdateVersionInfo(buildInfoUrl, showVersionUpToDate))
            {
                return;
            }

            if (!await HandleVersionComparison(currentVersion, showVersionUpToDate))
            {
                return;
            }

            await FetchBuildSizeInfo();

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ShowUpdateDialogAndExecute(mainWindow);
            });
        }

        private static void DetectPlatform()
        {
            if (OperatingSystem.IsMacOS())
            {
                _platformExt = "macos_universal.app.tar.gz";
            }
            else if (OperatingSystem.IsWindows())
            {
                _platformExt = "win_x64.zip";
            }
            else if (OperatingSystem.IsLinux())
            {
                var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
                _platformExt = $"linux_{arch}.tar.gz";
            }
        }

        private static Version GetCurrentVersion()
        {
            try
            {
                return Version.Parse(Program.Version);
            }
            catch
            {
                Logger.Error?.Print(LogClass.Application, "Failed to convert the current Ryujinx version!");

                ContentDialogHelper.CreateWarningDialog(
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterConvertFailedMessage],
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterCancelUpdateMessage]);
                _running = false;
                return null;
            }
        }

        private static async Task<bool> TryUpdateVersionInfo(string buildInfoUrl, bool showVersionUpToDate)
        {
            try
            {
                HttpResponseMessage response = await SendAsyncWithHeaders(buildInfoUrl);
                string fetchedJson = await response.Content.ReadAsStringAsync();
                var fetched = JsonHelper.Deserialize(fetchedJson, _serializerContext.GithubReleasesJsonResponse);
                _buildVer = fetched.Name;

                foreach (var asset in fetched.Assets)
                {
                    if (asset.Name.StartsWith("ryujinx") && asset.Name.EndsWith(_platformExt) && asset.State == "uploaded")
                    {
                        _buildUrl = asset.BrowserDownloadUrl;
                        return true;
                    }
                }

                if (_buildUrl == null && showVersionUpToDate)
                {
                    await ContentDialogHelper.CreateUpdaterInfoDialog(
                        LocaleManager.Instance[LocaleKeys.DialogUpdaterAlreadyOnLatestVersionMessage], "");
                }

                _running = false;
                return false;
            }
            catch (Exception exception)
            {
                Logger.Error?.Print(LogClass.Application, exception.Message);
                await ContentDialogHelper.CreateErrorDialog(
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterFailedToGetVersionMessage]);
                _running = false;
                return false;
            }
        }

        private static async Task<bool> HandleVersionComparison(Version currentVersion, bool showVersionUpToDate)
        {
            try
            {
                Version newVersion = Version.Parse(_buildVer);
                if (newVersion <= currentVersion)
                {
                    if (showVersionUpToDate)
                    {
                        await ContentDialogHelper.CreateUpdaterInfoDialog(
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterAlreadyOnLatestVersionMessage], "");
                    }

                    _running = false;
                    return false;
                }

                return true;
            }
            catch
            {
                Logger.Error?.Print(LogClass.Application, "Failed to convert the received Ryujinx version from Github!");
                await ContentDialogHelper.CreateWarningDialog(
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterConvertFailedGithubMessage],
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterCancelUpdateMessage]);
                _running = false;
                return false;
            }
        }

        private static async Task FetchBuildSizeInfo()
        {
            try
            {
                HttpResponseMessage message = await SendAsyncWithHeaders(_buildUrl, new RangeHeaderValue(0, 0));
                _buildSize = message.Content.Headers.ContentRange.Length.Value;
            }
            catch (Exception ex)
            {
                Logger.Warning?.Print(LogClass.Application, ex.Message);
                Logger.Warning?.Print(LogClass.Application, "Couldn't determine build size for update, using single-threaded updater");
                _buildSize = -1;
            }
        }

        private static async Task ShowUpdateDialogAndExecute(Window mainWindow)
        {
            var shouldUpdate = await ContentDialogHelper.CreateChoiceDialog(
                LocaleManager.Instance[LocaleKeys.RyujinxUpdater],
                LocaleManager.Instance[LocaleKeys.RyujinxUpdaterMessage],
                $"{Program.Version} -> {_buildVer}");

            if (shouldUpdate)
            {
                await UpdateRyujinx(mainWindow, _buildUrl);
            }
            else
            {
                _running = false;
            }
        }

        private static async Task<HttpResponseMessage> SendAsyncWithHeaders(string url, RangeHeaderValue range = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (range != null)
            {
                request.Headers.Range = range;
            }
            return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }

        private static async Task UpdateRyujinx(Window parent, string downloadUrl)
        {
            _updateSuccessful = false;

            // Empty update dir, although it shouldn't ever have anything inside it
            if (Directory.Exists(_updateDir))
            {
                Directory.Delete(_updateDir, true);
            }

            Directory.CreateDirectory(_updateDir);

            string updateFile = Path.Combine(_updateDir, "update.bin");

            TaskDialog taskDialog = new()
            {
                Header = LocaleManager.Instance[LocaleKeys.RyujinxUpdater],
                SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterDownloading],
                IconSource = new SymbolIconSource { Symbol = Symbol.Download },
                ShowProgressBar = true,
                XamlRoot = parent,
            };

            taskDialog.Opened += async (s, e) =>
            {
                if (_buildSize >= 0)
                {
                    await DoUpdateWithMultipleThreads(taskDialog, downloadUrl, updateFile);
                }
                else
                {
                    DoUpdateWithSingleThread(taskDialog, downloadUrl, updateFile);
                }
            };

            await taskDialog.ShowAsync(true);

            if (_updateSuccessful)
            {
                bool shouldRestart = true;

                if (!OperatingSystem.IsMacOS())
                {
                    shouldRestart = await ContentDialogHelper.CreateChoiceDialog(
                        LocaleManager.Instance[LocaleKeys.RyujinxUpdater],
                        LocaleManager.Instance[LocaleKeys.DialogUpdaterCompleteMessage],
                        LocaleManager.Instance[LocaleKeys.DialogUpdaterRestartMessage]);
                }

                if (shouldRestart)
                {
                    RestartApplication(parent);
                }
            }
        }

        private static void RestartApplication(Window parent)
        {
            List<string> arguments = CommandLineState.Arguments.ToList();
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (OperatingSystem.IsMacOS())
            {
                string baseBundlePath = Path.GetFullPath(Path.Combine(executableDirectory, "..", ".."));
                string newBundlePath = Path.Combine(_updateDir, "Ryujinx.app");
                string updaterScriptPath = Path.Combine(newBundlePath, "Contents", "Resources", "updater.sh");
                string currentPid = Environment.ProcessId.ToString();

                arguments.InsertRange(0, new List<string> { updaterScriptPath, baseBundlePath, newBundlePath, currentPid });
                Process.Start("/bin/bash", arguments);
            }
            else
            {
                string ryuName = Path.GetFileName(Environment.ProcessPath) ?? (OperatingSystem.IsWindows() ? "Ryujinx.exe" : "Ryujinx");
                if (ryuName.StartsWith("Ryujinx.Ava"))
                {
                    ryuName = ryuName.Replace(".Ava", "");
                }

                if (ryuName.EndsWith(".ryuold"))
                {
                    ryuName = ryuName[..^7];
                }

                // Fallback if the executable could not be found.
                if (ryuName.Length == 0 || !Path.Exists(Path.Combine(executableDirectory, ryuName)))
                {
                    ryuName = OperatingSystem.IsWindows() ? "Ryujinx.exe" : "Ryujinx";
                }

                ProcessStartInfo processStart = new ProcessStartInfo(ryuName)
                {
                    UseShellExecute = true,
                    WorkingDirectory = executableDirectory,
                };

                foreach (string argument in arguments)
                {
                    processStart.ArgumentList.Add(argument);
                }

                Process.Start(processStart);
            }

            Environment.Exit(0);
        }

        private static async Task DoUpdateWithMultipleThreads(TaskDialog taskDialog, string downloadUrl, string updateFile)
        {
            long chunkSize = _buildSize / ConnectionCount;
            long remainderChunk = _buildSize % ConnectionCount;

            int completedRequests = 0;
            int[] progressPercentage = new int[ConnectionCount];
            List<byte[]> chunkDataList = new List<byte[]>(new byte[ConnectionCount][]);

            List<Task> downloadTasks = new List<Task>();

            for (int i = 0; i < ConnectionCount; i++)
            {
                long rangeStart = i * chunkSize;
                long rangeEnd = (i == ConnectionCount - 1) ? (rangeStart + chunkSize + remainderChunk - 1) : (rangeStart + chunkSize - 1);
                int index = i;

                downloadTasks.Add(Task.Run(async () =>
                {
                    byte[] chunkData = await DownloadFileChunk(downloadUrl, rangeStart, rangeEnd, index, taskDialog, progressPercentage);
                    chunkDataList[index] = chunkData;

                    Interlocked.Increment(ref completedRequests);
                    if (Interlocked.Equals(completedRequests, ConnectionCount))
                    {
                        byte[] allData = CombineChunks(chunkDataList, _buildSize);
                        File.WriteAllBytes(updateFile, allData);

                        // On macOS, ensure that we remove the quarantine bit to prevent Gatekeeper from blocking execution.
                        if (OperatingSystem.IsMacOS())
                        {
                            using Process xattrProcess = Process.Start("xattr", new List<string> { "-d", "com.apple.quarantine", updateFile });

                            xattrProcess.WaitForExit();
                        }

                        // Ensure that the install update is run on the UI thread.
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            try
                            {
                                await InstallUpdate(taskDialog, updateFile);
                            }
                            catch (Exception e)
                            {
                                Logger.Warning?.Print(LogClass.Application, e.Message);
                                Logger.Warning?.Print(LogClass.Application, "Multi-Threaded update failed, falling back to single-threaded updater.");
                                DoUpdateWithSingleThread(taskDialog, downloadUrl, updateFile);
                            }
                        });
                    }
                }));
            }

            await Task.WhenAll(downloadTasks);
        }

        private static byte[] CombineChunks(List<byte[]> chunks, long totalSize)
        {
            byte[] data = new byte[totalSize];
            long position = 0;
            foreach (byte[] chunk in chunks)
            {
                Buffer.BlockCopy(chunk, 0, data, (int)position, chunk.Length);
                position += chunk.Length;
            }
            return data;
        }

        private static async Task<byte[]> DownloadFileChunk(string url, long start, long end, int index, TaskDialog taskDialog, int[] progressPercentage)
        {
            byte[] buffer = new byte[8192];
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(start, end);
            HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var memoryStream = new MemoryStream();
            int bytesRead;
            long totalRead = 0;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
                int progress = (int)((totalRead * 100) / (end - start + 1));
                progressPercentage[index] = progress;

                Dispatcher.UIThread.Post(() =>
                {
                    taskDialog.SetProgressBarState(progressPercentage.Sum() / ConnectionCount, TaskDialogProgressState.Normal);
                });
            }

            return memoryStream.ToArray();
        }

        private static async Task DoUpdateWithSingleThreadWorker(TaskDialog taskDialog, string downloadUrl, string updateFile)
        {
            // We do not want to timeout while downloading
            httpClient.Timeout = TimeSpan.FromDays(1);

            // Use the existing httpClient instance, correctly configured
            HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to download file: {response.ReasonPhrase}");
            }

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            long byteWritten = 0;

            // Ensure the entire content body is read asynchronously
            using Stream remoteFileStream = await response.Content.ReadAsStreamAsync();
            using Stream updateFileStream = File.Open(updateFile, FileMode.Create);

            byte[] buffer = new byte[32 * 1024];
            int readSize;

            while ((readSize = await remoteFileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                updateFileStream.Write(buffer, 0, readSize);
                byteWritten += readSize;

                int progress = GetPercentage(byteWritten, totalBytes);
                Dispatcher.UIThread.Post(() =>
                {
                    taskDialog.SetProgressBarState(progress, TaskDialogProgressState.Normal);
                });
            }

            await InstallUpdate(taskDialog, updateFile);
        }

        private static int GetPercentage(long value, long total)
        {
            if (total == 0)
                return 0;
            return (int)((value * 100) / total);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double GetPercentage(double value, double max)
        {
            return max == 0 ? 0 : value / max * 100;
        }

        private static void DoUpdateWithSingleThread(TaskDialog taskDialog, string downloadUrl, string updateFile)
        {
            Task.Run(async () =>
            {
                await DoUpdateWithSingleThreadWorker(taskDialog, downloadUrl, updateFile);
            });
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private static void ExtractTarGzipFile(TaskDialog taskDialog, string archivePath, string outputDirectoryPath)
        {
            using Stream inStream = File.OpenRead(archivePath);
            using GZipInputStream gzipStream = new(inStream);
            using TarInputStream tarStream = new(gzipStream, Encoding.ASCII);

            TarEntry tarEntry;

            while ((tarEntry = tarStream.GetNextEntry()) is not null)
            {
                if (tarEntry.IsDirectory)
                {
                    continue;
                }

                string outPath = Path.Combine(outputDirectoryPath, tarEntry.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                using FileStream outStream = File.OpenWrite(outPath);
                tarStream.CopyEntryContents(outStream);

                File.SetUnixFileMode(outPath, (UnixFileMode)tarEntry.TarHeader.Mode);
                File.SetLastWriteTime(outPath, DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc));

                Dispatcher.UIThread.Post(() =>
                {
                    if (tarEntry is null)
                    {
                        return;
                    }

                    taskDialog.SetProgressBarState(GetPercentage(tarEntry.Size, inStream.Length), TaskDialogProgressState.Normal);
                });
            }
        }

        private static void ExtractZipFile(TaskDialog taskDialog, string archivePath, string outputDirectoryPath)
        {
            using Stream inStream = File.OpenRead(archivePath);
            using ZipFile zipFile = new(inStream);

            double count = 0;
            foreach (ZipEntry zipEntry in zipFile)
            {
                count++;
                if (zipEntry.IsDirectory)
                {
                    continue;
                }

                string outPath = Path.Combine(outputDirectoryPath, zipEntry.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                using Stream zipStream = zipFile.GetInputStream(zipEntry);
                using FileStream outStream = File.OpenWrite(outPath);

                zipStream.CopyTo(outStream);

                File.SetLastWriteTime(outPath, DateTime.SpecifyKind(zipEntry.DateTime, DateTimeKind.Utc));

                Dispatcher.UIThread.Post(() =>
                {
                    taskDialog.SetProgressBarState(GetPercentage(count, zipFile.Count), TaskDialogProgressState.Normal);
                });
            }
        }

        private static async Task InstallUpdate(TaskDialog taskDialog, string updateFile)
        {
            // Extract Update
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                taskDialog.SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterExtracting];
                taskDialog.SetProgressBarState(0, TaskDialogProgressState.Normal);
            });

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                ExtractTarGzipFile(taskDialog, updateFile, _updateDir);
            }
            else if (OperatingSystem.IsWindows())
            {
                ExtractZipFile(taskDialog, updateFile, _updateDir);
            }
            else
            {
                throw new NotSupportedException();
            }

            // Delete downloaded zip
            File.Delete(updateFile);

            List<string> allFiles = EnumerateFilesToDelete().ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                taskDialog.SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterRenaming];
                taskDialog.SetProgressBarState(0, TaskDialogProgressState.Normal);
                taskDialog.Hide();
            });

            // NOTE: On macOS, replacement is delayed to the restart phase.
            if (!OperatingSystem.IsMacOS())
            {
                // Replace old files
                double count = 0;
                foreach (string file in allFiles)
                {
                    count++;
                    try
                    {
                        File.Move(file, file + ".ryuold");

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            taskDialog.SetProgressBarState(GetPercentage(count, allFiles.Count), TaskDialogProgressState.Normal);
                        });
                    }
                    catch
                    {
                        Logger.Warning?.Print(LogClass.Application, LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.UpdaterRenameFailed, file));
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    taskDialog.SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterAddingFiles];
                    taskDialog.SetProgressBarState(0, TaskDialogProgressState.Normal);
                });

                MoveAllFilesOver(_updatePublishDir, _homeDir, taskDialog);

                Directory.Delete(_updateDir, true);
            }

            _updateSuccessful = true;

            taskDialog.Hide();
        }

        public static bool CanUpdate(bool showWarnings)
        {
#if !DISABLE_UPDATER
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                if (showWarnings)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                        ContentDialogHelper.CreateWarningDialog(
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterNoInternetMessage],
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterNoInternetSubMessage])
                    );
                }

                return false;
            }

            if (Program.Version.Contains("dirty") || !ReleaseInformation.IsValid)
            {
                if (showWarnings)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                        ContentDialogHelper.CreateWarningDialog(
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterDirtyBuildMessage],
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterDirtyBuildSubMessage])
                    );
                }

                return false;
            }

            return true;
#else
            if (showWarnings)
            {
                if (ReleaseInformation.IsFlatHubBuild)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                        ContentDialogHelper.CreateWarningDialog(
                            LocaleManager.Instance[LocaleKeys.UpdaterDisabledWarningTitle],
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterFlatpakNotSupportedMessage])
                    );
                }
                else
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                        ContentDialogHelper.CreateWarningDialog(
                            LocaleManager.Instance[LocaleKeys.UpdaterDisabledWarningTitle],
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterDirtyBuildSubMessage])
                    );
                }
            }

            return false;
#endif
        }

        // NOTE: This method should always reflect the latest build layout.
        private static IEnumerable<string> EnumerateFilesToDelete()
        {
            var files = Directory.EnumerateFiles(_homeDir); // All files directly in base dir.

            // Determine and exclude user files only when the updater is running, not when cleaning old files
            if (_running && !OperatingSystem.IsMacOS())
            {
                // Compare the loose files in base directory against the loose files from the incoming update, and store foreign ones in a user list.
                var oldFiles = Directory.EnumerateFiles(_homeDir, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
                var newFiles = Directory.EnumerateFiles(_updatePublishDir, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
                var userFiles = oldFiles.Except(newFiles).Select(filename => Path.Combine(_homeDir, filename));

                // Remove user files from the paths in files.
                files = files.Except(userFiles);
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (string dir in _windowsDependencyDirs)
                {
                    string dirPath = Path.Combine(_homeDir, dir);
                    if (Directory.Exists(dirPath))
                    {
                        files = files.Concat(Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories));
                    }
                }
            }

            return files.Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System));
        }

        private static void MoveAllFilesOver(string root, string dest, TaskDialog taskDialog)
        {
            int total = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length;
            foreach (string directory in Directory.GetDirectories(root))
            {
                string dirName = Path.GetFileName(directory);

                if (!Directory.Exists(Path.Combine(dest, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(dest, dirName));
                }

                MoveAllFilesOver(directory, Path.Combine(dest, dirName), taskDialog);
            }

            double count = 0;
            foreach (string file in Directory.GetFiles(root))
            {
                count++;

                File.Move(file, Path.Combine(dest, Path.GetFileName(file)), true);

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    taskDialog.SetProgressBarState(GetPercentage(count, total), TaskDialogProgressState.Normal);
                });
            }
        }

        public static void CleanupUpdate()
        {
            foreach (string file in Directory.GetFiles(_homeDir, "*.ryuold", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }

            // Migration: Delete old Ryujinx binary.
            // TODO: Remove this in a future update.
            if (!OperatingSystem.IsMacOS())
            {
                string[] oldRyuFiles = Directory.GetFiles(_homeDir, "Ryujinx.Ava*", SearchOption.TopDirectoryOnly);
                // Assume we are running the new one if the process path is not available.
                // This helps to prevent an infinite loop of restarts.
                string currentRyuName = Path.GetFileName(Environment.ProcessPath) ?? (OperatingSystem.IsWindows() ? "Ryujinx.exe" : "Ryujinx");

                string newRyuName = Path.Combine(_homeDir, currentRyuName.Replace(".Ava", ""));
                if (!currentRyuName.Contains("Ryujinx.Ava"))
                {
                    foreach (string oldRyuFile in oldRyuFiles)
                    {
                        File.Delete(oldRyuFile);
                    }
                }
                // Should we be running the old binary, start the new one if possible.
                else if (File.Exists(newRyuName))
                {
                    ProcessStartInfo processStart = new(newRyuName)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = _homeDir,
                    };

                    foreach (string argument in CommandLineState.Arguments)
                    {
                        processStart.ArgumentList.Add(argument);
                    }

                    Process.Start(processStart);

                    Environment.Exit(0);
                }
            }
        }
    }
}
