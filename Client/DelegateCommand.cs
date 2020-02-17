using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client
{
    class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;
        private bool _canExecute;

        public bool CanExecuteCommand
        {
            get { return _canExecute; }
            set
            {
                _canExecute = value;
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }
        }

        public DelegateCommand(Action<object> execute)
        {
            _execute = execute;
            CanExecuteCommand = true;
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteCommand;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged;
    }
}
