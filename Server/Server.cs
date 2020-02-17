using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Server : IDisposable
    {
        private readonly IPEndPoint _localPoint;
        private readonly List<ServerUser> _userList;
        private readonly Socket _listenSocket;
        private readonly ManualResetEvent _listenBlocker;

        private static byte Connections;

        /// <summary>
        /// Constructs the server
        /// </summary>
        /// <param name="ipAddress">The <see cref="IPAddress"/> to start the server listening on</param>
        /// <param name="portNo">The port number on which to start binding to</param>
        public Server(IPAddress ipAddress, int portNo)
        {
            _localPoint = new IPEndPoint(ipAddress, portNo);
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenBlocker = new ManualResetEvent(false);
            _userList = new List<ServerUser>();
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            ListenForConnections();
        }

        /// <summary>
        /// Asynchronously listens for incoming connections
        /// </summary>
        private void ListenForConnections()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += AcceptConnectionCallback;
            try
            {
                _listenSocket.Bind(_localPoint);
                _listenSocket.Listen(5);
                Console.WriteLine($"Listening on: {_listenSocket.LocalEndPoint}");
                while (true)
                {
                    _listenBlocker.Reset();
                    _listenSocket.BeginAccept(AcceptConnectionCallback, _listenSocket);
                    _listenBlocker.WaitOne();
                }
            }
            finally
            {
                _listenSocket.Dispose();
            }
        }

        /// <summary>
        /// The callback for when a new connection has been accepted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AcceptConnectionCallback(object sender, SocketAsyncEventArgs args)
        {
            // Get the socket created to handle the new user
            var sock = args.AcceptSocket;

            // Start listening again
            ListenForConnections();

            // Create the new user
            var newUser = new ServerUser(++Connections, sock, ProcessMessage);

            // Add the user to the collection of users
            lock (_userList)
                _userList.Add(newUser);

            // Send the user their ID
            sock.Send(new[] {newUser.Id});

            // Start the user listener service
            newUser.StartUserService();

            // Notify the existing users of the new user
            foreach (var user in _userList)
            {
                newUser.QueueMessage(new Message {Data = new[] {user.Id}, FromId = 0, ToId = newUser.Id, MessageType = MessageType.NewUser});
                user.QueueMessage(new Message {Data = new [] {newUser.Id}, FromId = 0, ToId = user.Id, MessageType = MessageType.NewUser});
            }

            // Log the connection
            Console.WriteLine($"Connection established with client {sock.RemoteEndPoint}");
        }
         
        private void AcceptConnectionCallback(IAsyncResult ar)
        {
            // Get the socket we are listening on
            var listener = (Socket) ar.AsyncState;

            // Accept the new connection
            var sock = listener.EndAccept(ar);

            // Continue listening
            _listenBlocker.Set();

            // Create the new user
            var newUser = new ServerUser(++Connections, sock, ProcessMessage);

            // Send the new user their ID
            sock.Send(new[] { newUser.Id });

            // Notify all of the users of the new connection, send name and profile picture
            foreach (var user in _userList)
            {
                newUser.QueueMessage(new Message { Data = new[] { user.Id }, FromId = 0, ToId = newUser.Id, MessageType = MessageType.NewUser });
                if (!string.IsNullOrEmpty(user.Name))
                {
                    newUser.QueueMessage(new Message { Data = Encoding.Unicode.GetBytes(user.Name), FromId = user.Id, ToId = newUser.Id, MessageType = MessageType.UserName }); 
                }
                if (user.Picture != null && user.Picture.Length > 0)
                {
                    newUser.QueueMessage(new Message { Data = user.Picture, FromId = user.Id, MessageType = MessageType.ProfilePicture, MessageData = user.ImageType.ToMessageData().GetValueOrDefault(), ToId = newUser.Id }); 
                }
                user.QueueMessage(new Message { Data = new[] { newUser.Id }, FromId = 0, ToId = user.Id, MessageType = MessageType.NewUser });
            }
            lock (_userList)
                _userList.Add(newUser);

            // Start the user receiver service
            newUser.StartUserService();

            // Log the connection
            Console.WriteLine($"Connection established with User {newUser.Id} ({sock.RemoteEndPoint})");
        }

        /// <summary>
        /// Processes received messages
        /// </summary>
        /// <param name="message">The <see cref="Message"/> to process</param>
        private void ProcessMessage(Message message)
        {
            if (message.MessageType == MessageType.ToClient)
            {
                _userList.Find(u => u.Id == message.ToId)?.QueueMessage(message);
            }
            if(message.MessageType == MessageType.UserName)
                foreach (var user in _userList)
                {
                    user.QueueMessage(message);
                }
            if (message.MessageType == MessageType.UserDisconnect)
            {
                Console.WriteLine($"User {message.FromId} has disconnected");
                _userList.RemoveAll(m => m.Id == message.FromId);
                foreach (var user in _userList)
                {
                    user.QueueMessage(message);
                }
            }
            if (message.MessageType == MessageType.UserNameRequest)
            {
                var requestedName = Encoding.Unicode.GetString(message.Data);
                if(_userList.Any(u => u.Name == requestedName))
                    foreach (var user in _userList)
                    {
                        user.QueueMessage(new Message {Data = null, FromId = message.FromId, MessageType = MessageType.UserName, ToId = user.Id});
                    }
                else
                {
                    _userList.Find(u => u.Id == message.FromId).Name = requestedName;
                    foreach (var user in _userList)
                    {
                        user.QueueMessage(new Message {Data = Encoding.Unicode.GetBytes(requestedName), FromId = message.FromId, MessageType = MessageType.UserName, ToId = user.Id});
                    }
                }
            }
            if (message.MessageType == MessageType.ProfilePictureRequest)
            {
                var sender = _userList.Find(u => u.Id == message.FromId);
                sender.Picture = message.Data;
                sender.ImageType = message.MessageData.ToImageType().GetValueOrDefault();
                foreach (var user in _userList)
                {
                    user.QueueMessage(new Message {Data = sender.Picture, FromId = message.FromId, MessageData = message.MessageData, MessageType = MessageType.ProfilePicture, ToId = user.Id});
                }
            }
            if (message.MessageType == MessageType.TypingStart || message.MessageType == MessageType.TypingEnd)
            {
                _userList.Find(u => u.Id == message.ToId)?.QueueMessage(message);
            }
        }

        public void Dispose()
        {
            _listenBlocker?.Dispose();
            _listenSocket?.Dispose();
        }
    }
}
