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
            Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(5, 5000, 2);
            ClusteringSimple clustering = new ClusteringSimple(descriptors);

            clustering.Clusterize(0.1, 10, 5334);

            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, 720, 720);
        }
    }
}
