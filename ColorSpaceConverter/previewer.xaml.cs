using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace ColorSpaceConverter
{
    /// <summary>
    /// Interaction logic for previewer.xaml
    /// </summary>
    public partial class previewer : Window
    {
        public string SourcePath
        {
            set
            {
                imgSource.Source = new BitmapImage(new Uri(value, UriKind.Absolute));
            }
        }
        public ColorConvertedBitmap destinationImage
        {
            set
            {

                imgDestination.Source = value;
                
            }
        }
        public string TitleText
        {
            set
            {
                this.Title = value;
            }
        }



        public previewer()
        {
            InitializeComponent();
        }
    }
}
