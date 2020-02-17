using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Common;

namespace Client
{
    class MainViewModel : ViewModel
    {
        private const int BUFFER_SIZE = 65536;
        private readonly string CONNECT_TITLE = "Hallo Chat - Sign In";
        private readonly string CONTACTS_TITLE = "Hallo Chat - Contacts";
        private readonly ConnectionSettings _connectSettings;
        private readonly UserSettings _userSettings;
        private Thread _receiveThread;
        private CancellationTokenSource _tokenSource;
        private TcpClient _connection;
        private ManualResetEvent _receiveBlocker;
        private List<byte> _bufferOverflow;
        private HashSet<User> _users;
        private ObservableCollection<UserViewModel> _userViewModels;
        private List<ChatViewModel> _chats;
        private byte? _myId;
        private object _windowContent;
        private bool _isListening;
        private string _windowTitle;

        /// <summary>
        /// The collection of users available to contact
        /// </summary>
        public ObservableCollection<UserViewModel> Contacts
        {
            get { return _userViewModels; }
            set
            {
                _userViewModels = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The id of the local user
        /// </summary>
        public byte? MyId
        {
            get { return _myId;}
            set
            {
                _myId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The content to be displayed
        /// </summary>
        public object WindowContent
        {
            get { return _windowContent; }
            set
            {
                _windowContent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The title
        /// </summary>
        public string WindowTitle
        {
            get { return _windowTitle; }
            set
            {
                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            Application.Current.Exit += (sender, args) =>
            {
                _isListening = false;
                _receiveBlocker?.Set();
                _receiveThread?.Abort();
                _tokenSource?.Cancel();
            };
            _connectSettings = new ConnectionSettings();
            _userSettings = new UserSettings();
            WindowContent = new ConnectViewModel(_connectSettings, _userSettings, Connect);
            WindowTitle = CONNECT_TITLE;
        }

        /// <summary>
        /// Connect to the specified server
        /// </summary>
        private void Connect()
        {
            IPEndPoint endPoint;
            try
            {
                var ipAddress = IPAddress.Parse(_connectSettings.IpAddress);
                _connection = new TcpClient();
                endPoint = new IPEndPoint(ipAddress, _connectSettings.Port);
            }
            catch (SystemException e)
            {
                MessageBox.Show(Application.Current.MainWindow, $"Error: {e.Message}", "Connection Error!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var args = new SocketAsyncEventArgs {RemoteEndPoint = endPoint};
            args.Completed += (s, eventArgs) => Application.Current.Dispatcher.Invoke(() => Connection_OnConnected(eventArgs));
            _connection.Client.ConnectAsync(args);
        }

        public void QueueMessage(Message message)
        {
            SendMessage(message);
        }

        /// <summary>
        /// Post-connection set up
        /// </summary>
        /// <param name="e">The socket event data</param>
        private void Connection_OnConnected(SocketAsyncEventArgs e)
        {
            var socket = e.ConnectSocket;
            if (socket == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "Error connecting to server.\r\nPlease check details and try again.", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                //WindowContent = new ConnectViewModel(_connectSettings, _userSettings, Connect);
                return;
            }
            _users = new HashSet<User>();
            _userViewModels = new ObservableCollection<UserViewModel>();
            // Get the Id
            var buffer = new byte[1];
            socket.Receive(buffer);
            MyId = buffer[0];
            if(_userSettings.Name != null)
                SendMessage(new Message{Data = Encoding.Unicode.GetBytes(_userSettings.Name), FromId = MyId.Value, MessageType = MessageType.UserNameRequest, ToId = 0});
            if(_userSettings.ImageData != null)
                SendMessage(new Message {Data = _userSettings.ImageData, MessageType = MessageType.ProfilePictureRequest, FromId = MyId.Value, MessageData = _userSettings.ImageType == ImageType.Jpeg ? MessageData.JpegImage : MessageData.PngImage, ToId = 0});
            _tokenSource = new CancellationTokenSource();
            //Task.Factory.StartNew(ListenForMessages, _tokenSource.Token);
            _receiveThread = new Thread(ListenForMessages);
            _receiveBlocker = new ManualResetEvent(false);
            _receiveThread.Start();
            WindowContent = this;
            WindowTitle = CONTACTS_TITLE;
        }

        /// <summary>
        /// Sends a message to the server
        /// </summary>
        /// <param name="msg"></param>
        private void SendMessage(Message msg)
        {
            msg.FromId = MyId.GetValueOrDefault();
            var msgBytes = msg.GetBytes();
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(msgBytes, 0, msgBytes.Length);
            args.Completed += Send_OnCompleted;
            _connection.Client.SendAsync(args);
        }

        private void Send_OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            //if(e.SocketError == SocketError.Success)
            //    MessageBox.Show($"Message Sent..!\r\n{e.SocketError}");
            //_sendBlocker.Set();
        }

        private void ListenForMessages()
        {
            _bufferOverflow = new List<byte>();
            _chats = new List<ChatViewModel>();
            _isListening = true;
            while (_isListening)
            {
                ReceiveMessage();
            }
        }

        /// <summary>
        /// Sets up the buffer and calls the asynchronous BeginReceive method
        /// </summary>
        /// <param name="bytesToRead"></param>
        private void ReceiveMessage(int bytesToRead = -1)
        {
            var stream = _connection.GetStream();
            var buffer = new byte[BUFFER_SIZE];
            var state = new ReceiveState(stream, buffer, buffer.Length);
            _receiveBlocker.Reset();
            stream.BeginRead(buffer, 0, bytesToRead == -1 ? buffer.Length : bytesToRead, ReadCallback, state);
            _receiveBlocker.WaitOne();
        }

        /// <summary>
        /// Processes the data after it has been received
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            var state = (ReceiveState) ar.AsyncState;
            var receivedData = state.Buffer;
            int receivedDataCount;
            try
            {
                // End the read of the socket
                receivedDataCount = state.Stream.EndRead(ar);
            }
            catch (Exception e)
            {
                var inner = e.InnerException as SocketException;
                if (inner == null) return;
                if (inner.SocketErrorCode != SocketError.ConnectionReset && inner.SocketErrorCode != SocketError.ConnectionAborted) throw;
                Restart();
                return;
            }
            byte[] data;
            // If some data has already been received
            if (_bufferOverflow.Count > 0)
                // Add the new data to thew previously received data
                data = _bufferOverflow.Concat(receivedData.Take(receivedDataCount)).ToArray();
            else
                data = receivedData.Take(receivedDataCount).ToArray();
            /* If the amount of received data is more or equal to the size of the buffer,
            ** then there must be more data to receive so store the data in the buffer overflow, and receive again
            */
            if (receivedDataCount >= BUFFER_SIZE)
            {
                _bufferOverflow = data.ToList();
                _receiveBlocker.Set(); // Allow the receive thread to continue
                return;
            }
            var messages = new List<Message>();

            // While the total size of the parsed messages is less than the amount of data to parse
            while (messages.Sum(m => m.Size) < data.Length)
            {
                var startOfNextMessage = messages.Sum(m => m.Size);
                var nextMessageSize = Message.GetMessageSize(data, startOfNextMessage);
                if (nextMessageSize <= data.Length - messages.Sum(m => m.Size))
                {
                    byte[] nextMessageData = new byte[nextMessageSize];
                    Array.ConstrainedCopy(data, startOfNextMessage, nextMessageData, 0, nextMessageSize);
                    messages.Add(Message.GetMessage(nextMessageData));
                }
                // If the size of the next message is more than the amount of parseable data, Receive again as we don't have all the data
                else
                {
                    Debug.WriteLine("Didn't receive enough data so going round again.");
                    _bufferOverflow = data.ToList();
                    _receiveBlocker.Set();
                    return;
                }
            }
            // We have parsed all of the buffer's data, so if it contains data then clear it
            if (_bufferOverflow.Count > 0)
                _bufferOverflow.Clear();
            // Allow the receive thread to continue, now we are done with the buffer
            _receiveBlocker.Set();

            // Process and decide how to act on each of the received messages
            foreach (var msg in messages)
            {
                if (msg.MessageType == MessageType.ToClient)
                {
                    if (_chats.Any(c => c.Contact.Id == msg.FromId))
                    {
                        var chat = _chats.Single(c => c.Contact.Id == msg.FromId);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            chat.AddMessage(msg);
                            chat.Show();
                        });
                    }
                    else
                    {
                        Debug.Assert(MyId != null, "Client does not have an ID");
                        var chatVm = new ChatViewModel(QueueMessage, MyId.Value, _userViewModels.Single(u => u.Id == msg.FromId), msg);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var newChat = new ChatWindow { DataContext = chatVm, Owner = Application.Current.MainWindow};
                            newChat.Show();
                        });
                        _chats.Add(chatVm);
                    }
                }
                if (msg.MessageType == MessageType.NewUser && msg.Data[0] != _myId.GetValueOrDefault())
                    Application.Current.Dispatcher.Invoke(() => AddUser(new User(msg.Data[0])));
                if (msg.MessageType == MessageType.UserDisconnect)
                    Application.Current.Dispatcher.Invoke(() => RemoveUser(msg.FromId));
                if (msg.MessageType == MessageType.UserName && msg.FromId != MyId.GetValueOrDefault())
                    Application.Current.Dispatcher.Invoke(
                        () =>
                        {
                            _userViewModels.Single(u => u.Id == msg.FromId).Name = Encoding.Unicode.GetString(msg.Data);
                        });
                if (msg.MessageType == MessageType.ProfilePicture && msg.FromId != MyId)
                    Application.Current.Dispatcher.Invoke(() => UpdatePicture(msg));
                if (msg.MessageType == MessageType.TypingStart || msg.MessageType == MessageType.TypingEnd)
                    Application.Current.Dispatcher.Invoke(() => SetTyping(msg));
            }
        }

        /// <summary>
        /// Enable/Disable the typing notification within a chat
        /// </summary>
        /// <param name="msg">The message containing the typing notification</param>
        private void SetTyping(Message msg)
        {
            var chat = _chats.SingleOrDefault(c => c.Contact.Id == msg.FromId);
            if (chat == null) return;
            chat.IsContactTyping = msg.MessageType == MessageType.TypingStart;
        }

        /// <summary>
        /// Update a user's profile picture
        /// </summary>
        /// <param name="msg">The message containing the image data</param>
        private void UpdatePicture(Message msg)
        {
            var user = _userViewModels.SingleOrDefault(u => u.Id == msg.FromId);
            if (user == null) return;
            user.Picture = msg.Data;
            user.PictureType = msg.MessageData == MessageData.PngImage ? ImageType.Png : ImageType.Jpeg;
        }

        /// <summary>
        /// Add a user to the list
        /// </summary>
        /// <param name="user">The <see cref="User"/> to add</param>
        public void AddUser(User user)
        {
            lock (_users)
            {
                _users.Add(user);
                _userViewModels.Add(new UserViewModel(user));
                OnPropertyChanged(nameof(Contacts));
            }
        }

        /// <summary>
        /// Remove a user
        /// </summary>
        /// <param name="id">The ID of the user to remove</param>
        public void RemoveUser(int id)
        {
            var toRemove = _userViewModels.SingleOrDefault(u => u.Id == id);
            if (toRemove == null) return;
            _userViewModels.Remove(toRemove);
            _users.Remove(toRemove.User);
        }

        /// <summary>
        /// Determines whether a chat already exists
        /// </summary>
        /// <param name="userId">The ID of the chat recepient</param>
        /// <returns></returns>
        public bool ChatExists(int userId)
        {
            return _chats.Any(c => c.Contact.Id == userId);
        }

        /// <summary>
        /// Add a chat
        /// </summary>
        /// <param name="chat"></param>
        public void AddChat(ChatViewModel chat)
        {
            _chats.Add(chat);
        }

        public ChatViewModel GetExistingChat(int contactId)
        {
            return _chats.Single(c => c.Contact.Id == contactId);
        }

        public void RemoveChat(ChatViewModel chat)
        {
            _chats.Remove(chat);
        }


        /// <summary>
        /// Soft reset of the chat functionality, return back to the control that handles the <see cref="ConnectViewModel"/>
        /// </summary>
        private void Restart()
        {
            _chats.Clear();
            _users.Clear();
            WindowContent = new ConnectViewModel(_connectSettings, _userSettings, Connect);
            WindowTitle = CONNECT_TITLE;
        }
    }
}
