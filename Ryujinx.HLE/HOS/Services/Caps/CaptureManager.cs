using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Caps.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Ryujinx.HLE.HOS.Services.Caps
{
    class CaptureManager
    {
        private string _sdCardPath;

        private uint _shimLibraryVersion;

        public CaptureManager(Switch device)
        {
            _sdCardPath = device.FileSystem.GetSdCardPath();

            SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.SetEncoder(JpegFormat.Instance, new JpegEncoder()
            {
                Quality = 100
            });
        }

        public ResultCode SetShimLibraryVersion(ServiceCtx context)
        {
            ulong shimLibraryVersion   = context.RequestData.ReadUInt64();
            ulong appletResourceUserId = context.RequestData.ReadUInt64();

            // TODO: Service checks if the pid is present in an internal list and returns ResultCode.BlacklistedPid if it is.
            //       The list content needs to be determined.

            ResultCode resultCode = ResultCode.OutOfRange;

            if (shimLibraryVersion != 0)
            {
                if (_shimLibraryVersion == shimLibraryVersion)
                {
                    resultCode = ResultCode.Success;
                }
                else if (_shimLibraryVersion != 0)
                {
                    resultCode = ResultCode.ShimLibraryVersionAlreadySet;
                }
                else if (shimLibraryVersion == 1)
                {
                    resultCode = ResultCode.Success;

                    _shimLibraryVersion = 1;
                }
            }

            return resultCode;
        }

        public ResultCode SaveScreenShot(byte[] screenshotData, ulong appletResourceUserId, ulong titleId, out ApplicationAlbumEntry applicationAlbumEntry)
        {
            applicationAlbumEntry = default;

            if (screenshotData.Length == 0)
            {
                return ResultCode.NullInputBuffer;
            }

            /*
            // NOTE: Our current implementation of appletResourceUserId starts at 0, disabled it for now.
            if (appletResourceUserId == 0)
            {
                return ResultCode.InvalidArgument;
            }
            */

            /*
            // Doesn't occur in our case.
            if (applicationAlbumEntry == null)
            {
                return ResultCode.NullOutputBuffer;
            }
            */
            
            if (screenshotData.Length >= 0x384000)
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    DateTime currentDateTime = DateTime.Now;

                    applicationAlbumEntry = new ApplicationAlbumEntry()
                    {
                        Size              = (ulong)Marshal.SizeOf(typeof(ApplicationAlbumEntry)),
                        TitleId           = titleId,
                        AlbumFileDateTime = new AlbumFileDateTime()
                        {
                            Year     = (ushort)currentDateTime.Year,
                            Month    = (byte)currentDateTime.Month,
                            Day      = (byte)currentDateTime.Day,
                            Hour     = (byte)currentDateTime.Hour,
                            Minute   = (byte)currentDateTime.Minute,
                            Second   = (byte)currentDateTime.Second,
                            UniqueId = 0 // Incremented when there is multiple Album files with the same timestamp. Doesn't occur in our case.
                        },
                        AlbumStorage      = AlbumStorage.Sd,
                        ContentType       = ContentType.Screenshot,
                        Padding           = new Array5<byte>(),
                        Unknown0x1f       = 1
                    };

                    // NOTE: The hex hash is a HMAC-SHA256 (first 32 bytes) using a hardcoded secret key over the titleId, we can simulate it by hashing the titleId instead.
                    string hash                 = BitConverter.ToString(sha256Hash.ComputeHash(BitConverter.GetBytes(titleId))).Replace("-", "").Remove(0x20);
                    string fileName             = $"{currentDateTime.ToString("yyyyMMddHHmmss")}{applicationAlbumEntry.AlbumFileDateTime.UniqueId.ToString("00")}-{hash}.jpg";
                    string screenshotFolderPath = Path.Combine(_sdCardPath, "Nintendo", "Album", currentDateTime.Year.ToString("00"), currentDateTime.Month.ToString("00"), currentDateTime.Day.ToString("00"));

                    Directory.CreateDirectory(screenshotFolderPath);

                    // NOTE: The saved JPEG file doesn't have the extra EXIF data limitation.
                    Image.LoadPixelData<Rgba32>(screenshotData, 1280, 720).SaveAsJpegAsync(Path.Combine(screenshotFolderPath, fileName));
                }

                return ResultCode.Success;
            }

            return ResultCode.NullInputBuffer;
        }
    }
}