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
            //string queryIdsFile = args[1];
            string classMappingFile = args[1];
            int[] zoomingSteps = ParseIntArray(args[2]);
            int[] browsingCoherences = ParseIntArray(args[3]);
            float[] dropFactors = ParseFloatArray(args[4]);
            string outputDirectory = args[5];
            string cacheFilename = null;

            cacheFilename = args[6];

            List<string> mlesLayers = new List<string>();
            for (int i = 7; i < args.Length; i++)
            {
                mlesLayers.Add(args[i]);
            }


            float[][] descriptors = LoadDescriptorFile(descriptorFile);

            int[] classMapping = LoadClassMapping(classMappingFile);

            Tuple<int, int[]>[][] clustering = LoadMlesLayers(mlesLayers);


            //int[] queryIds = LoadQueryIds(queryIdsFile);
            MLES mles = new MLES(descriptors, classMapping, clustering, cacheFilename);
            BrowsingSimulatorEngine simulator = new BrowsingSimulatorEngine(mles);

            for (int iZoom = 0; iZoom < zoomingSteps.Length; iZoom++)
                for (int iCoherence = 0; iCoherence < browsingCoherences.Length; iCoherence++)
                    for (int iDrop = 0; iDrop < dropFactors.Length; iDrop++)
                    {
                        //simulator.RunSimulations(queryIds, simulator.Mles.Layers[0].Length, 
                        simulator.RunClassSimulations(simulator.Mles.Layers[0].Length,
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

        private static int[] LoadClassMapping(string classMappingFile)
        {
            List<int> result = new List<int>();
            using (StreamReader reader = new StreamReader(classMappingFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    result.Add(int.Parse(line));
                }
            }

            return result.ToArray();
        }


        private static float[][] LoadDescriptorFile(string descriptorFile)
        {
            using (FeatureReader featureReader = new FeatureReader(descriptorFile))
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


        private static Tuple<int, int[]>[][] LoadMlesLayers(List<string> mlesLayers)
        {
            Tuple<int, int[]>[][] result = new Tuple<int, int[]>[mlesLayers.Count][];

            for (int iLayer = 0; iLayer < mlesLayers.Count; iLayer++)
            {
                using (StreamReader reader = new StreamReader(mlesLayers[iLayer]))
                {
                    List<Tuple<int, int[]>> resultLayer = new List<Tuple<int, int[]>>();
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
                        resultLayer.Add(new Tuple<int, int[]>(descriptorId, clusterItemIds));
                    }
                    result[iLayer] = resultLayer.ToArray();
                }
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
