using DescriptorClustering;
using System;
using System.Collections.Generic;
using System.Drawing;
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


        public static void VisualizeClustering(Descriptor[] descriptors, Centroid[] centroids, int width, int height)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.White);

                foreach (Descriptor d in descriptors)
                {
                    float[] v = d.Values;
                    g.DrawEllipse(System.Drawing.Pens.Black, v[0] * bmp.Width - 1, v[1] * bmp.Height - 1, 2, 2);
                }

                foreach (Centroid c in centroids)
                {
                    if (c.Mean == null) continue;
                    g.DrawEllipse(System.Drawing.Pens.Red, c.Mean.Values[0] * bmp.Width - 4, c.Mean.Values[1] * bmp.Height - 4, 8, 8);
                    g.DrawEllipse(System.Drawing.Pens.Red, c.Mean.Values[0] * bmp.Width - 3, c.Mean.Values[1] * bmp.Height - 3, 6, 6);
                }

            }

            Form form = new Form();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);
            form.Width = width;
            form.Height = height;
            form.Text = "Image Viewer";
            PictureBox pictureBox = new PictureBox();
            pictureBox.Image = bmp;
            pictureBox.Dock = DockStyle.Fill;
            form.Controls.Add(pictureBox);
            Application.Run(form);

            //drawCanvas.CreateGraphics().DrawImage(bmp, 0, 0);
            //bmp.Dispose();
        }
    }
}
