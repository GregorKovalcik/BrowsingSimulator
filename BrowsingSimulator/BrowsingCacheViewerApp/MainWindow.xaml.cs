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

namespace BrowsingCacheViewerApp
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
        public string LoadedCacheFile { get; set; }
        public string LoadedThumbnailsFile { get; set; }
        public int DisplayId { get; set; }
        private BitmapReader.BitmapReader bitmapReader = null;
        private Tuple<int, int[]>[] cachedItems = null;


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

        
        private void LoadCacheFile(string cacheFilename)
        {
            if (cacheFilename == null || !File.Exists(cacheFilename))  // cache disabled
            {
                return;
            }

            List<Tuple<int, int[]>> items = new List<Tuple<int, int[]>>();
            using (BinaryReader reader = new BinaryReader(File.Open(cacheFilename,
                FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                int cacheSize = reader.ReadInt32();
                int cachedArrayLength = reader.ReadInt32();

                for (int i = 0; i < cacheSize; i++)
                {
                    int key = reader.ReadInt32();
                    int[] values = new int[cachedArrayLength];
                    for (int j = 0; j < cachedArrayLength; j++)
                    {
                        values[j] = reader.ReadInt32();
                    }
                    items.Add(new Tuple<int, int[]>(key, values));
                }
            }
            cachedItems = items.ToArray();
        }

        private void LoadCacheFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadedCacheFileTextBox.Text = "Loading...";
                    LoadCacheFile(openFileDialog.FileName);
                    LoadedCacheFileTextBox.Text = openFileDialog.FileName;
                }
                catch
                {
                    MessageBox.Show("Unable to load cache file!");
                    LoadedCacheFileTextBox.Text = "";
                    cachedItems = null;
                }
            }
            DisplayIdTextBox.Text = 0.ToString();
            ShowItem(0);
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
            ShowItem(0);
        }


        private void ShowItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (bitmapReader == null)
            {
                MessageBox.Show("No thumbnail file is loaded!");
                return;
            }
            if (cachedItems == null)
            {
                MessageBox.Show("No cache file is loaded!");
                return;
            }

            int itemId = -1;
            try
            {
                itemId = int.Parse(DisplayIdTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Unable to decode item ID: " + DisplayIdTextBox.Text);
                return;
            }

            if (itemId >= cachedItems.Length || itemId < 0)
            {
                MessageBox.Show("Display ID: " + itemId + " out of range! Correct range is [0, " + (cachedItems.Length - 1) + "].");
                return;
            }

            ShowItem(itemId);
        }

        private void ShowItem(int itemId)
        {
            if (bitmapReader != null && cachedItems != null)
            {
                Thumbnails.Clear();
                Tuple<int, int[]> item = cachedItems[itemId];
                int queryId = item.Item1;
                int[] displayedItems = item.Item2;

                Bitmap bitmap = bitmapReader.ReadFrame(queryId);
                MarkSelectedItem(bitmap);
                Thumbnails.Add(new Thumbnail(queryId.ToString(), BitmapToImageSource(bitmap)));

                foreach (int displayedId in displayedItems)
                {
                    // TODO verbose (videoID, shotID...)
                    bitmap = bitmapReader.ReadFrame(displayedId);
                    Thumbnails.Add(new Thumbnail(displayedId.ToString(), BitmapToImageSource(bitmap)));

                    if (Thumbnails.Count > int.Parse(MaxItemsTextBox.Text))
                    {
                        break;
                    }
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
            if (cachedItems != null)
            {
                if (e.Key == Key.Up)
                {
                    ModifyNumber(DisplayIdTextBox, x => (x + 1 < cachedItems.Length) ? x + 1 : cachedItems.Length - 1);
                }

                else if (e.Key == Key.Down)
                {
                    ModifyNumber(DisplayIdTextBox, x => (x - 1 >= 0) ? x - 1 : 0);
                }
            }
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
            ShowItem(clusterId);
        }

        private void MaxItemsTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void MaxItemsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
