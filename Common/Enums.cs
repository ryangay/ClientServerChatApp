using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Describes the format of an image
    /// </summary>
    public enum ImageType
    {
        /// <summary>
        /// Image is in the Portable Network Graphics (PNG) format
        /// </summary>
        Png,

        /// <summary>
        /// Image is in the Joint Photographic Experts Group (JPEG) format
        /// </summary>
        Jpeg
    }

    /// <summary>
    /// Describes the purpose of <see cref="Message"/>
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// A "normal" message to be sent to another client user
        /// </summary>
        ToClient,

        /// <summary>
        /// Notification that a new user has connected to the server
        /// </summary>
        NewUser,

        /// <summary>
        /// Notification that a user has disconnected
        /// </summary>
        UserDisconnect,

        /// <summary>
        /// Sends a request for a user name
        /// </summary>
        UserNameRequest,

        /// <summary>
        /// Message contains the user name of a user
        /// </summary>
        UserName,

        /// <summary>
        /// Sends a request for a profile picture
        /// </summary>
        ProfilePictureRequest,

        /// <summary>
        /// Message contains the profile picture of a user
        /// </summary>
        ProfilePicture,

        /// <summary>
        /// Notification that a user has started typing
        /// </summary>
        TypingStart,

        /// <summary>
        /// Notification that a user has stopped typing
        /// </summary>
        TypingEnd
    }

    /// <summary>
    /// Describes the data contained within a <see cref="Message"/>
    /// </summary>
    public enum MessageData : byte
    {
        /// <summary>
        /// The data is should be converted to a <see cref="string"/>
        /// </summary>
        Text,

        /// <summary>
        /// The data is a Portable Network Graphics (PNG) format image
        /// </summary>
        PngImage,

        /// <summary>
        /// The data is a Joint Photographic Experts Group (JPEG) format image
        /// </summary>
        JpegImage
    }

    /// <summary>
    /// Utility methods for <see cref="Enum"/>
    /// </summary>
    public static class EnumUtils
    {
        /// <summary>
        /// Converts the <see cref="MessageData"/> to a <see cref="Nullable{ImageType}"/> of <see cref="ImageType"/>
        /// </summary>
        /// <param name="data">The <see cref="MessageData"/> to convert</param>
        /// <returns></returns>
        public static ImageType? ToImageType(this MessageData data)
        {
            switch (data)
            {
                case MessageData.JpegImage:
                    return ImageType.Jpeg;
                case MessageData.PngImage:
                    return ImageType.Png;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts the <see cref="ImageType"/> to a <see cref="Nullable{T}"/> of <see cref="ImageType"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MessageData? ToMessageData(this ImageType type)
        {
            switch (type)
            {
                case ImageType.Jpeg:
                    return MessageData.JpegImage;
                case ImageType.Png:
                    return MessageData.PngImage;
                default:
                    return null;
            }
        }
    }
}
