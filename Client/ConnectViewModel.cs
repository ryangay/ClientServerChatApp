using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Common;

namespace Client
{
    class ConnectViewModel : ViewModel
    {
        private Visibility _portVisibility;
        private readonly ConnectionSettings _connectionSettings;
        private readonly UserSettings _userSettings;
        private bool _canConnect;

        private Action ConnectToServer { get; }

        /// <summary>
        /// The IP Address of the messaging server
        /// </summary>
        public string IpAddress
        {
            get { return _connectionSettings.IpAddress;}
            set
            {
                _connectionSettings.IpAddress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The port that the messaging server is listening on
        /// </summary>
        public int Port
        {
            get { return _connectionSettings.Port; }
            set
            {
                _connectionSettings.Port = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The name of the local user
        /// </summary>
        public string Name
        {
            get { return _userSettings.Name; }
            set
            {
                _userSettings.Name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The image buffer of the user's profile picture
        /// </summary>
        public byte[] ImageData
        {
            get { return _userSettings.ImageData; }
            set
            {
                _userSettings.ImageData = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The type of the image (JPEG/PNG)
        /// </summary>
        public ImageType ImageType
        {
            get { return _userSettings.ImageType; }
            set
            {
                _userSettings.ImageType = value;
                OnPropertyChanged();
            }
        }

        public bool CanConnect
        {
            get { return _canConnect; }
            set
            {
                _canConnect = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Visibility of the Port input field
        /// </summary>
        public Visibility PortVisibility
        {
            get { return _portVisibility; }
            set
            {
                _portVisibility = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command to toggle the visibility of the <see cref="Port"/> field
        /// </summary>
        public ICommand TogglePortCommand { get; }

        /// <summary>
        /// Command to select a profile picture
        /// </summary>
        public ICommand UpdateImageCommand { get; }

        /// <summary>
        /// Command that tells the client to connect the server
        /// </summary>
        public ICommand ConnectCommand { get; }

        /// <summary>
        /// Constructs the Connector
        /// </summary>
        /// <param name="connectSettings">The object to hold the connection settings</param>
        /// <param name="userSettings">The object to hold the user settings</param>
        /// <param name="connect">The action to trigger when the client is told to connect</param>
        public ConnectViewModel(ConnectionSettings connectSettings, UserSettings userSettings, Action connect)
        {
            _connectionSettings = connectSettings;
            _userSettings = userSettings;
            ConnectToServer = connect;
            PortVisibility = Visibility.Collapsed;
            TogglePortCommand = new DelegateCommand(o => PortVisibility = PortVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible);
            UpdateImageCommand = new DelegateCommand(o => UpdateImage());
            ConnectCommand = new DelegateCommand(o => Connect());
            CanConnect = true;
        }

        private void Connect()
        {
            CanConnect = false;
            ConnectToServer();
        }

        /// <summary>
        /// Allows the user to select a profile picture
        /// </summary>
        private void UpdateImage()
        {
            var dialog = new OpenFileDialog { Multiselect = false, Filter = "Image Files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png;" };
            string filename;
            if (dialog.ShowDialog() == true)
                filename = dialog.FileName;
            else
                return;
            var info = new FileInfo(filename);
            var fileSize = (int)info.Length;
            var extension = info.Extension == ".png" ? ImageType.Png : ImageType.Jpeg;
            var imageStream = File.Open(filename, FileMode.Open);
            var buffer = new byte[fileSize];
            imageStream.Read(buffer, 0, fileSize);
            ImageType = extension;
            ImageData = buffer;
        }
    }
}
