using DescriptorClustering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestClustering
{
    public class HelperTestClass
    {
        public static Descriptor[] GenerateHierarchicalDescriptors(int seed, int nDescriptorsMultiplier, int dimension)
        {
            int nDescriptors = nDescriptorsMultiplier * 5;
            Descriptor[] descriptors = new Descriptor[nDescriptors];
            Random random = new Random(seed);

            Console.WriteLine("Generating {0} descriptors ({1} dimensions).", nDescriptors, dimension);
            for (int i = 0; i < nDescriptors; i += 5)
            {
                descriptors[i] = new Descriptor(i, GetRandomVector(random, dimension, 1));
                descriptors[i + 1] = new Descriptor(i + 1, GetRandomVector(random, dimension, 4 / 5.0));
                descriptors[i + 2] = new Descriptor(i + 2, GetRandomVector(random, dimension, 3 / 5.0));
                descriptors[i + 3] = new Descriptor(i + 3, GetRandomVector(random, dimension, 2 / 5.0));
                descriptors[i + 4] = new Descriptor(i + 4, GetRandomVector(random, dimension, 1 / 5.0));
            }

            return descriptors;
        }

        private static float[] GetRandomVector(Random random, int dimension, double scale)
        {
            float[] vector = new float[dimension];
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] = (float)(random.NextDouble() * scale);
            }
            return vector;
        }

        public static void TestDescriptorAssignment(int nDescriptors, Centroid[] centroids)
        {
            bool[] isAssigned = Enumerable.Repeat(false, nDescriptors).ToArray();

            // catch duplicate assignments
            foreach (Centroid centroid in centroids)
            {
                foreach (Descriptor descriptor in centroid.Descriptors)
                {
                    Assert.IsFalse(isAssigned[descriptor.Id], "Duplicate assignment detected!");
                    isAssigned[descriptor.Id] = true;
                }
            }

            // catch not assigned
            foreach (bool assignment in isAssigned)
            {
                Assert.IsTrue(assignment, "Descriptor without assignment detected!");
            }

            // catch duplicate centroids
            HashSet<int> hashSet = new HashSet<int>();
            foreach (Centroid centroid in centroids)
            {
                Assert.IsTrue(!hashSet.Contains(centroid.Id));
                hashSet.Add(centroid.Id);
            }
        }


        public static void VisualizeClustering(Descriptor[] descriptors, Centroid[] centroids, int width, int height)
        {
            Centroid[][] layeredCentroids = new Centroid[][] { centroids };
            VisualizeClustering(descriptors, layeredCentroids, width, height);
        }

        public static void VisualizeClustering(Descriptor[] descriptors, Centroid[][] centroidLayers, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                // draw centroid - descriptor lines
                for (int i = centroidLayers.Length - 1; i >= 0; i--)
                {
                    Centroid[] centroidLayer = centroidLayers[(centroidLayers.Length - 1) - i];
                    //Color color = Color.Blue;
                    //color = (i % 3 == 0) ? Color.Red : color;
                    //color = (i % 3 == 1) ? Color.Lime : color;
                    //color = (i % 3 == 2) ? Color.Blue : color;
                    Color color = Color.Gray;
                    Pen centroidLinePen = new Pen(color);
                    centroidLinePen.Width = 1;
                    foreach (Centroid c in centroidLayer)
                    {
                        foreach (Descriptor d in c.Descriptors)
                        {
                            g.DrawLine(centroidLinePen,
                                   c.Mean.Values[0] * bmp.Width, c.Mean.Values[1] * bmp.Height,
                                   d.Values[0] * bmp.Width, d.Values[1] * bmp.Height);
                        }
                    }
                }

                // draw points
                foreach (Descriptor d in descriptors)
                {
                    float[] v = d.Values;
                    g.DrawEllipse(Pens.Black, v[0] * bmp.Width - 1, v[1] * bmp.Height - 1, 2, 2);
                }

                // draw centroids
                for (int i = 0; i < centroidLayers.Length; i++)
                {
                    Centroid[] centroidLayer = centroidLayers[(centroidLayers.Length - 1) - i];
                    Color color = Color.Blue;
                    color = (i % 3 == 0) ? Color.Red : color;
                    color = (i % 3 == 1) ? Color.Lime : color;
                    color = (i % 3 == 2) ? Color.Blue : color;
                    Pen centroidPen = new Pen(color);
                    centroidPen.Width = 2 + i * 2;
                    
                    foreach (Centroid c in centroidLayer)
                    {
                        if (c.Mean == null) continue;
                        int elipseRadius = 3 + i * 2;
                        g.DrawEllipse(centroidPen,
                            c.Mean.Values[0] * bmp.Width - elipseRadius,
                            c.Mean.Values[1] * bmp.Height - elipseRadius,
                            elipseRadius * 2, elipseRadius * 2);
                    }
                }
            }
            ShowInPictureBox(bmp);
        }

        public static void SaveClustering(Descriptor[] descriptors, Centroid[][] centroidLayers, int width, int height, string filename)
        {
            for (int iLayer = 0; iLayer < centroidLayers.Length; iLayer++)
            {
                Centroid[] centroidLayer = centroidLayers[(centroidLayers.Length - 1) - iLayer];

                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.White);

                    // draw centroid - descriptor lines
                    Color color = Color.Gray;
                    Pen centroidLinePen = new Pen(color);
                    centroidLinePen.Width = 1;
                    foreach (Centroid c in centroidLayer)
                    {
                        foreach (Descriptor d in c.Descriptors)
                        {
                            g.DrawLine(centroidLinePen,
                                    c.Mean.Values[0] * bmp.Width, c.Mean.Values[1] * bmp.Height,
                                    d.Values[0] * bmp.Width, d.Values[1] * bmp.Height);
                        }
                    }
                    

                    // draw points
                    foreach (Descriptor d in descriptors)
                    {
                        float[] v = d.Values;
                        g.DrawEllipse(Pens.Black, v[0] * bmp.Width - 1, v[1] * bmp.Height - 1, 2, 2);
                    }

                    // draw centroids
                    
                    color = Color.Red;
                    Pen centroidPen = new Pen(color);
                    centroidPen.Width = 2;

                    foreach (Centroid c in centroidLayer)
                    {
                        if (c.Mean == null) continue;
                        int elipseRadius = 3;
                        g.DrawEllipse(centroidPen,
                            c.Mean.Values[0] * bmp.Width - elipseRadius,
                            c.Mean.Values[1] * bmp.Height - elipseRadius,
                            elipseRadius * 2, elipseRadius * 2);
                    }
                    
                }

                bmp.Save(filename + "_" + ((centroidLayers.Length - 1) - iLayer) + ".png");
                bmp.Dispose();
            }
        }


        public static void ShowInPictureBox(Bitmap bitmap)
        {
            Form form = new Form();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);
            form.Width = bitmap.Width;
            form.Height = bitmap.Height;
            form.Text = "Image Viewer";

            PictureBox pictureBox = new PictureBox();
            pictureBox.Image = bitmap;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.MouseClick += new MouseEventHandler(pictureBoxClickEventHandler);

            form.Controls.Add(pictureBox);
            Application.Run(form);
        }

        private static void pictureBoxClickEventHandler(object sender, EventArgs e)
        {
            PictureBox pictureBox = (PictureBox)sender;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = "png";
            saveDialog.Filter = "Images|*.png;*.bmp;*.jpg|Bitmap Image (.bmp)|*.bmp|Gif Image (.gif)|*.gif|JPEG Image (.jpeg)|*.jpeg|PNG Image (.png)|*.png";
            ImageFormat format = ImageFormat.Png;
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = System.IO.Path.GetExtension(saveDialog.FileName);
                switch (ext)
                {
                    case ".jpg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;
                    case ".gif":
                        format = ImageFormat.Gif;
                        break;
                }
                pictureBox.Image.Save(saveDialog.FileName, format);
            }
        }
    }
}
