using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Stores information about a client connected to the server
    /// </summary>
    public class User
    {
        /// <summary>
        /// The user's ID
        /// </summary>
        public byte Id { get; }

        /// <summary>
        /// The user's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The user's profile picture
        /// </summary>
        public byte[] Picture { get; set; }

        /// <summary>
        /// The format of the <see cref="Picture"/>
        /// </summary>
        public ImageType ImageType { get; set; }

        /// <summary>
        /// Constructs the <see cref="User "/> object
        /// </summary>
        /// <param name="id">The ID of the user</param>
        public User(byte id)
        {
            Id = id;
        }
    }
}
