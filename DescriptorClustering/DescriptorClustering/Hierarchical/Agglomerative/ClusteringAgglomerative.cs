#define VERBOSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Hierarchical.Agglomerative
{
    public class ClusteringAgglomerative : ClusteringHierarchicalBase
    {
        private WeightedCentroid[] weightedCentroids;
        private double[][] centroidDistances;
        private bool[] isDropped;

        public ClusteringAgglomerative(Descriptor[] descriptors) : base(descriptors)
        {
            centroidDistances = new double[descriptors.Length][];
            isDropped = new bool[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
            {
                isDropped[i] = false;
                centroidDistances[i] = new double[descriptors.Length];
            }
        }

        public virtual double[] Clusterize()
        {
            return Clusterize(int.MaxValue);
        }

        public virtual double[] Clusterize(int maxIterations)
        {
            int iterationCount = (Descriptors.Length - 1 < maxIterations) ? Descriptors.Length - 1 : maxIterations;

            List<double> iterationDistances = new List<double>();

            weightedCentroids = WeightedCentroid.FromDescriptors(Descriptors);
            Centroids = new Centroid[iterationCount][];

#if VERBOSE
            Console.WriteLine("Precomputing {0}x{0} = {1} distances.", 
                centroidDistances.Length, centroidDistances.Length * centroidDistances.Length);
#endif
            PrecomputeAllDistances();

#if VERBOSE
            Console.WriteLine("Running {0} iterations:", iterationCount);
#endif
            // iterations
            for (int i = 0; i < iterationCount; i++)
            {
#if VERBOSE
                Console.WriteLine("Iteration {0} / {1}.", i, iterationCount);
#endif
                Tuple<int, int> idPair = FindMinimalDistanceIds();

                int mergedId;
                int droppedId;
                // merge lighter into heavier
                if (weightedCentroids[idPair.Item1].Weight >= weightedCentroids[idPair.Item2].Weight)
                {
                    mergedId = idPair.Item1;
                    droppedId = idPair.Item2;
                }
                else
                {
                    mergedId = idPair.Item2;
                    droppedId = idPair.Item1;
                }

                MergeClusters(mergedId, droppedId);

                ComputeNewDistances(mergedId, droppedId);

                Centroid[] layerCentroids = ExportLayer();
                AssignClosestDescriptors(layerCentroids, Descriptors);
                Centroids[(iterationCount - 1) - i] = layerCentroids;
            }
            return iterationDistances.ToArray();
        }


        private void PrecomputeAllDistances()
        {
            Parallel.For(0, centroidDistances.Length, i =>
            //for (int i = 0; i < centroidDistances.Length; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    centroidDistances[i][j] = centroidDistances[j][i] = WeightedCentroid.GetDistance(weightedCentroids[i], weightedCentroids[j]);
                }
            //}
            });
        }

        private void ComputeNewDistances(int mergedCentroidId, int droppedCentroidId)
        {
            Parallel.For(0, centroidDistances.Length, i =>
            //for (int i = 0; i < centroidDistances.Length; i++)
            {
                // compute new distances
                if (!isDropped[i])
                {
                    centroidDistances[i][mergedCentroidId] = centroidDistances[mergedCentroidId][i]
                        = WeightedCentroid.GetDistance(weightedCentroids[i], weightedCentroids[mergedCentroidId]);
                }
                // invalidate dropped distances (just to be sure)
                centroidDistances[i][droppedCentroidId] = centroidDistances[droppedCentroidId][i] = double.NaN;
            //}
            });
        }

        private Tuple<int, int> FindMinimalDistanceIds()
        {
            // initial values
            double[] minimalDistances = new double[centroidDistances.Length];
            int[] minimalIndexesJ = new int[centroidDistances.Length];
            for (int i = 0; i < centroidDistances.Length; i++)
            {
                minimalDistances[i] = double.MaxValue;
                minimalIndexesJ[i] = -1;
            }

            // searching
            Parallel.For(0, centroidDistances.Length, i =>
            //for (int i = 0; i < centroidDistances.Length; i++)
            {
                if (!isDropped[i])
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (!isDropped[j])
                        {
                            if (centroidDistances[i][j] < minimalDistances[i])
                            {
                                minimalDistances[i] = centroidDistances[i][j];
                                minimalIndexesJ[i] = j;
                            }
                        }
                    }
                }
            });

            double minimalDistance = double.MaxValue;
            int minimalIndexI = -1;
            for (int i = 0; i < minimalDistances.Length; i++)
            {
                if (!isDropped[i])
                {
                    if (minimalDistances[i] < minimalDistance)
                    {
                        minimalDistance = minimalDistances[i];
                        minimalIndexI = i;
                    }
                }
            }

            return new Tuple<int, int>(minimalIndexI, minimalIndexesJ[minimalIndexI]);
        }

        private void MergeClusters(int mergedId, int droppedId)
        {
            WeightedCentroid mergedCentroid = weightedCentroids[mergedId];
            WeightedCentroid droppedCentroid = weightedCentroids[droppedId];
            weightedCentroids[mergedId] = WeightedCentroid.Merge(mergedCentroid, droppedCentroid);
            isDropped[droppedId] = true;
        }

        private Centroid[] ExportLayer()
        {
            List<Centroid> notDroppedCentroids = new List<Centroid>();
            for (int i = 0; i < isDropped.Length; i++)
            {
                if (!isDropped[i])
                {
                    notDroppedCentroids.Add(weightedCentroids[i].Centroid.Clone());
                }
            }
            return notDroppedCentroids.ToArray();
        }

        class WeightedCentroid
        {
            public Centroid Centroid { get; set;}
            public int Weight { get; set; }

            public WeightedCentroid(Centroid centroid, int weight)
            {
                Centroid = centroid;
                Weight = weight;
            }

            public WeightedCentroid(Centroid centroid)
            {
                Centroid = centroid;
                Weight = 1;
            }

            public WeightedCentroid(int id, Descriptor descriptor)
            {
                Centroid = new Centroid(id, descriptor);
                Centroid.Descriptors.Add(descriptor);
                Weight = 1;
            }

            public static WeightedCentroid Merge(WeightedCentroid merged, WeightedCentroid dropped)
            {
                Centroid mergedCentroid = new Centroid(merged.Centroid.Id);
                mergedCentroid.Descriptors.AddRange(merged.Centroid.Descriptors);
                mergedCentroid.Descriptors.AddRange(dropped.Centroid.Descriptors);
                mergedCentroid.ComputeMean();
                WeightedCentroid mergedWeightedCentroid = new WeightedCentroid(mergedCentroid);
                return mergedWeightedCentroid;
            }

            public static WeightedCentroid[] FromDescriptors(Descriptor[] descriptors)
            {
                WeightedCentroid[] result = new WeightedCentroid[descriptors.Length];
                for (int i = 0; i < descriptors.Length; i++)
                {
                    result[i] = new WeightedCentroid(i, descriptors[i]);
                }
                return result;
            }

            public static Centroid[] ToCentroids(WeightedCentroid[] centroids)
            {
                Centroid[] result = new Centroid[centroids.Length];
                for (int i = 0; i < centroids.Length; i++)
                {
                    result[i] = centroids[i].Centroid;
                }
                return result;
            }

            public static double GetDistance(WeightedCentroid a, WeightedCentroid b)
            {
                double l2sqr = Descriptor.GetDistanceSQR(a.Centroid.Mean.Values, b.Centroid.Mean.Values);
                double weightMultiplied = a.Weight * b.Weight;
                double weightAdded = a.Weight + b.Weight;
                double result = l2sqr * weightMultiplied / weightAdded;
                return result;
            }
        }
    }
}
