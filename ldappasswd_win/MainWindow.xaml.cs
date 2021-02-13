using System;
using System.Collections.Generic;
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
using LdapPasswdLib;

namespace ldappasswd_win
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();

            viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OldPasswordChanged(object sender, RoutedEventArgs e)
        {
            viewModel.OldPassword = OldPasswordBox.Password;
        }

        private void NewPasswordChanged(object sender, RoutedEventArgs e)
        {
            viewModel.NewPassword = NewPasswordBox.Password;
        }

        private void ConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }
    }
}
