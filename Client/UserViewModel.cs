using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Client
{
    class UserViewModel : ViewModel
    {
        /// <summary>
        /// The user represeneted by the view model
        /// </summary>
        public User User { get; }

        /// <summary>
        /// The user's name
        /// </summary>
        public string Name
        {
            get { return User.Name; }
            set
            {
                User.Name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The user's Id
        /// </summary>
        public byte Id => User.Id;

        /// <summary>
        /// The user's profile picture
        /// </summary>
        public byte[] Picture
        {
            get { return User.Picture; }
            set
            {
                User.Picture = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The format of the user's profile picture
        /// </summary>
        public ImageType PictureType
        {
            get { return User.ImageType; }
            set
            {
                User.ImageType = value;
                OnPropertyChanged();
            }
        }

        public UserViewModel(User user)
        {
            User = user;
        }
    }
}
