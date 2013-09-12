using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorSpaceConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] allowedImageExtensions = new string[] { ".jpg", ".png" };
        string[] allowedProfileExtensions = new string[] { ".icc", ".icm" };

        BackgroundWorker worker;
        ProgressDialog pd;

        #region constructor
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region aux methods
        private void PrepareColorProfileList()
        {
            cmbProfiles.Items.Clear();
            string path = Assembly.GetExecutingAssembly().Location;
            var directory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "Color Profiles");

            DirectoryInfo di = new DirectoryInfo(directory);

            FileInfo[] allFiles = di.GetFiles("*.icc");

            foreach (FileInfo file in allFiles)
            {
                cmbProfiles.Items.Add(System.IO.Path.GetFileNameWithoutExtension(file.FullName));
            }

            if (cmbProfiles.Items.Count>0)
            {
                cmbProfiles.SelectedIndex = 0;
            }
        }

        private void FillImageListView()
        {
            lsbAllImages.Items.Clear();
            lsbSelectedImages.Items.Clear();

            DirectoryInfo di = new DirectoryInfo(lblFolderName.Text);

            foreach (FileInfo item in GetFilesByExtensions(di, allowedImageExtensions))
            {
                lsbAllImages.Items.Add(item);
            }
        }

        public IEnumerable<FileInfo> GetFilesByExtensions(DirectoryInfo di, string[] extensions)
        {
            var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
            return di.EnumerateFiles().Where(x => allowedExtensions.Contains(x.Extension));
        }

        ColorConvertedBitmap ConvertSingleImage(string sourceImagePath, ColorContext destinationContext)
        {
            try
            {
                Stream sourceImageStream = new FileStream(sourceImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                BitmapSource sourceBitmapSource = BitmapFrame.Create(sourceImageStream);

                // try to get source color context
                BitmapFrame sourceBitmapFrame = (BitmapFrame)sourceBitmapSource;

                ColorContext sourceColorContext = sourceBitmapFrame.ColorContexts == null ? new ColorContext(PixelFormats.Rgb24) : sourceBitmapFrame.ColorContexts[0];

                // If source color context is not available, set it to sRGB24
                

                ColorConvertedBitmap ccb = new ColorConvertedBitmap(sourceBitmapSource, sourceColorContext, destinationContext, PixelFormats.Cmyk32);

                return ccb;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        ColorContext GetSelectedColorProfile()
        {
            try
            {
                string path = Assembly.GetExecutingAssembly().Location;
                var directory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), "Color Profiles");
                string selectedProfileName = cmbProfiles.SelectedItem.ToString();
                DirectoryInfo di = new DirectoryInfo(directory);
                FileInfo fii = di.EnumerateFiles().Where(x => x.Name.Contains(selectedProfileName)).First();

                Uri fileUri = new Uri(fii.FullName, UriKind.Absolute);
                ColorContext finalContext = new ColorContext(fileUri);
                return finalContext;
            }
            catch
            {
                return null;
            }

        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblFolderName.Text = lblDestination.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            //lblFolderName.Content = @"C:\Users\Can\Pictures\wallpapers";
            PrepareColorProfileList();
            FillImageListView();

        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = lblFolderName.Text;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lblFolderName.Text = ((System.Windows.Forms.FolderBrowserDialog)dialog).SelectedPath;

                FillImageListView();
            }
        }

        private void btnAddImage_Click(object sender, RoutedEventArgs e)
        {
            foreach (var selectedItem in lsbAllImages.SelectedItems)
            {

                if (!lsbSelectedImages.Items.Contains(selectedItem))
                {
                    lsbSelectedImages.Items.Add(selectedItem); 
                }
            }
        }

        private void btnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (lsbSelectedImages.SelectedItems.Count>0)
            {
                lsbSelectedImages.Items.Remove(lsbSelectedImages.SelectedItems[0]);
            }
        }

        private void btnAddAllImages_Click(object sender, RoutedEventArgs e)
        {
            lsbSelectedImages.Items.Clear();
            foreach (var item in lsbAllImages.Items)
            {
                lsbSelectedImages.Items.Add(item);
            }
        }

        private void btnRemoveAllImages_Click(object sender, RoutedEventArgs e)
        {
            lsbSelectedImages.Items.Clear();
        }

        // delegate for updating progress UI
        public delegate void updateProgressDelegate(int percentage, int recordCount);

        // method that update delegate executes
        public void updateProgressText(int percentage, int recordCount)
        {
            pd.ProgressText = string.Format("{0}% of {1} Images", percentage.ToString(), recordCount.ToString());
            pd.ProgressValue = percentage;
        }

        //method that cancels progress
        void CancelProcess(object sender, EventArgs e)
        {
            worker.CancelAsync();
        }   

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {

            pd = new ProgressDialog();
            pd.Owner = this;

            // Hooking into the cancel event of child form
            pd.Cancel +=CancelProcess;

            // Get dispatcher
            System.Windows.Threading.Dispatcher pdDispatcher = pd.Dispatcher;

            // Create background worker and support cancellation
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            int totalCount = lsbSelectedImages.Items.Count;
            pd.ProgressText = string.Format("0% of {0} Images",  totalCount.ToString());

            var allFileInfos = lsbSelectedImages.Items;
            FileInfo currentInfo;
            ColorContext selectedContext = GetSelectedColorProfile();
            ColorConvertedBitmap currentConvertedImage;
            string destFolder = lblDestination.Text;


            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                for (int i = 0; i < totalCount; i++)
                {
                    if (worker.CancellationPending)
                    {
                        args.Cancel = true;
                        return;
                    }

                    // Do real work here
                    currentInfo = (FileInfo)allFileInfos[i];
                    currentConvertedImage = ConvertSingleImage(currentInfo.FullName, selectedContext);
                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                    encoder.Compression = TiffCompressOption.Zip;
                    FileStream fs = new FileStream(System.IO.Path.Combine(destFolder, System.IO.Path.GetFileNameWithoutExtension(currentInfo.FullName) + "_cmyk.tif"), FileMode.Create,FileAccess.Write,FileShare.Write);

                    encoder.Frames.Add(BitmapFrame.Create(currentConvertedImage));
                    encoder.Save(fs);

                    fs.Close();

                    // Creating a new delegate for updating progress
                    updateProgressDelegate update = new updateProgressDelegate(updateProgressText);

                    // Invoke dispatcher and pass percentage & max number of work
                    pdDispatcher.BeginInvoke(update, Convert.ToInt32(((decimal)i / (decimal)totalCount * 100)), totalCount);
                }
            };

            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs arg)
            {
                pd.Close();
            };

            // Running the progress and showing the progress dialog
            worker.RunWorkerAsync();
            pd.ShowDialog();
        }
        private void preview_Click(object sender, RoutedEventArgs e)
        {
            if (lsbAllImages.SelectedItems.Count>0)
            {
                previewer previewForm = new previewer();
                string sourcePath = ((FileInfo)(lsbAllImages.SelectedItems[lsbAllImages.SelectedItems.Count - 1])).FullName;
                previewForm.SourcePath =sourcePath;

                ColorContext destColorContext = GetSelectedColorProfile();
                ColorConvertedBitmap ccbPreview = ConvertSingleImage(sourcePath, destColorContext);
                previewForm.destinationImage = ccbPreview;

                string titleText = "Preview - File: " + ((FileInfo)(lsbAllImages.SelectedItems[lsbAllImages.SelectedItems.Count - 1])).Name + " Profile: " + cmbProfiles.SelectedItem.ToString();
                previewForm.TitleText = titleText;

                previewForm.ShowDialog(); 
            }
        }

        //Prevent right click to select & unselect items 
        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void btnSameAsSource_Click(object sender, RoutedEventArgs e)
        {
            lblDestination.Text = lblFolderName.Text;
        }

        private void btnSelectDestination_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = lblDestination.Text;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lblDestination.Text = ((System.Windows.Forms.FolderBrowserDialog)dialog).SelectedPath;
            }
        }

        private void btnAddProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select color profile file";
            ofd.Filter = "ICC Profile|*.icc|ICM Profile|*.icm";
            Nullable<bool> result = ofd.ShowDialog();


            if (result == true)
            {
                string sourceFilePath = ofd.FileName;
                string destFolderPath= System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Color Profiles");
                string destFilePath = System.IO.Path.Combine(destFolderPath, System.IO.Path.GetFileName(sourceFilePath));

                File.Copy(sourceFilePath, destFilePath, true);

                PrepareColorProfileList();
            }
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            about frmAbout = new about();
            frmAbout.Owner = this;
            frmAbout.ShowDialog();
        }

    }
}
