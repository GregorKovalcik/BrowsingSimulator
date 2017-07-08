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

namespace ClusteringBrowserApp
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
        public string LoadedClusteringFile { get; set; }
        public string LoadedThumbnailsFile { get; set; }
        public int ClusterId { get; set; }
        public int MaxImages { get; set; }
        private int[][] clusters = null;
        private BitmapReader.BitmapReader bitmapReader = null;

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

        private void LoadClusteringFile(string fileName)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                List<int[]> clusterDescriptorsIds = new List<int[]>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(':');
                    int clusterId = int.Parse(tokens[0]);
                    int descriptorId = int.Parse(tokens[1]);
                    string descriptors = tokens[2];

                    tokens = descriptors.Split(';');
                    int[] descriptorIds = new int[tokens.Length];
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        descriptorIds[i] = int.Parse(tokens[i]);
                    }
                    clusterDescriptorsIds.Add(descriptorIds);
                }
                clusters = clusterDescriptorsIds.ToArray();

                ClusterIdTextBox.Text = 0.ToString();
                ShowCluster(0);
            }
        }

        private void LoadClusteringFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadedClusteringFileTextBox.Text = "Loading...";
                    LoadClusteringFile(openFileDialog.FileName);
                    LoadedClusteringFileTextBox.Text = openFileDialog.FileName;
                }
                catch
                {
                    MessageBox.Show("Unable to load clustering file!");
                    LoadedClusteringFileTextBox.Text = "";
                }
            }
            ShowCluster(0);
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
            ShowCluster(0);
        }

        private void ShowCluster(int clusterId)
        {
            if (bitmapReader != null && clusters != null)
            {
                Thumbnails.Clear();
                foreach (int descriptorId in clusters[clusterId])
                {
                    // TODO verbose (videoID, shotID...)
                    Bitmap bitmap = bitmapReader.ReadFrame(descriptorId);
                    Thumbnails.Add(new Thumbnail(descriptorId.ToString(), BitmapToImageSource(bitmap)));
                }
            }
        }

        private void ShowClusterButton_Click(object sender, RoutedEventArgs e)
        {
            if (bitmapReader == null)
            {
                MessageBox.Show("No thumbnail file is loaded!");
                return;
            }
            if (clusters == null)
            {
                MessageBox.Show("No clustering file is loaded!");
                return;
            }

            int clusterId = -1;
            try
            {
                clusterId = int.Parse(ClusterIdTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Unable to decode cluster ID: " + ClusterIdTextBox.Text);
                return;
            }

            if (clusterId >= clusters.Length || clusterId < 0)
            {
                MessageBox.Show("Cluster ID: " + clusterId + " out of range! Correct range is [0, " + (clusters.Length - 1) + "].");
                return;
            }

            ShowCluster(clusterId);
        }

        private void MaxImagesTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                MaxImages = int.Parse(MaxImagesTextBox.Text);
            }
            catch
            {
                MaxImages = 100;
                MaxImagesTextBox.Text = 100.ToString();
            }
        }

        private void ClusterIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int clusterId = -1;
            try
            {
                clusterId = int.Parse(ClusterIdTextBox.Text);
            }
            catch
            {
                ClusterIdTextBox.Text = 0.ToString();
                clusterId = 0;
            }
            ShowCluster(clusterId);
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

        private void ClusterIdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (clusters != null)
            {
                if (e.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    ModifyNumber(ClusterIdTextBox, x => (x + 100 < clusters.Length) ? x + 100 : 0);
                }
                else if (e.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    ModifyNumber(ClusterIdTextBox, x => (x + 10 < clusters.Length) ? x + 10 : 0);
                }
                else if (e.Key == Key.Up)
                {
                    ModifyNumber(ClusterIdTextBox, x => (x + 1 < clusters.Length) ? x + 1 : 0);
                }

                else if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    ModifyNumber(ClusterIdTextBox, x => (x - 100 >= 0) ? x - 100 : clusters.Length - 1);
                }
                else if (e.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    ModifyNumber(ClusterIdTextBox, x => (x - 10 >= 0) ? x - 10 : clusters.Length - 1);
                }
                else if (e.Key == Key.Down)
                {
                    ModifyNumber(ClusterIdTextBox, x => (x - 1 >= 0) ? x - 1 : clusters.Length - 1);
                }
            }
        }
    }
}
