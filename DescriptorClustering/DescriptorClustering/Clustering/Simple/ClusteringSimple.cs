using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Simple
{
    public class ClusteringSimple : ClusteringSimpleBase
    {
        public ClusteringSimple(Descriptor[] descriptors) : base(descriptors)
        {
        }

        protected override void Assign()
        {
            foreach (Centroid centroid in Centroids)
            {
                centroid.ClearDescriptors();
            }

            Parallel.ForEach(Descriptors, descriptor =>
            {
                double smallestDistance = double.MaxValue;
                Centroid closestCentroid = null;
                foreach (Centroid centroid in Centroids)
                {
                    double distance = Descriptor.GetDistance(centroid.Mean.Values, descriptor.Values);  // TODO: change to Func<>
                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        closestCentroid = centroid;
                    }
                }
                closestCentroid.Descriptors.Add(descriptor);
                descriptor.Centroid = closestCentroid;
                
                #if DEBUG
                Console.WriteLine("Descriptor {0} assigned to cluster: {1}", descriptor.Id, closestCentroid.Id);
                #endif
            });
        }

        protected override double Update()
        {
            double[] updatedCentroidDeltas = new double[Centroids.Length];
            Parallel.For(0, Centroids.Length, index =>
            {
                Centroid centroid = Centroids[index];
                Descriptor oldMean = centroid.Mean;
                centroid.ComputeMean();
                Descriptor newMean = centroid.Mean;

                if (newMean != null && oldMean != null)
                {
                    updatedCentroidDeltas[index] = Descriptor.GetDistance(oldMean.Values, newMean.Values);
                }
                else
                {
                    updatedCentroidDeltas[index] = 0;
                }
            });

            return updatedCentroidDeltas.Sum();
        }

        protected override void AssignClosestDescriptors()
        {
            Parallel.ForEach(Centroids, centroid =>
            {
                double smallestDistance = double.MaxValue;
                Descriptor closestDescriptor = null;
                foreach (Descriptor descriptor in Descriptors)
                {
                    double distance = Descriptor.GetDistance(centroid.Mean.Values, descriptor.Values);  // TODO: change to Func<>
                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        closestDescriptor = descriptor;
                    }
                }
                centroid.AssignClosestDescriptor(closestDescriptor);

                #if DEBUG
                Console.WriteLine("Descriptor {0} assigned to cluster: {1}", closestDescriptor.Id, centroid.Id);
                #endif
            });
        }
    }
}
