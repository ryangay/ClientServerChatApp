using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace Server
{
    class ServerUser : User
    {
        private const int BUFFER_SIZE = 65536;
        private List<byte> _bufferOverflow;

        /// <summary>
        /// The connection to the user
        /// </summary>
        public Socket Connection { get; }

        /// <summary>
        /// Processes received messages
        /// </summary>
        private Action<Message> ProcessMessage { get; }
        
        /// <summary>
        /// Constructs the user
        /// </summary>
        /// <param name="id">The user's ID</param>
        /// <param name="processor">The method used to process recieved messages</param>
        public ServerUser(byte id, Action<Message> processor) : base(id)
        {
            ProcessMessage = processor;
            _bufferOverflow = new List<byte>();
        }

        /// <summary>
        /// Constructs the user
        /// </summary>
        /// <param name="id">The user's ID</param>
        /// <param name="processor">The method used to process recieved messages</param>
        /// <param name="conn">The connection to the user</param>
        public ServerUser(byte id, Socket conn, Action<Message> processor) : this(id, processor)
        {
            Connection = conn;
        }

        /// <summary>
        /// Starts receiving data from the user
        /// </summary>
        public void StartUserService()
        {
            Receive();
        }

        /// <summary>
        /// Sends a message to the user
        /// </summary>
        /// <param name="message">The message to send</param>
        public void QueueMessage(Message message)
        {
            Send(message);
        }

        /// <summary>
        /// Asynchronously begins receving data from the user
        /// </summary>
        private void Receive()
        {
            var buffer = new byte[BUFFER_SIZE];
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, 0, buffer.Length);
            args.Completed += ReceiveDataCallback;
            Connection.ReceiveAsync(args);
        }

        /// <summary>
        /// Callback for when data is received from the user
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void ReceiveDataCallback(object sender, SocketAsyncEventArgs e)
        {
            // Get the buffer of received data, and number of bytes received
            var receivedData = e.Buffer;
            var receivedDataCount = e.BytesTransferred;
            byte[] data;
            // If some data has already been received
            if (_bufferOverflow.Count > 0)
                // Add the new data to thew previously received data
                data = _bufferOverflow.Concat(receivedData.Take(receivedDataCount)).ToArray();
            else
            {
                data = receivedData.Take(receivedDataCount).ToArray();
                if (data.Length == 0)
                {
                    var msg = new Message {FromId = Id, MessageType = MessageType.UserDisconnect};
                    ProcessMessage(msg);
                    return;
                }
            }
            /* If the amount of received data is more or equal to the size of the buffer,
            ** then there must be more data to receive so store the data in the buffer overflow, and receive again
            */
            if (receivedDataCount >= BUFFER_SIZE)
            {
                _bufferOverflow = data.ToList();
                Receive(); // Receive more data
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
                    _bufferOverflow = data.ToList();
                    Receive();
                    return;
                }
            }
            // We have parsed all of the buffer's data, so if it contains data then clear it
            if (_bufferOverflow.Count > 0)
                _bufferOverflow.Clear();
            foreach (var msg in messages)
            {
                ProcessMessage(msg);
            }
            // Receive more data
            Receive();
        }

        /// <summary>
        /// Asynchrnously sends a message to the user
        /// </summary>
        /// <param name="msg">The message to send</param>
        private void Send(Message msg)
        {
            // Gets the message's binary represntation
            var buffer = msg.GetBytes();
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, 0, buffer.Length);
            args.Completed += (s, eventArgs) =>
                Console.WriteLine( // Log the send event
                    $"Message sent from User {msg.FromId} to User {Id}: {Encoding.ASCII.GetString(msg.Data.Take(20).ToArray())}");
            for (var i = 0; i < 10 && !Connection.Connected; i++) { Task.Delay(100); }

            // Send the message
            Connection.SendAsync(args);
        }
    }
}
