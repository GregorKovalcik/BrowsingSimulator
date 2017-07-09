using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Hierarchical
{
    public abstract class ClusteringHierarchicalBase : ClusteringBase
    {
        // Hierarchical clusterization produces multiple layers, so we need to have array of array of centroids
        public Centroid[][] Centroids { get; protected set; }

        public ClusteringHierarchicalBase(Descriptor[] descriptors) : base(descriptors)
        {
        }

    }
}
