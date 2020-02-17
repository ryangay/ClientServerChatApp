using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Object to hold state data when receiving from a socket asynchronously
    /// </summary>
    public class ReceiveState
    {
        /// <summary>
        /// The stream to which receive data from
        /// </summary>
        public NetworkStream Stream { get; }

        /// <summary>
        /// The buffer holding the received data
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// The length of the buffer
        /// </summary>
        public int BufferLength { get; }

        /// <summary>
        /// Constructs the <see cref="ReceiveState"/> object
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferLength"></param>
        public ReceiveState(NetworkStream stream, byte[] buffer, int bufferLength)
        {
            Stream = stream;
            Buffer = buffer;
            BufferLength = bufferLength;
        }
    }
}
