using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Common
{
    /// <summary>
    /// Describes information to send over through a socket to the server or client
    /// </summary>
    public class Message
    {
        /// <summary>
        /// The constant size of a <see cref="Message"/> header
        /// </summary>
        public const int HEADER_SIZE = 16;

        /// <summary>
        /// The type of the message
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// The data that the message contains
        /// </summary>
        public MessageData MessageData { get; set; }

        /// <summary>
        /// The time and date of when the message was sent
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The size of the message (including the constant <see cref="HEADER_SIZE"/>)
        /// </summary>
        public int Size => HEADER_SIZE + (Data?.Length ?? 0);

        /// <summary>
        /// The Message's data
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The ID of the user that the message is intended for
        /// </summary>
        public byte ToId { get; set; }

        /// <summary>
        /// The ID of the user that sent the message
        /// </summary>
        public byte FromId { get; set; }
        
        /// <summary>
        /// Constructs a <see cref="Message"/> object from its serialised binary representation
        /// </summary>
        /// <param name="data">The binary message</param>
        /// <returns>A <see cref="Message"/> object</returns>
        public static Message GetMessage(byte[] data)
        {
            var msg = new Message();

            var header = data.Take(HEADER_SIZE).ToArray();
            msg.Timestamp = new DateTime(BitConverter.ToInt64(header, 0));
            msg.ToId = header[8];
            msg.FromId = header[9];
            msg.MessageType = (MessageType) header[10];
            msg.MessageData = (MessageData) header[11];
            var size = BitConverter.ToInt32(header, 12);
            msg.Data = new byte[size];
            Array.Copy(data, HEADER_SIZE, msg.Data, 0, size);

            return msg;
        }

        /// <summary>
        /// Serialises this message
        /// </summary>
        /// <returns>A binary representation of the message</returns>
        /// <remarks>
        /// The serialised form of a Hallo Chat message is as follows:
        /// 
        ///   Bytes   0 - 7: The <see cref="Timestamp"/> of the message
        ///   Byte        8: The sender of the message (<see cref="FromId"/>)
        ///   Byte        9: The intended recpient of the message (<see cref="ToId"/>)
        ///   Byte       10: The type of message (<see cref="MessageType"/>)
        ///   Byte       11: The format of the Message's data (<see cref="MessageData"/>)
        ///   Bytes 12 - 15: The size of the message (<see cref="Size"/>) (exluding the constant <see cref="HEADER_SIZE"/>)
        ///   Bytes 16 -  n: The message's main content (<see cref="Data"/>)
        /// </remarks>
        public byte[] GetBytes()
        {
            var bytes = new byte[Size];
            Data = Data ?? new byte[0];
            Array.ConstrainedCopy(BitConverter.GetBytes(Timestamp.Ticks), 0, bytes, 0, 8);
            bytes[8] = ToId;
            bytes[9] = FromId;
            bytes[10] = (byte) MessageType;
            bytes[11] = (byte) MessageData;
            Array.ConstrainedCopy(BitConverter.GetBytes(Size-HEADER_SIZE), 0, bytes, 12, 4);
            Array.ConstrainedCopy(Data, 0, bytes, 16, Size - HEADER_SIZE);

            return bytes;
        }

        /// <summary>
        /// Gets the message size from a binary representation (including the constant <see cref="HEADER_SIZE"/>)
        /// </summary>
        /// <param name="data">The binary represntation of a serialised message</param>
        /// <param name="messageStartIndex">The index at which the message starts within the data</param>
        /// <returns>An integer representing the size of the data</returns>
        public static int GetMessageSize(byte[] data, int messageStartIndex)
        {
            return HEADER_SIZE + BitConverter.ToInt32(data, messageStartIndex + 12);
        }
    }
}
