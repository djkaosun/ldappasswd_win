using System;
using System.ComponentModel;
using System.DirectoryServices.Protocols;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;

namespace LdapPasswdLib
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// LDAPS のポート番号
        /// </summary>
        public const int LDAP_PORT = 389;
        public const int LDAPS_PORT = 636;

        #region Properties for Binding

        private string _Server;
        public string Server
        {
            get { return _Server; }
            set
            {
                _Server = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Server)));
            }
        }

        private bool _IsTls;
        public bool IsTls
        {
            get { return _IsTls; }
            set
            {
                _IsTls = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTls)));
            }
        }

        private string _DistinguishedName;
        public string DistinguishedName
        {
            get { return _DistinguishedName; }
            set
            {
                _DistinguishedName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistinguishedName)));
            }
        }

        private string _OldPassword;
        public string OldPassword
        {
            get { return _OldPassword; }
            set
            {
                _OldPassword = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OldPassword)));
            }
        }

        private string _NewPassword;
        public string NewPassword
        {
            get { return _NewPassword; }
            set
            {
                _NewPassword = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewPassword)));
            }
        }

        private string _ConfirmPassword;
        public string ConfirmPassword
        {
            get { return _ConfirmPassword; }
            set
            {
                _ConfirmPassword = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConfirmPassword)));
            }
        }

        private string _Message;
        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }
        }

        private Brush _MessageColor;
        public Brush MessageColor
        {
            get { return _MessageColor; }
            set
            {
                _MessageColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageColor)));
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 実行コマンド。
        /// </summary>
        public ICommand ExecuteCommand { get; private set; }
        private class ExecuteCommandImpl : ICommand
        {
            private MainWindowViewModel viewModel;
            public ExecuteCommandImpl(MainWindowViewModel viewModel)
            {
                this.viewModel = viewModel;
                viewModel.PropertyChanged += OnViewModelPropertyChangedEventHandler;
            }

            private void OnViewModelPropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(viewModel.CanExecute):
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return viewModel.CanExecute;
            }

            public void Execute(object parameter)
            {
                viewModel.CanExecute = false;
                var serverString = ServerPortSpecify(viewModel.Server, viewModel.IsTls);
                try
                {
                    LdapPasswordChanger.ChangePassword(viewModel.DistinguishedName, viewModel.OldPassword, viewModel.NewPassword, serverString, viewModel.IsTls);

                    viewModel.Message = "Complete successfully.\nYour password is changed.";
                    viewModel.MessageColor = Brushes.DodgerBlue;
                }
                catch(Exception e)
                {
                    if(e is LdapException ldapException)
                    {
                        viewModel.Message = "(" + ldapException.GetType().Name + ":" + ldapException.ErrorCode + ")\n"
                                + ldapException.Message;
                        if (!String.IsNullOrEmpty(ldapException.ServerErrorMessage)) viewModel.Message += "\n" + ldapException.ServerErrorMessage;
                    }
                    else {
                        viewModel.Message = "(" + e.GetType().Name + ")\n" + e.Message;
                    }
                    viewModel.MessageColor = Brushes.Red;
                    viewModel.CanExecute = true;
                }
            }
        }

        #endregion

        private bool _CanExecute;
        private bool CanExecute {
            get { return _CanExecute; }
            set
            {
                _CanExecute = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanExecute)));
            }
        }

        /// <summary>
        /// プロパティが変更されたときに発生するイベントです。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            ExecuteCommand = new ExecuteCommandImpl(this);
            this.PropertyChanged += PropertyChangedEventHandler;
            ConfirmPassword = String.Empty;
        }

        #region Event Handlers

        private void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Server):
                case nameof(DistinguishedName):
                case nameof(OldPassword):
                case nameof(NewPassword):
                case nameof(ConfirmPassword):
                    if (String.IsNullOrEmpty(Server))
                    {
                        Message = "Server is null.";
                        MessageColor = Brushes.Red;
                        CanExecute = false;
                    }
                    else if (String.IsNullOrEmpty(DistinguishedName))
                    {
                        Message = "DN is null.";
                        MessageColor = Brushes.Red;
                        CanExecute = false;
                    }
                    else if (String.IsNullOrEmpty(OldPassword))
                    {
                        Message = "Old password is null.";
                        MessageColor = Brushes.Red;
                        CanExecute = false;
                    }
                    else if (String.IsNullOrEmpty(NewPassword))
                    {
                        Message = "New password is null.";
                        MessageColor = Brushes.Red;
                        CanExecute = false;
                    }
                    else if (NewPassword != ConfirmPassword)
                    {
                        Message = "Input for confirm is different from new password.";
                        MessageColor = Brushes.Red;
                        CanExecute = false;
                    }
                    else
                    {
                        Message = "Passed the input validation check.";
                        MessageColor = Brushes.DodgerBlue;
                        CanExecute = true;
                    }
                    break;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// TCP ポート指定がされていないとき、明示的にポート指定を追加する。
        /// </summary>
        /// <param name="ldapServer">LDAP サーバー指定</param>
        private static string ServerPortSpecify(string ldapServer, bool isTls)
        {
            if (!Regex.IsMatch(ldapServer, ":[1-9][0-9]*$"))
            {
                if (isTls) return ldapServer + ":" + LDAPS_PORT;
                else return ldapServer + ":" + LDAP_PORT;
            }
            else return ldapServer;
        }

        #endregion
    }
}
