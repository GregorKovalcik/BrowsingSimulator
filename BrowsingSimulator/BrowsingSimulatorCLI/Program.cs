using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrowsingSimulator;
using DescriptorClusteringCLI;
using System.IO;

namespace BrowsingSimulatorCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            string descriptorFile = args[0];
            string clusteringFileL0 = args[1];
            string clusteringFileL1 = args[2];
            string outputFile = args[3];

            float[][] descriptors = LoadDescriptorFile(descriptorFile);
            Tuple<int, int[]>[][] clustering = Load3LayerClusteringFiles(clusteringFileL0, clusteringFileL1);

            MLES mles = new MLES(descriptors, clustering);
            BrowsingSimulatorEngine simulator = new BrowsingSimulatorEngine(mles);
            simulator.RunSimulations(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 96, 1, 1);

        }



        public static float[][] LoadDescriptorFile(string descriptorFile)
        {
            using (FeatureReader featureReader = new FeatureReader(descriptorFile, true/*false*/))
            {
                Console.WriteLine("Loading {0} descriptors ({1} dimensions).", featureReader.FeatureCount, featureReader.FeatureDimension);
                float[][] descriptors = new float[featureReader.FeatureCount][];
                for (int i = 0; i < featureReader.FeatureCount; i++)
                {
                    descriptors[i] = featureReader.GetFeatures(i);
                }
                return descriptors;
            }
        }

        public static Tuple<int, int[]>[][] Load3LayerClusteringFiles(string clusteringFileL0, string clusteringFileL1)
        {
            Tuple<int, int[]>[][] result = new Tuple<int, int[]>[2][];
            using (StreamReader reader = new StreamReader(clusteringFileL0))
            {
                List<Tuple<int, int[]>> resultL0 = new List<Tuple<int, int[]>>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(':');
                    int clusterId = int.Parse(tokens[0]);
                    int descriptorId = int.Parse(tokens[1]);
                    string clusterItems = tokens[2];

                    tokens = clusterItems.Split(';');
                    int[] clusterItemIds = new int[tokens.Length];
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        clusterItemIds[i] = int.Parse(tokens[i]);
                    }
                    resultL0.Add(new Tuple<int, int[]>(descriptorId, clusterItemIds));
                }
                result[0] = resultL0.ToArray();
            }

            using (StreamReader reader = new StreamReader(clusteringFileL1))
            {
                List<Tuple<int, int[]>> resultL1 = new List<Tuple<int, int[]>>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(':');
                    int clusterId = int.Parse(tokens[0]);
                    int descriptorId = int.Parse(tokens[1]);
                    string clusterItems = tokens[2];

                    tokens = clusterItems.Split(';');
                    int[] clusterItemIds = new int[tokens.Length];
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        clusterItemIds[i] = int.Parse(tokens[i]);
                    }
                    resultL1.Add(new Tuple<int, int[]>(descriptorId, clusterItemIds));
                }
                result[1] = resultL1.ToArray();
            }

            return result;
        }
    }
}
