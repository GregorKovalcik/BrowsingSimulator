using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Hierarchical.Divisive
{
    public abstract class ClusteringDivisiveBase : ClusteringHierarchicalBase
    {
        public ClusteringDivisiveBase(Descriptor[] descriptors) : base(descriptors)
        {
        }

        private static void CheckClusterizeArguments(double[] layerSeedPercentages, int iterationCount)
        {
            if (layerSeedPercentages == null)
            {
                throw new ArgumentNullException("Layer seed percentages have not to be null!");
            }
            if (iterationCount <= 0)
            {
                throw new ArgumentOutOfRangeException("Iteration count has to be a positive integer!");
            }
        }

        public override Centroid[][] Clusterize(double[] layerSeedPercentages, int iterationCount)
        {
            CheckClusterizeArguments(layerSeedPercentages, iterationCount);

            int layerCount = layerSeedPercentages.Length + 1;   // +1 level without clusters

            // run clustering for each new layer
            for (int iLayer = 0; iLayer < layerSeedPercentages.Length; iLayer++)
            {
                Descriptor[] subDescriptors;
                Centroid[] subCentroids;

            }
            return null;


            //Console.WriteLine("Running {0} iterations.", iterationCount);
            //for (int i = 0; i < maxIterations; i++)
            //{
            //    Console.WriteLine("Iteration {0}, assign phase.", i);
            //    Assign();
            //    Console.WriteLine("Update phase.");
            //    double updateDistance = Update();
            //    if (updateDistance < minUpdateDistance)
            //    {
            //        break;
            //    }
            //}
            //Console.WriteLine("Iterating finished.");
        }
    }
}
