using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Common;

namespace Client
{
    /// <summary>
    /// Interaction logic for ContactsControl.xaml
    /// </summary>
    public partial class ContactsControl : UserControl
    {
        /// <summary>
        /// Constructs a <see cref="ContactsControl" />
        /// </summary>
        public ContactsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Captures the click event when a contact is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contact_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement != null)
            {
                // Get the user
                var user = frameworkElement.DataContext as UserViewModel;
                if (user == null)
                    return;
                var dataContext = (MainViewModel)DataContext;
                var already = dataContext.ChatExists(user.Id);

                // Get the window
                var thisWindow = Window.GetWindow(this);
                if (thisWindow == null)
                    throw new InvalidOperationException("Error finding the client's window.");
                Window chatWindow;
                bool clearChat = true;
                // If there is already a chat with this user
                if (already)
                {
                    chatWindow = thisWindow?.OwnedWindows.Cast<Window>().SingleOrDefault(w => ((ChatViewModel)w.DataContext).Contact == user);
                    if (chatWindow != null)
                    {
                        chatWindow.Activate();
                        return; 
                    }
                    clearChat = MessageBox.Show(thisWindow,
                        "Would you like to recover the contents of the last chat you had with this contact?",
                        "Existing chat found", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No) == MessageBoxResult.No;
                }

                Debug.Assert(dataContext.MyId != null, "My Id is null");
                var chat = already && !clearChat ? dataContext.GetExistingChat(user.Id) : new ChatViewModel(dataContext.QueueMessage, dataContext.MyId.Value, user);
                if(!already)
                    dataContext.AddChat(chat);
                // Create the chat window
                var newChat = new ChatWindow
                {
                    DataContext = chat,
                    Owner = thisWindow
                };
                newChat.Show();
            }
        }
    }
}
