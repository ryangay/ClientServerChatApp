using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Common;
using Microsoft.Win32;

namespace Client
{
    class ChatViewModel : ViewModel
    {
        private UserViewModel _user;
        private byte _myId;
        private Action<Message> _sendMessage;
        private bool _topMost;
        private string _textMessage;
        private bool _isAlreadyTyping;
        private bool _isContactTyping;

        /// <summary>
        /// Speicifes whether the chat window should be on top of all others
        /// </summary>
        public bool TopMost
        {
            get { return _topMost; }
            private set
            {
                _topMost = value; 
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Specifices whether the chat's recepient is typing or not
        /// </summary>
        public bool IsContactTyping
        {
            get { return _isContactTyping; }
            set
            {
                _isContactTyping = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The recepient of the chat's messages
        /// </summary>
        public UserViewModel Contact
        {
            get { return _user; }
            set
            {
                _user = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The ID of the local user
        /// </summary>
        public byte MyId => _myId;

        /// <summary>
        /// The message typed by the local user
        /// </summary>
        public string TextMessage
        {
            get { return _textMessage; }
            set
            {
                _textMessage = value;
                OnPropertyChanged();
                SendTypingNotification();
            }
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        public ICommand SendMessageCommand { get; }

        /// <summary>
        /// Sends an image
        /// </summary>
        public ICommand SendImageCommand { get; }

        /// <summary>
        /// The collection of messages belonging to this chat
        /// </summary>
        public ObservableCollection<Message> Messages { get; }

        /// <summary>
        /// Constructs the chat
        /// </summary>
        /// <param name="sendMessage">The action to take when the chat needs to send a message</param>
        /// <param name="myId">The ID of the local user</param>
        private ChatViewModel(Action<Message> sendMessage, byte myId)
        {
            _sendMessage = sendMessage;
            _myId = myId;
            SendMessageCommand = new DelegateCommand(SendMessage);
            SendImageCommand = new DelegateCommand(SendImage);
        }

        /// <summary>
        /// Constructs the chat
        /// </summary>
        /// <param name="sendMessage">The action to take when the chat needs to send a message</param>
        /// <param name="myId">The ID of the local user</param>
        /// <param name="contact">The recipient of the chat's messages</param>
        public ChatViewModel(Action<Message> sendMessage, byte myId, UserViewModel contact) : this(sendMessage, myId)
        {
            Contact = contact;
            Messages = new ObservableCollection<Message>();
        }

        /// <summary>
        /// Constructs the chat
        /// </summary>
        /// <param name="sendMessage">The action to take when the chat needs to send a message</param>
        /// <param name="myId">The ID of the local user</param>
        /// <param name="contact">The recipient of the chat's messages</param>
        /// <param name="message">A message to insert to the chat</param>
        public ChatViewModel(Action<Message> sendMessage, byte myId, UserViewModel contact, Message message) 
            : this(sendMessage, myId, contact)
        {
            Messages = new ObservableCollection<Message> {message};
            Show();
        }

        /// <summary>
        /// Adds a message into the chat
        /// </summary>
        /// <param name="message">The message to add</param>
        public void AddMessage(Message message)
        {
            Messages.Add(message);
        }

        /// <summary>
        /// Shows the chat by making it the TopMost window
        /// </summary>
        public void Show()
        {
            TopMost = true;
            TopMost = false;
        }

        /// <summary>
        /// Sends a typing notification to the chat's recepient
        /// </summary>
        private void SendTypingNotification()
        {
            var isTyping = !string.IsNullOrWhiteSpace(TextMessage);

            if (!(isTyping && _isAlreadyTyping))
            {
                _sendMessage(new Message
                {
                    ToId = Contact.Id,
                    FromId = MyId,
                    MessageType = isTyping ? MessageType.TypingStart : MessageType.TypingEnd
                }); 
            }
            _isAlreadyTyping = isTyping;
        }

        /// <summary>
        /// Sends a text message to the recepient
        /// </summary>
        /// <param name="data"></param>
        private void SendMessage(object data)
        {
            var message = new Message
            {
                Data = Encoding.Unicode.GetBytes(TextMessage),
                ToId = Contact.Id,
                MessageType = MessageType.ToClient, MessageData = MessageData.Text,
                Timestamp = DateTime.Now
            };
            Messages.Add(message);
            _sendMessage(message);
            TextMessage = string.Empty;
        }

        /// <summary>
        /// Sends an image to the chat's recipient
        /// </summary>
        /// <param name="data"></param>
        private void SendImage(object data)
        {
            // Open a file picker dialog
            var dialog = new OpenFileDialog {Multiselect = false, Filter = "Image Files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png;"};
            string filename;
            if (dialog.ShowDialog() == true)
                filename = dialog.FileName;
            else
                return;
            // Get the file info
            var info = new FileInfo(filename);
            var fileSize = (int)info.Length;

            // Open the file
            var imageStream = File.Open(filename, FileMode.Open);
            var buffer = new byte[fileSize];

            // Bring the file into the in-memory buffer
            imageStream.Read(buffer, 0, fileSize);
            var message = new Message
            {
                Data = buffer,
                ToId = Contact.Id,
                MessageType = MessageType.ToClient,
                MessageData =
                    info.Extension == ".jpeg" || info.Extension == ".jpg" ? MessageData.JpegImage : MessageData.PngImage,
                Timestamp = DateTime.Now
            };
            Messages.Add(message);
            _sendMessage(message);
            TextMessage = string.Empty;
        }
    }
}
