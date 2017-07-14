using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SessionLogViewerApp
{
    public class Thumbnail
    {
        public string Title { get; set; }
        public ImageSource Image { get; set; }

        public Thumbnail(string title, ImageSource image)
        {
            Title = title;
            Image = image;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Thumbnail> Thumbnails { get; set; }
        public string LoadedBrowsingLogFile { get; set; }
        public string LoadedThumbnailsFile { get; set; }
        public int DisplayId { get; set; }
        private BitmapReader.BitmapReader bitmapReader = null;
        private Tuple<int, int[]>[] log = null;

        public MainWindow()
        {
            Thumbnails = new ObservableCollection<Thumbnail>();

            int imageCount = 100;
            int imageSize = 20;
            Bitmap bitmapWhite = new Bitmap(1, 1);
            bitmapWhite.SetPixel(0, 0, System.Drawing.Color.Red);
            bitmapWhite = new Bitmap(bitmapWhite, imageSize + 10, imageSize);
            Bitmap bitmapBlack = new Bitmap(1, 1);
            bitmapBlack.SetPixel(0, 0, System.Drawing.Color.Black);
            bitmapBlack = new Bitmap(bitmapBlack, imageSize, imageSize + 10);

            for (int i = 0; i < imageCount; i++)
            {
                if (i % 2 == 0)
                {
                    Thumbnails.Add(new Thumbnail(i.ToString(), BitmapToImageSource(bitmapWhite)));
                }
                else
                {
                    Thumbnails.Add(new Thumbnail(i.ToString(), BitmapToImageSource(bitmapBlack)));
                }
            }


            InitializeComponent();
            DataContext = this;
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void LoadBrowsingLogFile(string fileName)
        {
            List<Tuple<int, int[]>> result = new List<Tuple<int, int[]>>();
            using (StreamReader reader = new StreamReader(fileName))
            {
                string headerLine = reader.ReadLine();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(';');

                    int selectedItemId = int.Parse(tokens[3].Split(':')[1]);
                    string[] displayTokens = tokens[6].Split(':');
                    int[] displayedIds = new int[displayTokens.Length - 1];
                    for (int i = 1; i < displayTokens.Length; i++)
                    {
                        displayedIds[i - 1] = int.Parse(displayTokens[i]);
                    }
                    result.Add(new Tuple<int, int[]>(selectedItemId, displayedIds));
                }
            }
            log = result.ToArray();
        }

        private void LoadLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadedBrowsingLogFileTextBox.Text = "Loading...";
                    LoadBrowsingLogFile(openFileDialog.FileName);
                    LoadedBrowsingLogFileTextBox.Text = openFileDialog.FileName;
                }
                catch
                {
                    MessageBox.Show("Unable to load browsing log file!");
                    LoadedBrowsingLogFileTextBox.Text = "";
                }
            }
            DisplayIdTextBox.Text = 0.ToString();
            ShowDisplay(0);
        }

        private void LoadThumbnailsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadedThumbnailsFileTextBox.Text = "Loading...";
                    bitmapReader = new BitmapReader.BitmapReader(openFileDialog.FileName);
                    LoadedThumbnailsFileTextBox.Text = openFileDialog.FileName;
                }
                catch
                {
                    MessageBox.Show("Unable to load thumbnails file!");
                    bitmapReader = null;
                    LoadedThumbnailsFileTextBox.Text = "";
                }
            }
            DisplayIdTextBox.Text = 0.ToString();
            ShowDisplay(0);
        }

        private void ShowDisplay(int displayId)
        {
            if (bitmapReader != null && log != null)
            {
                Thumbnails.Clear();
                int selectedItemId = log[displayId].Item1;
                int[] displayedItems = log[displayId].Item2;
                foreach (int displayedId in displayedItems)
                {
                    // TODO verbose (videoID, shotID...)
                    Bitmap bitmap = bitmapReader.ReadFrame(displayedId);
                    if (selectedItemId == displayedId) { MarkSelectedItem(bitmap); }
                    Thumbnails.Add(new Thumbnail(displayedId.ToString(), BitmapToImageSource(bitmap)));
                }
            }
        }

        private void MarkSelectedItem(Bitmap bitmap)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                System.Drawing.Color color = System.Drawing.Color.Yellow;
                System.Drawing.Pen pen = new System.Drawing.Pen(color);
                pen.Width = 5;
                g.DrawRectangle(pen, new System.Drawing.Rectangle(0, 0, bitmap.Width - 1, bitmap.Height - 1));
            }
        }

        private void ShowDisplayButton_Click(object sender, RoutedEventArgs e)
        {
            if (bitmapReader == null)
            {
                MessageBox.Show("No thumbnail file is loaded!");
                return;
            }
            if (log == null)
            {
                MessageBox.Show("No browsing log file is loaded!");
                return;
            }

            int displayId = -1;
            try
            {
                displayId = int.Parse(DisplayIdTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Unable to decode display ID: " + DisplayIdTextBox.Text);
                return;
            }

            if (displayId >= log.Length || displayId < 0)
            {
                MessageBox.Show("Display ID: " + displayId + " out of range! Correct range is [0, " + (log.Length - 1) + "].");
                return;
            }

            ShowDisplay(displayId);
        }        

        private void DisplayIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int clusterId = -1;
            try
            {
                clusterId = int.Parse(DisplayIdTextBox.Text);
            }
            catch
            {
                DisplayIdTextBox.Text = 0.ToString();
                clusterId = 0;
            }
            ShowDisplay(clusterId);
        }

        private void ModifyNumber(TextBox numberTextBox, Func<int, int> modifier)
        {
            int clusterId = -1;
            try
            {
                clusterId = int.Parse(numberTextBox.Text);
            }
            catch
            {
                return;
            }

            int modifiedId = modifier(clusterId);
            numberTextBox.Text = modifiedId.ToString();
        }

        private void DisplayIdTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (log != null)
            {
                if (e.Key == Key.Up)
                {
                    ModifyNumber(DisplayIdTextBox, x => (x + 1 < log.Length) ? x + 1 : log.Length - 1);
                }

                else if (e.Key == Key.Down)
                {
                    ModifyNumber(DisplayIdTextBox, x => (x - 1 >= 0) ? x - 1 : 0);
                }
            }
        }
    }
}
