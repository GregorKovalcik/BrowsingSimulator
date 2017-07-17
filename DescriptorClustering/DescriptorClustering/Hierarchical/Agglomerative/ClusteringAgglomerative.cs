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
        private int[] descriptorWeights;
        private double[][] centroidDistances;
        private double[] rowMinimalDistances;
        private int[] rowMinimalDistanceIds;
        private bool[] isDropped;

        public ClusteringAgglomerative(Descriptor[] descriptors, int[] descriptorWeights) : base(descriptors)
        {
            this.descriptorWeights = descriptorWeights;

            centroidDistances = new double[descriptors.Length][];
            rowMinimalDistances = new double[descriptors.Length];
            rowMinimalDistanceIds = new int[descriptors.Length];
            isDropped = new bool[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
            {
                isDropped[i] = false;
                centroidDistances[i] = new double[descriptors.Length];
                rowMinimalDistances[i] = double.MaxValue;
                rowMinimalDistanceIds[i] = -1;
            }
        }

        public virtual double[] Clusterize()
        {
            return Clusterize(int.MaxValue, new List<int>(Enumerable.Range(0, Descriptors.Length - 1)));
        }

        public virtual double[] Clusterize(List<int> layersToExport)
        {
            return Clusterize(int.MaxValue, layersToExport);
        }


        public virtual double[] Clusterize(int maxIterations, List<int> layersToExport)
        {
            int iterationCount = (Descriptors.Length - 1 < maxIterations) ? Descriptors.Length - 1 : maxIterations;

            List<double> iterationDistances = new List<double>();
            List<Centroid[]> results = new List<Centroid[]>();

            weightedCentroids = WeightedCentroid.FromDescriptors(Descriptors, descriptorWeights);
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

                // export levels
                if (layersToExport.Contains((iterationCount - 1) - i))
                {
                    Centroid[] layerCentroids = ExportLayer();
                    AssignClosestDescriptors(layerCentroids);
                    //Centroids[(iterationCount - 1) - i] = layerCentroids;
                    results.Add(layerCentroids);
                }
            }
            Centroids = results.ToArray();
            return iterationDistances.ToArray();
        }


        private void PrecomputeAllDistances()
        {
            Parallel.For(0, centroidDistances.Length, i =>
            {
                for (int j = 0; j < i; j++)
                {
                    // precompute distance
                    centroidDistances[i][j] = WeightedCentroid.GetDistance(weightedCentroids[i], weightedCentroids[j]);
                    // precompute minimal distance in a row
                    if (centroidDistances[i][j] < rowMinimalDistances[i])
                    {
                        rowMinimalDistances[i] = centroidDistances[i][j];
                        rowMinimalDistanceIds[i] = j;
                    }
                }
            });
        }

        private void ComputeNewDistances(int mergedCentroidId, int droppedCentroidId)
        {
            // invalidate dropped data
            rowMinimalDistances[droppedCentroidId] = double.NaN;
            rowMinimalDistanceIds[droppedCentroidId] = -1;
            Parallel.For(0, droppedCentroidId, j =>
            {
                centroidDistances[droppedCentroidId][j] = double.NaN;
            });
            Parallel.For(droppedCentroidId + 1, centroidDistances.Length, i =>
            {
                centroidDistances[i][droppedCentroidId] = double.NaN;
            });


            // compute new distances and minimal value in mergedId row
            rowMinimalDistances[mergedCentroidId] = double.MaxValue;
            Parallel.For(0, mergedCentroidId, j =>
            {
                if (!isDropped[j])
                {
                    centroidDistances[mergedCentroidId][j]
                        = WeightedCentroid.GetDistance(weightedCentroids[j], weightedCentroids[mergedCentroidId]);

                    // update minimal distances
                    if (centroidDistances[mergedCentroidId][j] < rowMinimalDistances[mergedCentroidId])
                    {
                        rowMinimalDistances[mergedCentroidId] = centroidDistances[mergedCentroidId][j];
                        rowMinimalDistanceIds[mergedCentroidId] = j;
                    }
                }
            });
            if (rowMinimalDistances[mergedCentroidId] == double.MaxValue)
            {
                rowMinimalDistances[mergedCentroidId] = double.NaN;
            }

            // compute new distances in mergedId column
            Parallel.For(mergedCentroidId + 1, centroidDistances.Length, i =>
            {
                if (!isDropped[i])
                {
                    centroidDistances[i][mergedCentroidId]
                        = WeightedCentroid.GetDistance(weightedCentroids[i], weightedCentroids[mergedCentroidId]);

                    // update minimal distances
                    if (centroidDistances[i][mergedCentroidId] < rowMinimalDistances[i])
                    {
                        rowMinimalDistances[i] = centroidDistances[i][mergedCentroidId];
                        rowMinimalDistanceIds[i] = mergedCentroidId;
                    }
                }
            });


            // find new minimal value in the row if the value from dropped column was minimal
            for (int i = droppedCentroidId + 1; i < centroidDistances.Length; i++)
            {
                if (!isDropped[i] && rowMinimalDistanceIds[i] == droppedCentroidId)
                {
                    rowMinimalDistances[i] = double.MaxValue;
                    rowMinimalDistanceIds[i] = -1;
                    for (int j = 0; j < i; j++)
                    {
                        if (!isDropped[j] && centroidDistances[i][j] < rowMinimalDistances[i]) 
                        {
                            rowMinimalDistances[i] = centroidDistances[i][j];
                            rowMinimalDistanceIds[i] = j;
                        }
                    }
                    if (rowMinimalDistances[i] == double.MaxValue)
                    {
                        rowMinimalDistances[i] = double.NaN;
                    }
                }
            }
        }
        private Tuple<int, int> FindMinimalDistanceIds()
        {
            // initial values
            //double[] minimalDistances = new double[centroidDistances.Length];
            //int[] minimalIndexesJ = new int[centroidDistances.Length];
            //for (int i = 0; i < centroidDistances.Length; i++)
            //{
            //    minimalDistances[i] = double.MaxValue;
            //    minimalIndexesJ[i] = -1;
            //}

            // searching
            //Parallel.For(0, centroidDistances.Length, i =>
            ////for (int i = 0; i < centroidDistances.Length; i++)
            //{
            //    if (!isDropped[i])
            //    {
            //        for (int j = 0; j < i; j++)
            //        {
            //            if (!isDropped[j])
            //            {
            //                if (centroidDistances[i][j] < minimalDistances[i])
            //                {
            //                    minimalDistances[i] = centroidDistances[i][j];
            //                    minimalIndexesJ[i] = j;
            //                }
            //            }
            //        }
            //    }
            //});

            //double minimalDistance = double.MaxValue;
            //int minimalIndexI = -1;
            //for (int i = 0; i < minimalDistances.Length; i++)
            //{
            //    if (!isDropped[i])
            //    {
            //        if (minimalDistances[i] < minimalDistance)
            //        {
            //            minimalDistance = minimalDistances[i];
            //            minimalIndexI = i;
            //        }
            //    }
            //}

            //return new Tuple<int, int>(minimalIndexI, minimalIndexesJ[minimalIndexI]);

            // new code, using a column of precomputed minimal values
            double minimalDistance = double.MaxValue;
            int minimalIndexI = -1;
            for (int i = 1; i < rowMinimalDistances.Length; i++)
            {
                if (!isDropped[i])
                {
                    if (rowMinimalDistances[i] < minimalDistance)
                    {
                        minimalDistance = rowMinimalDistances[i];
                        minimalIndexI = i;
                    }
                }
            }

            return new Tuple<int, int>(minimalIndexI, rowMinimalDistanceIds[minimalIndexI]);
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
            public int Weight { get; protected set; }

            public WeightedCentroid(Centroid centroid)
            {
                Centroid = centroid;
                Weight = 1;
            }

            public WeightedCentroid(Centroid centroid, int weight)
            {
                Centroid = centroid;
                Weight = weight;
            }

            public WeightedCentroid(int id, Descriptor descriptor)
            {
                Centroid = new Centroid(id, descriptor);
                Centroid.Descriptors.Add(descriptor);
                Weight = 1;
            }

            public WeightedCentroid(int id, Descriptor descriptor, int weight)
            {
                Centroid = new Centroid(id, descriptor);
                Centroid.Descriptors.Add(descriptor);
                Weight = weight;
            }

            public static WeightedCentroid Merge(WeightedCentroid merged, WeightedCentroid dropped)
            {
                Centroid mergedCentroid = new Centroid(merged.Centroid.Id);
                mergedCentroid.Descriptors.AddRange(merged.Centroid.Descriptors);
                mergedCentroid.Descriptors.AddRange(dropped.Centroid.Descriptors);
                mergedCentroid.ComputeMean();
                WeightedCentroid mergedWeightedCentroid = new WeightedCentroid(mergedCentroid, merged.Weight + dropped.Weight);
                return mergedWeightedCentroid;
            }

            public static WeightedCentroid[] FromDescriptors(Descriptor[] descriptors, int[] descriptorWeights)
            {
                WeightedCentroid[] result = new WeightedCentroid[descriptors.Length];
                for (int i = 0; i < descriptors.Length; i++)
                {
                    result[i] = new WeightedCentroid(i, descriptors[i], descriptorWeights[i]);
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
