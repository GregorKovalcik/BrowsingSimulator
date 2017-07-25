#define PARALLEL
//#define VERBOSE

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

#if PARALLEL
            Parallel.ForEach(Descriptors, descriptor =>
            {
#else
            for (int index = 0; index < Descriptors.Length; index++)
            {
                Descriptor descriptor = Descriptors[index];
#endif
                double smallestDistance = double.MaxValue;
                Centroid closestCentroid = null;
                foreach (Centroid centroid in Centroids)
                {
                    double distance = Descriptor.GetDistanceSQR(centroid.Mean.Values, descriptor.Values);  // TODO: change to Func<>
                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        closestCentroid = centroid;
                    }
                }
                closestCentroid.AddDescriptorConcurrent(descriptor);
                descriptor.Centroid = closestCentroid;

#if VERBOSE
                Console.WriteLine("Descriptor {0} assigned to cluster: {1}", descriptor.Id, closestCentroid.Id);
#endif
            }
#if PARALLEL
            );
#endif
#if DEBUG
            TestUniqueDescriptorAssignment(Centroids);
#endif
        }


        protected static void TestUniqueDescriptorAssignment(Centroid[] centroids)
        {
            HashSet<int> hashSet = new HashSet<int>();
            foreach (Centroid centroid in centroids)
            {
                foreach (Descriptor descriptor in centroid.Descriptors)
                {
                    if (hashSet.Contains(descriptor.Id))
                    {
                        throw new ArgumentException("Descriptor double assignment detected! ID: " + descriptor.Id);
                    }
                    else
                    {
                        hashSet.Add(descriptor.Id);
                    }
                }
            }

            Console.WriteLine("Descriptor assignment tested, no duplicates found.");
        }


        protected override double Update()
        {
            double[] updatedCentroidDeltas = new double[Centroids.Length];
#if PARALLEL
            Parallel.For(0, Centroids.Length, index =>
#else
            for (int index = 0; index < Centroids.Length; index++)
#endif
            {
                Centroid centroid = Centroids[index];
                Descriptor oldMean = centroid.Mean;
                centroid.ComputeMean();
                Descriptor newMean = centroid.Mean;

                if (newMean != null && oldMean != null)
                {
                    updatedCentroidDeltas[index] = Descriptor.GetDistanceSQR(oldMean.Values, newMean.Values);
                }
                else
                {
                    updatedCentroidDeltas[index] = 0;
                }
            }
#if PARALLEL
            );
#endif
            return updatedCentroidDeltas.Sum();
        }


        protected override int DropEmptyCentroids()
        {
            //List<int> emptyCentroidIds = new List<int>();
            //for (int i = 0; i < Centroids.Length; i++)
            //{
            //    if (Centroids[i].Mean == null)
            //    {
            //        emptyCentroidIds.Add(i);
            //    }
            //}

            //if (emptyCentroidIds.Count > 0)
            //{
            //    Centroid[] newCentroids = new Centroid[Centroids.Length - emptyCentroidIds.Count];
            //    int skippedCounter = 0;
            //    for (int i = 0; i < Centroids.Length; i++)
            //    {
            //        if (emptyCentroidIds.Contains(i))
            //        {
            //            skippedCounter++;
            //        }
            //        else
            //        {
            //            int offsetId = i - skippedCounter;
            //            newCentroids[offsetId] = Centroids[i];
            //        }
            //    }

            //    Centroids = newCentroids;
            //}

            // ^ this probably works,
            // but this is safer:
            LinkedList<Centroid> linkedCentroids = new LinkedList<Centroid>(Centroids);
            LinkedListNode<Centroid> node = linkedCentroids.First;
            int droppedCounter = 0;
            while (node != null)
            {
                if (node.Value.Mean == null)
                {
                    LinkedListNode<Centroid> nodeToRemove = node;
                    node = node.Next;
                    linkedCentroids.Remove(nodeToRemove);
                    droppedCounter++;
                }
                else
                {
                    node = node.Next;
                }
            }
            Centroids = linkedCentroids.ToArray();

            return droppedCounter;
        }


        
    }
}
