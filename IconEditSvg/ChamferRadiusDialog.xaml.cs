using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください
// Chamfer_R_Button_Click
// ChamferRadiusDialog
namespace IconEditSvg
{
    public sealed partial class ChamferRadiusDialog : ContentDialog
    {
        public ChamferRadiusDialog()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RadiusText.Text = Radius.ToString();
            

        }

        private int _radius = 4;
        public int Radius { get { return _radius; } internal set { _radius = value; } }

        bool _success = false;
        public bool Success { get { return _success; } }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

            int width = 0;
            Int32.TryParse(RadiusText.Text, out width);
            if (width >= 1) {
                _radius = width;
                _success = true;
            }

        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
