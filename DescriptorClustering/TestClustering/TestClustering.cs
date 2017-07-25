using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptorClustering;
using DescriptorClustering.Simple;
using System.Diagnostics;
using DescriptorClustering.Hierarchical.Divisive;
using System.Linq;
using System.Collections.Generic;
using DescriptorClustering.Hierarchical.Agglomerative;

namespace TestClustering
{
    [TestClass]
    public class TestClustering
    {
        static int seed = 5334;
        static int windowSize = 1024;
        static int nDescriptors = 10000;
        static int descriptorDimension = 2;
        static int nClusters = 100;
        static int iterationCount = 10;

        static int nDescriptorsDiv5 = nDescriptors / 5;
        static Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(seed, nDescriptorsDiv5, descriptorDimension);

        public void TestClusteringSimple(ClusteringSimpleBase clustering)
        {
            Assert.ReferenceEquals(descriptors, clustering.Descriptors);
            Assert.IsNull(clustering.Centroids);

            foreach (Descriptor descriptor in clustering.Descriptors)
            {
                Assert.IsNotNull(descriptor);
                Assert.IsNotNull(descriptor.Values);
            }
        }


        [TestMethod]
        public void VisualizeSimpleCPU()
        {
            double seedPercentage = 0.1;
            int iterationCount = 10;

            ClusteringSimple clustering = new ClusteringSimple(descriptors);
            TestClusteringSimple(clustering);

            clustering.Clusterize(seedPercentage, iterationCount, seed);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
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

            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids1, windowSize, windowSize);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids2, windowSize, windowSize);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids4, windowSize, windowSize);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids8, windowSize, windowSize);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids16, windowSize, windowSize);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids32, windowSize, windowSize);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, centroids64, windowSize, windowSize);

        }


        [TestMethod]
        public void HugeSimpleCPU()
        {
            int nDescriptors = 350000;
            int descriptorDiension = 4096;
            int nClusters = 25;// 128;
            int iterationCount = 10;

            int nDescriptorsDiv5 = nDescriptors / 5;
            
            Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(seed, nDescriptorsDiv5, descriptorDiension);
            ClusteringSimple clustering = new ClusteringSimple(descriptors);
            TestClusteringSimple(clustering);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            clustering.Clusterize(nClusters, iterationCount, seed);
            stopWatch.Stop();

            double elapsedSeconds = (stopWatch.ElapsedMilliseconds * 0.001);
            double nPerSecond = nDescriptorsDiv5 * 5 * nClusters / elapsedSeconds;
            Console.WriteLine("Computing time: " + elapsedSeconds + " seconds (" + nPerSecond  + " per second).");

            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
        }

        [TestMethod]
        public void HugeDivisiveCPU()
        {
            int nDescriptors = 350000;
            int descriptorDimension = 4096;
            int[] nClusters = new int[] { 150, 150 };
            int[] iterationCounts = new int[] { 10, 10 };

            int nDescriptorsDiv5 = nDescriptors / 5;

            Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(seed, nDescriptorsDiv5, descriptorDimension);
            ClusteringDivisive clustering = new ClusteringDivisive(descriptors);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            clustering.Clusterize(nClusters, iterationCounts, seed);
            stopWatch.Stop();

            double elapsedSeconds = (stopWatch.ElapsedMilliseconds * 0.001);
            double nPerSecond = nDescriptorsDiv5 * 5 * nClusters[0] * nClusters[1] / elapsedSeconds;
            Console.WriteLine("Computing time: " + elapsedSeconds + " seconds (" + nPerSecond + " per second).");

            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
        }


        [TestMethod]
        public void VisualizeHeuristicDivisive2Layer()
        {
            int[] seedCounts = new int[] { 10, 100 };
            int[] iterationCounts = new int[] { 10, 10 };

            ClusteringDivisive clustering = new ClusteringDivisive(descriptors, true);

            clustering.Clusterize(seedCounts, iterationCounts, seed);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
        }

        [TestMethod]
        public void VisualizeHeuristicDivisive2LayerB()
        {
            int[] seedCounts = new int[] { 100, 10 };
            int[] iterationCounts = new int[] { 10, 10 };

            ClusteringDivisive clustering = new ClusteringDivisive(descriptors);

            clustering.Clusterize(seedCounts, iterationCounts, seed);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
        }


        [TestMethod]
        public void VisualizeHeuristicDivisive3Layer()
        {
            int[] seedCounts = new int[] { 10, 10, 10 };
            int[] iterationCounts = new int[] { 10, 10, 10 };

            ClusteringDivisive clustering = new ClusteringDivisive(descriptors);

            clustering.Clusterize(seedCounts, iterationCounts, seed);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids, windowSize, windowSize);
        }

        

        [TestMethod]
        public void TestDescriptorAssignmentSimple()
        {
            ClusteringSimple clustering = new ClusteringSimple(descriptors);
            clustering.Clusterize(nClusters, iterationCount, seed);
            HelperTestClass.TestDescriptorAssignment(descriptors.Length, clustering.Centroids);
        }

        [TestMethod]
        public void TestDescriptorAssignmentDivisive()
        {
            int[] nClusters = new int[] { 10, 10 };
            int[] iterationCounts = new int[] { 10, 10 };
            
            ClusteringDivisive clustering = new ClusteringDivisive(descriptors);
            clustering.Clusterize(nClusters, iterationCounts, seed);
            HelperTestClass.TestDescriptorAssignment(descriptors.Length, clustering.Centroids[clustering.Centroids.Length - 1]);
        }

        [TestMethod]
        public void VisualizeAgglomerative()
        {
            int nDescriptors = 100;
            int descriptorDimension = 2;
            int nDescriptorsDiv5 = nDescriptors / 5;

            Descriptor[] descriptors = HelperTestClass.GenerateHierarchicalDescriptors(seed, nDescriptorsDiv5, descriptorDimension);
            ClusteringAgglomerative clustering = 
                new ClusteringAgglomerative(descriptors, Enumerable.Repeat(1, descriptors.Length).ToArray());

            clustering.Clusterize();
            HelperTestClass.TestDescriptorAssignment(descriptors.Length, clustering.Centroids[0]);
            HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids[23], windowSize, windowSize);
        }

    }
}
