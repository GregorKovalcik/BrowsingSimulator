//using Alea;
//using Alea.Parallel;
using DescriptorClustering;
using DescriptorClustering.Simple;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
            double seedPercentage = double.Parse(args[2]);
            //DescriptorKMeans kMeans = new DescriptorKMeans(LoadDescriptors(descriptorFile), seedPercentage);
            //DescriptorKMeans kMeans = new DescriptorKMeansGPU(LoadArrayDescriptors(descriptorFile), seedPercentage,
            //    DescriptorKMeansGPU.L2SQR_I, 1);
            //kMeans.Run(10);

            int seed = 5334;
            int windowSize = 720;

            int nClusters = 64;// 128;
            int iterationCount = 1;


            ClusteringSimple clustering = new ClusteringSimple(LoadArrayDescriptors(descriptorFile));

            clustering.Clusterize(nClusters, iterationCount, seed);

            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
        }

        private static Descriptor[] LoadArrayDescriptors(string descriptorFile)
        {
            using (FeatureReader featureReader = new FeatureReader(descriptorFile))
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


        private static float[][,] LoadDescriptors(string descriptorFile)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(descriptorFile, FileMode.Open)))
                {
                    int descriptorCount = reader.ReadInt32();
                    int signatureDimension = reader.ReadInt32();
                    float[][,] descriptors = new float[descriptorCount][,];
                    //Console.WriteLine("Loading {0} dimensional descriptors with {1}-dimensional signatures from {2}",
                    //   descriptorCount, signatureDimension, descriptorFile);

                    for (int i = 0; i < descriptorCount; i++)
                    {
                        int centroidCount = reader.ReadInt32();
                        descriptors[i] = new float[centroidCount, signatureDimension];
                        for (int j = 0; j < centroidCount; j++)
                        {
                            for (int k = 0; k < signatureDimension; k++)
                            {
                                descriptors[i][j, k] = reader.ReadSingle();
                            }
                        }
                    }
                    return descriptors;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading binary descriptor file: " + descriptorFile);
                throw;
            }
        }

    }
}
