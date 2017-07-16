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
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            string descriptorFile = args[0];
            string clusteringFileL0 = args[1];
            string clusteringFileL1 = args[2];
            string queryIdsFile = args[3];
            int[] zoomingSteps = ParseIntArray(args[4]);
            int[] browsingCoherences = ParseIntArray(args[5]);
            float[] dropFactors = ParseFloatArray(args[6]);
            string outputDirectory = args[7];
            string cacheFilename = null;
            if (args.Length > 8) { cacheFilename = args[8]; }
            
            float[][] descriptors = LoadDescriptorFile(descriptorFile);
            Tuple<int, int[]>[][] clustering = Load3LayerClusteringFiles(clusteringFileL0, clusteringFileL1);
            int[] queryIds = LoadQueryIds(queryIdsFile);
            MLES mles = new MLES(descriptors, clustering, cacheFilename);
            BrowsingSimulatorEngine simulator = new BrowsingSimulatorEngine(mles);

            for (int iZoom = 0; iZoom < zoomingSteps.Length; iZoom++)
                for (int iCoherence = 0; iCoherence < browsingCoherences.Length; iCoherence++)
                    for (int iDrop = 0; iDrop < dropFactors.Length; iDrop++)
                    {
                        simulator.RunSimulations(queryIds, simulator.Mles.Layers[0].Length, 
                            zoomingSteps[iZoom], browsingCoherences[iCoherence], dropFactors[iDrop]);

                        string directory = "zoom" + zoomingSteps[iZoom]
                            + "_coherence" + browsingCoherences[iCoherence]
                            + "_drop" + dropFactors[iDrop];
                        string directoryPath = Path.Combine(outputDirectory, directory);

                        simulator.SaveSessionLogs(directoryPath);
                        mles.SaveCache();
                    }

            //simulator.RunSimulations(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 24, 1, 1);
            mles.Dispose();
        }

        private static float[][] LoadDescriptorFile(string descriptorFile)
        {
            using (FeatureReader featureReader = new FeatureReader(descriptorFile, false))
            //using (FeatureReader featureReader = new FeatureReader(descriptorFile, true))
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

        private static Tuple<int, int[]>[][] Load3LayerClusteringFiles(string clusteringFileL0, string clusteringFileL1)
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

        private static int[] ParseIntArray(string commaSeparatedIntArray)
        {
            string[] tokens = commaSeparatedIntArray.Split(',');
            int[] intArray = new int[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                intArray[i] = int.Parse(tokens[i]);
            }
            return intArray;
        }

        private static float[] ParseFloatArray(string commaSeparatedIntArray)
        {
            string[] tokens = commaSeparatedIntArray.Split(',');
            float[] floatArray = new float[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                floatArray[i] = int.Parse(tokens[i]);
            }
            return floatArray;
        }

        private static int[] LoadQueryIds(string queryIdsFilename)
        {
            List<int> queryIds = new List<int>();
            using (StreamReader reader = new StreamReader(queryIdsFilename))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    queryIds.Add(int.Parse(line));
                }
            }
            return queryIds.ToArray();
        }

    }
}
