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
using System.Windows.Shapes;

namespace ColorSpaceConverter
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public string ProgressText
        {
            set
            {
                lblProgress.Text = value;
            }
        }

        public int ProgressValue
        {
            set
            {
                progress.Value = value;
            }
        }

        public event EventHandler Cancel = delegate { };

        public ProgressDialog()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Cancel(sender, e);
        }


    }
}
