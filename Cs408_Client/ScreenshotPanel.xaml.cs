using System.Windows;
using System.Windows.Media.Imaging;

namespace Client
{
    /// <summary>
    /// Interaction logic for ScreenshotPanel.xaml
    /// </summary>
    public partial class ScreenshotPanel : Window
    {
        public ScreenshotPanel(BitmapImage source)
        {
            InitializeComponent();
            //set source to imgSS
            imgSS.Source = source;
        }
    }
}
