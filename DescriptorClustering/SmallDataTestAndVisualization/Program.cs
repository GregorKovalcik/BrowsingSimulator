using DescriptorClustering;
using DescriptorClustering.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClustering;

namespace SmallDataTestAndVisualization
{
    class Program
    {
        static void Main(string[] args)
        {
            int seed = 5334;
            int windowSize = 720;

            int nDescriptors = 350000;
            int descriptorDiension = 4096;
            int nClusters = 25;// 128;
            int iterationCount = 10;

            int nDescriptorsDiv5 = nDescriptors / 5;

            Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(seed, nDescriptorsDiv5, descriptorDiension);
            ClusteringSimple clustering = new ClusteringSimple(descriptors);

            clustering.Clusterize(nClusters, iterationCount, seed);

            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
        }
    }
}
