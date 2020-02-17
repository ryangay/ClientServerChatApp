using Common;

namespace Client
{
    internal class UserSettings
    {
        /// <summary>
        /// The name of the local user
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The binary representation of the user's profile picture
        /// </summary>
        public byte[] ImageData { get; set; }
        
        /// <summary>
        /// The format of the profile picture image
        /// </summary>
        public ImageType ImageType { get; set; }
    }
}