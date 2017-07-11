using DescriptorClustering;
using DescriptorClustering.Hierarchical.Agglomerative;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClusteringCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            string descriptorFile = args[0];
            string clusteringFile = args[1];
            string outputFile = args[2];
            
            int argumentOffset = 3;
            List<int> layersToExport = new List<int>();
            for (int i = argumentOffset; i < args.Length; i++)
            {
                layersToExport.Add(int.Parse(args[i]) - 1); // TODO: try-catch
            }
            
            ClusteringAgglomerative clustering = new ClusteringAgglomerative(LoadArrayDescriptors(descriptorFile, clusteringFile));
            clustering.Clusterize(layersToExport);

            foreach (Centroid[] layerCentroids in clustering.Centroids)
            {
                string filename = Path.GetFileNameWithoutExtension(outputFile) + "_" + layerCentroids.Length 
                    + "." + Path.GetExtension(outputFile);

                WriteToTextFile(layerCentroids, filename);
            }
        }



        private static Descriptor[] LoadArrayDescriptors(string descriptorFile, string clusteringFile)
        {
            List<int> idsToExtract = new List<int>();
            using (StreamReader reader = new StreamReader(clusteringFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] tokens = line.Split(':');
                    int clusterId = int.Parse(tokens[0]);
                    int descriptorId = int.Parse(tokens[1]);
                    string descriptors = tokens[2];
                    
                    idsToExtract.Add(descriptorId);
                }
            }

            using (FeatureReader featureReader = new FeatureReader(descriptorFile))
            {
                Console.WriteLine("Loading {0} descriptors ({1} dimensions).", idsToExtract.Count, featureReader.FeatureDimension);
                Descriptor[] descriptors = new Descriptor[idsToExtract.Count];
                for (int i = 0; i < idsToExtract.Count; i++)
                {
                    float[] features = featureReader.GetFeatures(idsToExtract[i]);    // TODO: optimize
                    descriptors[i] = new Descriptor(idsToExtract[i], features);
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
