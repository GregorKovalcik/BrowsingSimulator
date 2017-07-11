using DescriptorClustering;
using DescriptorClustering.Hierarchical.Divisive;
using DescriptorClustering.Simple;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClustering;

namespace DescriptorClusteringCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;


            string descriptorFile = args[0];
            string outputFile = args[1];
            int iterationCount = int.Parse(args[2]);

            int argumentOffset = 3;
            int[] seedCounts = new int[args.Length - argumentOffset];
            int[] iterationCounts = new int[args.Length - argumentOffset];
            for (int i = argumentOffset; i < args.Length; i++)
            {
                int seedIndex = i - argumentOffset;
                seedCounts[seedIndex] = int.Parse(args[i]); // TODO: try-catch
                iterationCounts[seedIndex] = iterationCount;
            }

            int seed = 5334;
            Descriptor[] descriptors = LoadArrayDescriptors(descriptorFile);

            // debug
            //Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(seed, 10000, 2);
            //seedCounts = new int[] { 10, 10 };

            ClusteringDivisive clustering = new ClusteringDivisive(descriptors);
            Console.WriteLine("Launching clusterization.");

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            clustering.Clusterize(seedCounts, iterationCounts, seed);
            stopWatch.Stop();

            Console.WriteLine("Clusterization finished.");
            int elapsedSeconds = (int)(stopWatch.ElapsedMilliseconds / 1000);
            int hours = (elapsedSeconds / (60 * 60));
            int secondsRemainder = (elapsedSeconds % (60 * 60));
            int minutes = (secondsRemainder / 60);
            int seconds = (secondsRemainder % 60);
            Console.WriteLine("Computing time: {0} hours, {1} minutes, {2} seconds.", hours, minutes, seconds);


            WriteToTextFile(clustering.Centroids[clustering.Centroids.Length - 1], outputFile);

            //int imageSize = 4096;
            //HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, imageSize, imageSize);
            //HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, imageSize * 2, imageSize * 2);
            //HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, imageSize * 4, imageSize * 4);
            //HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, imageSize * 8, imageSize * 8);
        }

        private static Descriptor[] LoadArrayDescriptors(string descriptorFile)
        {
            using (FeatureReader featureReader = new FeatureReader(descriptorFile, false))
            {
                Console.WriteLine("Loading {0} descriptors ({1} dimensions).", featureReader.FeatureCount, featureReader.FeatureDimension);
                Descriptor[] descriptors = new Descriptor[featureReader.FeatureCount];
                for (int i = 0; i < featureReader.FeatureCount; i++)
                {
                    float[] features = featureReader.GetFeatures(i);    // TODO: optimize
                    descriptors[i] = new Descriptor(i, features);
                }
                return descriptors;
            }
        }


        private static void WriteToTextFile(Centroid[] centroids, string textOutputFile)
        {
            Console.WriteLine("Writing to text file: {0}", textOutputFile);
            using (StreamWriter writer = new StreamWriter(textOutputFile))
            {
                foreach (Centroid centroid in centroids)
                {
                    writer.Write(centroid.Id + ":" + centroid.Mean.Id + ":");
                    bool isFirst = true;
                    foreach (Descriptor descriptor in centroid.Descriptors)
                    {
                        if (isFirst)
                        {
                            writer.Write(descriptor.Id);
                            isFirst = false;
                        }
                        else
                        {
                            writer.Write(";" + descriptor.Id);
                        }
                    }
                    writer.WriteLine();
                }
            }
        }
    }
}
