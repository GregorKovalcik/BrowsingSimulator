using DescriptorClustering;
using DescriptorClustering.Hierarchical.Agglomerative;
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

            int nDescriptors = 1000;
            int descriptorDimension = 2;
            int nClusters = 100;
            int iterationCount = 10;

            //int nDescriptors = 350000;
            //int descriptorDimension = 4096;
            //int nClusters = 25;// 128;
            //int iterationCount = 10;

            int nDescriptorsDiv5 = nDescriptors / 5;

            Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(seed, nDescriptorsDiv5, descriptorDimension);
            
            //ClusteringSimple clustering = new ClusteringSimple(descriptors);
            //clustering.Clusterize(nClusters, iterationCount, seed);

            ClusteringAgglomerative clustering = new ClusteringAgglomerative(descriptors);
            clustering.Clusterize(new List<int> { 0, 1, 3, 7, 15, 31, 63, 127, 255, 511 });
            //clustering.Clusterize();

            //HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids[0], windowSize, windowSize);
            HelperTestClass.SaveClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize, "clustering");

            for (int i = clustering.Centroids.Length - 1; i > 0; i--)
            {
                HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids[i], windowSize, windowSize);
            }

            //HelperTestClass.SaveClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize, "clustering");

            //int resolution = 20;
            //for (int i = clustering.Centroids.Length - 10; i < clustering.Centroids.Length; i += 1/*clustering.Centroids.Length / resolution*/)
            //{
            //    HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids[i], windowSize, windowSize);
            //}
        }
    }
}
