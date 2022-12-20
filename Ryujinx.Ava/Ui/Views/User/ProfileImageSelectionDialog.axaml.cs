using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Navigation;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.HLE.FileSystem;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using Image = SixLabors.ImageSharp.Image;

namespace Ryujinx.Ava.UI.Views
{
    public partial class ProfileImageSelectionDialog : UserControl
    {
        private ContentManager _contentManager;
        private NavigationDialogHost _parent;
        private TempProfile _profile;

        public bool FirmwareFound => _contentManager.GetCurrentFirmwareVersion() != null;

        public ProfileImageSelectionDialog()
        {
            InitializeComponent();
            AddHandler(Frame.NavigatedToEvent, (s, e) =>
            {
                NavigatedTo(e);
            }, RoutingStrategies.Direct);
        }

        private void NavigatedTo(NavigationEventArgs arg)
        {
            if (Program.PreviewerDetached)
            {
                switch (arg.NavigationMode)
                {
                    case NavigationMode.New:
                        (_parent, _profile) = ((NavigationDialogHost, TempProfile))arg.Parameter;
                        _contentManager = _parent.ContentManager;
                        break;
                    case NavigationMode.Back:
                        _parent.GoBack();
                        break;
                }

                DataContext = this;
            }
        }

        private async void Import_OnClick(object sender, RoutedEventArgs e)
        {
            var window = this.GetVisualRoot() as Window;
            var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple  = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance["AllSupportedFormats"])
                    {
                        Patterns                    = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" },
                        AppleUniformTypeIdentifiers = new[] { "public.jpeg", "public.png", "com.microsoft.bmp" },
                        MimeTypes                   = new[] { "image/jpeg", "image/png", "image/bmp" }
                    }
                }
            });

            if (result.Count > 0)
            {
                if (result[0].TryGetUri(out Uri uri))
                {
                    _profile.Image = ProcessProfileImage(File.ReadAllBytes(uri.LocalPath));
                    _parent.GoBack();
                }
            }
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            _parent?.GoBack();
        }

        private void SelectFirmwareImage_OnClick(object sender, RoutedEventArgs e)
        {
            if (FirmwareFound)
            {
                _parent.Navigate(typeof(AvatarWindow), (_parent, _profile));
            }
        }

        private static byte[] ProcessProfileImage(byte[] buffer)
        {
            using (Image image = Image.Load(buffer))
            {
                image.Mutate(x => x.Resize(256, 256));

                using (MemoryStream streamJpg = new())
                {
                    image.SaveAsJpeg(streamJpg);

                    return streamJpg.ToArray();
                }
            }
        }
    }
}