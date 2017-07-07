using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptorClustering;
using DescriptorClustering.Simple;

namespace TestClustering
{
    [TestClass]
    public class TestClustering
    {
        Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(5, 100, 2);


        public void TestClusteringSimple(ClusteringSimpleBase clustering)
        {
            Assert.ReferenceEquals(descriptors, clustering.Descriptors);
            Assert.IsNull(clustering.Centroids);
        }

        [TestMethod]
        public void VisualizeSimpleCPU()
        {
            ClusteringSimple clustering = new ClusteringSimple(descriptors);
            TestClusteringSimple(clustering);

            clustering.Clusterize(0.1, 10, 5334);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, 720, 720);
        }

        [TestMethod]
        public void VisualizeIterationsSimpleCPU()
        {
            ClusteringSimple clustering = new ClusteringSimple(descriptors);
            TestClusteringSimple(clustering);

            clustering.Clusterize(0.1, 1, 5334);
            Centroid[] centroids1 = clustering.Centroids;
            clustering.Clusterize(0.1, 2, 5334);
            Centroid[] centroids2 = clustering.Centroids;
            clustering.Clusterize(0.1, 4, 5334);
            Centroid[] centroids4 = clustering.Centroids;
            clustering.Clusterize(0.1, 8, 5334);
            Centroid[] centroids8 = clustering.Centroids;
            clustering.Clusterize(0.1, 16, 5334);
            Centroid[] centroids16 = clustering.Centroids;
            clustering.Clusterize(0.1, 32, 5334);
            Centroid[] centroids32 = clustering.Centroids;
            clustering.Clusterize(0.1, 64, 5334);
            Centroid[] centroids64 = clustering.Centroids;

            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids1, 720, 720);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids2, 720, 720);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids4, 720, 720);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids8, 720, 720);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids16, 720, 720);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids32, 720, 720);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids64, 720, 720);

        }
    }
}
