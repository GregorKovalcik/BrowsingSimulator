using ColorTransform;
using Extractor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDirectory = args[0];
            string inputFilelist = args[1];
            string outputFile = args[2];
            int resizeWidth = int.Parse(args[3]);
            int resizeHeight = int.Parse(args[4]);

            if (resizeWidth != resizeHeight)
            {
                throw new NotImplementedException();
            }

           
            // load filelist
            List<string> filelist = new List<string>();
            using (StreamReader reader = new StreamReader(inputFilelist))
            {
                while (!reader.EndOfStream)
                {
                    filelist.Add(inputDirectory + reader.ReadLine());
                }
            }

            ResizeExtractor extractor = new ResizeExtractor();

            // compute and write the result
            //const int batchSize = 1000;
            using (BinaryWriter writer = new BinaryWriter(new FileStream(outputFile, FileMode.Create)))
            {
                writer.Write(filelist.Count);                   // feature count
                writer.Write(resizeWidth * resizeHeight * 3);   // feature dimension

                for (int i = 0; i < filelist.Count; i++)
                {
                    Bitmap inputImage = new Bitmap(filelist[i]);
                    Bitmap resizedImage = ResizeExtractor.ResizeImage(inputImage, resizeWidth, resizeHeight);

                    //writer.Write(resizeWidth * resizeHeight);           // centroid count
                    for (int iRow = 0; iRow < resizeHeight; iRow++)     // each centroid
                    {
                        for (int iCol = 0; iCol < resizeWidth; iCol++)
                        {
                            Color color = resizedImage.GetPixel(iRow, iCol);
                            CIELab lab = ColorTransform.ColorTransform.RGBtoLab(color.R, color.G, color.B);
                            //writer.Write((float)1);                                 // weight in [0, 1]
                            //writer.Write((float)gaussKernel[iRow, iCol]);
                            //writer.Write((float)((iCol + 0.5) / resizeWidth));      // XY in [0, 1]
                            //writer.Write((float)((iRow + 0.5) / resizeHeight));
                            writer.Write((float)(lab.L / lab.lMaxValue));           // Lab in [-1, 1]
                            writer.Write((float)(lab.A / lab.aMaxValueAbs));
                            writer.Write((float)(lab.B / lab.bMaxValueAbs));
                            //writer.Write((float)0);
                            //writer.Write((float)0);
                        }
                    }

                    if ((i + 1) % 100 == 0)
                    {
                        Console.WriteLine("Extracted " + (i + 1) + "/" + filelist.Count + " signatures.");
                    }
                }
            }
        }
    }
}
