using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LdapPasswdLib;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace LdapPasswdWin
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MainWindowViewModel viewModel;
        public MainPage()
        {
            InitializeComponent();

            viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;
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
