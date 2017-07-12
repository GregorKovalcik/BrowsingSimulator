using Alea;
using Alea.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowsingSimulator
{
    public class MLES
    {
        public Item[] Dataset { get; protected set; }
        public Item[][] Layers { get; protected set; }

        protected int gpuDatasetSize = 0;
        protected float[][] subDatasetGpu;
        protected float[] distancesGpu;
        protected LaunchParam launchParam;
        
        public MLES(float[][] dataset, Tuple<int, int[]>[][] clusteringLayers)
        {
            // TODO: check input data
            FillDataset(dataset);
            FillLayers(clusteringLayers);

            AllocateGpuMemory();
        }

        protected void AllocateGpuMemory()
        {
            ulong maxMemory = Gpu.Default.Device.TotalMemory;
            int featureDimension = Dataset[0].Descriptor.Length;    // TODO: better solution

            int distancesGpuSize = sizeof(float) * 340000;          // TODO
            gpuDatasetSize = (int)((maxMemory - (ulong)distancesGpuSize) / (ulong)(featureDimension * sizeof(float)));
            gpuDatasetSize = (int)(gpuDatasetSize * 0.8);

            float[][] subDataset = new float[gpuDatasetSize][];
            for (int i = 0; i < subDataset.Length; i++)
            {
                subDataset[i] = Dataset[i].Descriptor;
            }
            subDatasetGpu = Gpu.Default.Allocate(subDataset);
            distancesGpu = Gpu.Default.Allocate<float>(gpuDatasetSize);

            int blockDim = 512;
            launchParam = new LaunchParam((gpuDatasetSize / blockDim) + 1, blockDim);
        }

        protected void FillDataset(float[][] dataset)
        {
            Dataset = new Item[dataset.Length];
            for (int i = 0; i < dataset.Length; i++)
            {
                Dataset[i] = new Item(i, i, dataset[i], null);
            }
        }

        protected void FillLayers(Tuple<int, int[]>[][] clusteringLayers)
        {
            Layers = new Item[clusteringLayers.Length + 1][];
            Layers[Layers.Length - 1] = Dataset;

            // bottom-up
            for (int iLayer = Layers.Length - 2; iLayer >= 0; iLayer--)
            {
                Console.WriteLine("Constructing layer {0}", iLayer);
                Tuple<int, int[]>[] clusterization = clusteringLayers[iLayer];

                // generate current layer clusters using bottom layer
                Layers[iLayer] = new Item[clusterization.Length];
                for (int iLayerCluster = 0; iLayerCluster < Layers[iLayer].Length; iLayerCluster++)
                {
                    // generate one cluster
                    int globalId = clusterization[iLayerCluster].Item1;
                    float[] descriptor = Dataset[globalId].Descriptor;

                    // assigne bottom layer subclusters
                    int[] clusterItemIds = clusterization[iLayerCluster].Item2;
                    Item[] clusterItems = new Item[clusterItemIds.Length];
                    Item[] bottomLayer = Layers[iLayer + 1];
                    for (int iClusterItem = 0; iClusterItem < clusterItemIds.Length; iClusterItem++)
                    {
                        int clusterItemId = clusterItemIds[iClusterItem];
                        if (iLayer == Layers.Length - 2)
                        {
                            // no need to do an expensive search in the last layer
                            clusterItems[iClusterItem] = Dataset[clusterItemId];
                        }
                        else
                        {
                            // expensive search in other layers
                            clusterItems[iClusterItem] = bottomLayer.Where(item => item.Id == clusterItemId).First();
                        }
                    }
                    Layers[iLayer][iLayerCluster] = new Item(globalId, iLayerCluster, descriptor, clusterItems);
                    foreach (Item item in clusterItems)
                    {
                        item.SetParentItem(Layers[iLayer][iLayerCluster]);
                    }
                }
            }
        }

        public Item[] SearchKNN(Item query, int layerId, int nResults, Func<int, bool> HasItemDroppedOut)
        {
            Item[] layer = Layers[layerId];
            return SearchKNN(query, layer, nResults, HasItemDroppedOut, new Item[0]);
        }
        public Item[] SearchKNN(Item query, Item[] set, int nResults, Func<int, bool> HasItemDroppedOut)
        {
            return SearchKNN(query, set, nResults, HasItemDroppedOut, new Item[0]);
        }

        public Item[] SearchKNN(Item query, int layerId, int nResults, 
            Func<int, bool> HasItemDroppedOut, ICollection<Item> alreadyUsedItems)
        {
            Item[] layer = Layers[layerId];
            return SearchKNN(query, layer, nResults, HasItemDroppedOut, alreadyUsedItems);
        }

        public Item[] SearchKNN(Item query, Item[] set, int nResults, 
            Func<int, bool> HasItemDroppedOut, ICollection<Item> alreadyUsedItems)
        {
            // TODO: cache
            if (false)
            {

            }
            else
            {
                float[] distances;
                if (set.Equals(Dataset))
                {
                    distances = ComputeDistancesDatasetGPU(query);
                }
                else
                {
                    distances = ComputeDistances(query, set);
                }
                Item[] sortedLayer = (Item[])set.Clone();
                Array.Sort(distances, sortedLayer);

                List<Item> results = new List<Item>();
                foreach (Item item in sortedLayer)
                {
                    if (!HasItemDroppedOut(item.Id) && !alreadyUsedItems.Contains(item))
                    {
                        results.Add(item);
                    }

                    if (results.Count == nResults)
                    {
                        return results.ToArray();
                    }
                }
                return results.ToArray();
            }
        }

        private static float[] ComputeDistances(Item query, Item[] set)
        {
            float[] distances = new float[set.Length];
            Parallel.For(0, set.Length, index =>
            {
                distances[index] = Item.GetDistanceSQR(query.Descriptor, set[index].Descriptor);
            });
            return distances;
        }

        private float[] ComputeDistancesDatasetGPU(Item query)
        {
            // gpu
            Task<float[]> task = Task<float[]>.Factory.StartNew(() =>
            {
                float[] queryGpu = Gpu.Default.Allocate(query.Descriptor);
                Gpu.Default.Launch<float[], float[][], float[], Func<float[], float[], float>>
                    (ComputeDistancesKernel, launchParam, queryGpu, subDatasetGpu, distancesGpu, Item.GetDistanceSQR);
                Gpu.Free(queryGpu);
                return Gpu.CopyToHost(distancesGpu);
            });
            
            // cpu
            float[] distances = new float[Dataset.Length];
            Parallel.For(gpuDatasetSize, distances.Length, index =>
            {
                distances[index] = Item.GetDistanceSQR(query.Descriptor, Dataset[index].Descriptor);
            });

            float[] distancesGpuResult = task.Result;
            Array.Copy(distancesGpuResult, distances, distancesGpuResult.Length);
            
            return distances;
        }
        

        public static void ComputeDistancesKernel(float[] query, float[][] datasetGpu, float[] distances, 
            Func<float[], float[], float> distanceFunction)
        {
            int start = blockIdx.x * blockDim.x + threadIdx.x;
            int stride = gridDim.x * blockDim.x;
            for (int i = start; i < distances.Length; i += stride)
            {
                distances[i] = distanceFunction(query, datasetGpu[i]);
            }
        }

        public static void Kernel<T>(T[] result, T[] arg1, T[] arg2, Func<T, T, T> op)
        {
            var start = blockIdx.x * blockDim.x + threadIdx.x;
            var stride = gridDim.x * blockDim.x;
            for (var i = start; i < result.Length; i += stride)
            {
                result[i] = op(arg1[i], arg2[i]);
            }
        }
    }
}
