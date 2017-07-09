﻿using DescriptorClustering;
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

            int nDescriptors = 100;
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
            clustering.Clusterize();

            for (int i = 9; i > 0; i--)
            {
                HelperTestClass.VisualizeClustering(clustering.Descriptors, clustering.Centroids[10 * i], windowSize, windowSize);
            }
        }
    }
}
