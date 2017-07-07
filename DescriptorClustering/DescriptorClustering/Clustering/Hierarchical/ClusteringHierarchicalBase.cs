using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Hierarchical
{
    public abstract class ClusteringHierarchicalBase : ClusteringBase
    {
        public Centroid[][] Centroids { get; private set; }

        public ClusteringHierarchicalBase(Descriptor[] descriptors) : base(descriptors)
        {
        }

        public abstract Centroid[][] Clusterize(double[] seedPercentages, int iterationCount);
    }
}
