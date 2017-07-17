#define VERBOSE

using DescriptorClustering.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Hierarchical.Divisive
{
    public class ClusteringDivisive : ClusteringHierarchicalBase
    {
        public ClusteringDivisive(Descriptor[] descriptors) : base(descriptors)
        {
        }
        

        private static void CheckClusterizeArguments(int[] layerSeedCounts, int[] iterationCounts)
        {
            if (layerSeedCounts == null)
            {
                throw new ArgumentNullException("Layer seed counts have not to be null!");
            }
            else if (layerSeedCounts.Length == 0)
            {
                throw new ArgumentOutOfRangeException("Layer seed counts have not to be empty!");
            }
            else
            {
                // TODO:
                // check decreasing amount of seeds (and if they are positive integers)
            }

            if (iterationCounts == null)
            {
                throw new ArgumentNullException("Iteration counts have not to be null!");
            }
            else if (iterationCounts.Length == 0)
            {
                throw new ArgumentOutOfRangeException("Iteration counts have not to be empty!");
            }
            else
            {
                // TODO: check if all values are positive integers
            }
        }


        // TODO: warning - no linkage between centroids of different layers. This is just heuristics for the lowest layer.
        public virtual double[] Clusterize(int[] layerSeedCounts, int[] iterationCounts, int randomSeed)
        {
            CheckClusterizeArguments(layerSeedCounts, iterationCounts);

            int layerCount = layerSeedCounts.Length;
            Centroid[][] hierarchicalClusters = new Centroid[layerCount][];

#if VERBOSE
            Console.WriteLine("Computing clusters for layer ID: 0");
#endif
            // compute the highest layer
            ClusteringSimple clusteringSimple = new ClusteringSimple(Descriptors);
            double[] updateDeltas = clusteringSimple.Clusterize(layerSeedCounts[0], iterationCounts[0], randomSeed);
            hierarchicalClusters[0] = clusteringSimple.Centroids;
            
#if VERBOSE
            Console.WriteLine("Helper higher level clustering added: ");
            foreach (Centroid c in clusteringSimple.Centroids)
            {
                Console.WriteLine("{0}:", c.Mean.Id);
            }
#endif
            int expectedLayerClusterCount = layerSeedCounts[0];
            // run clustering for each other layer
            for (int iLayer = 1; iLayer < layerCount; iLayer++)
            {
#if VERBOSE
                Console.WriteLine("Computing clusters for layer ID: {0}", iLayer);
#endif
                expectedLayerClusterCount *= layerSeedCounts[iLayer];
                List<Centroid> lowerLayerCentroids = new List<Centroid>();
                int layerClusterId = 0;
                foreach (Centroid higherLayerCentroid in hierarchicalClusters[iLayer - 1])
                {
                    Descriptor[] centroidDescriptors = higherLayerCentroid.Descriptors.ToArray();

                    clusteringSimple = new ClusteringSimple(centroidDescriptors);
                    int seedCount = (int)(centroidDescriptors.Length / (Descriptors.Length * 1.0 / expectedLayerClusterCount));
                    seedCount = (seedCount > 0) ? seedCount : 1;
                    //int seedCount = (layerSeedCounts[iLayer] <= centroidDescriptors.Length)
                    //    ? layerSeedCounts[iLayer]
                    //    : centroidDescriptors.Length;
#if VERBOSE
                    Console.WriteLine("Clusterization of {0} seeds, {1} iterations.", seedCount, iterationCounts[iLayer]);
#endif
                    clusteringSimple.Clusterize(seedCount, iterationCounts[iLayer], randomSeed);
#if VERBOSE
                    Console.WriteLine("Clusterization complete, {0} clusters found.", clusteringSimple.Centroids.Length);
#endif

                    // offset centroid indexes
                    foreach (Centroid centroid in clusteringSimple.Centroids)
                    {
                        centroid.Id = layerClusterId++;
                    }

                    lowerLayerCentroids.AddRange(clusteringSimple.Centroids);
#if VERBOSE
                    Console.WriteLine("Cluster {0} added: ", higherLayerCentroid.Mean.Id);
                    foreach (Centroid c in clusteringSimple.Centroids)
                    {
                        //Console.WriteLine("{0}:", c.Mean.Id);
                    }
#endif
                }
                hierarchicalClusters[iLayer] = lowerLayerCentroids.ToArray();
#if VERBOSE
                Console.WriteLine("Layer complete, {0} clusters found.", hierarchicalClusters[iLayer].Length);
#endif
            }
            Centroids = hierarchicalClusters;

            return updateDeltas;    // TODO deltas are just for the first layer
        }



        // TODO?
        //private static void CheckClusterizeArguments(double[] layerSeedPercentages, int iterationCount)
        // TODO?
        //public override Centroid[][] Clusterize(double[] layerSeedPercentages, int iterationCount); 

    }
}
