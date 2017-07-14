//#define GPU
#if GPU
using Alea;
using Alea.CSharp;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowsingSimulator
{
    public class MLES : IDisposable
    {
        public Item[] Dataset { get; protected set; }
        public Item[][] Layers { get; protected set; }
#if GPU
        protected int gpuDatasetSize = 0;
        protected float[][] subDatasetGpu;
        protected float[] distancesGpu;
        protected object gpuLock = new object();
        protected LaunchParam launchParam;
#endif
        protected ConcurrentDictionary<int, Item[]> cache = new ConcurrentDictionary<int, Item[]>();
        protected string cacheFilename;
        protected int cachedArrayLength = 1000;

        public MLES(float[][] dataset, Tuple<int, int[]>[][] clusteringLayers, string cacheFilename = null)
        {
            // TODO: check input data
            FillDataset(dataset);

#if DEBUG
            TestLayerChildrenCount(clusteringLayers[1], dataset.Length);
            TestLayerChildrenCount(clusteringLayers[0], clusteringLayers[1].Length);
#endif

            FillLayers(clusteringLayers);
#if DEBUG
            TestLayerParent(Layers[2], 2);
            TestLayerParent(Layers[1], 1);
            TestLayerParent(Layers[0], 0);
#endif
#if GPU
            AllocateGpuMemory();
#endif
            this.cacheFilename = cacheFilename;
            LoadCache();
        }
#if GPU
        protected void AllocateGpuMemory()
        {
            Console.WriteLine("Allocating GPU memory.");
            ulong maxMemory = Gpu.Default.Device.TotalMemory;
            int featureDimension = Dataset[0].Descriptor.Length;    // TODO: better solution

            //int distancesGpuSize = sizeof(float) * 340000;          // TODO
            //gpuDatasetSize = (int)((maxMemory - (ulong)distancesGpuSize) / (ulong)(featureDimension * sizeof(float)));
            //gpuDatasetSize = (int)(gpuDatasetSize * 0.8);
            gpuDatasetSize = Dataset.Length;
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
#endif
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

        private void TestLayerChildrenCount(Tuple<int, int[]>[] clusteringLayer, int expectedCount)
        {
            int childrenCount = 0;
            foreach (Tuple<int, int[]> node in clusteringLayer)
            {
                childrenCount += node.Item2.Length;
            }

            if (childrenCount != expectedCount)
            {
                throw new ArgumentException("Layer children count does not match!");
            }
        }

        private void TestLayerParent(Item[] layer, int expectedParentCount)
        {
            foreach (Item item in layer)
            {
                Item parent = item;
                for (int i = 0; i < expectedParentCount; i++)
                {
                    parent = parent.ParentItem;
                }
                if (parent.ParentItem != null)
                {
                    throw new ArgumentException("Parent should be null!");
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
            Item[] sortedLayer;

            // load sorted array
            if (set.Equals(Dataset) && cacheFilename != null && cache.ContainsKey(query.Id))
            {
                sortedLayer = cache[query.Id];
#if VERBOSE
                Console.WriteLine("== CACHE HIT ==");
#endif
            }
            else
            {
                float[] distances;
#if GPU
                if (set.Equals(Dataset))
                {
                    distances = ComputeDistancesDatasetGPU(query);
                    //distances = ComputeDistancesDatasetCPU(query, Dataset);
                }
                else
                {
                    distances = ComputeDistancesDatasetCPU(query, set);
                }
#else
                distances = ComputeDistancesDatasetCPU(query, set);
#endif
                sortedLayer = (Item[])set.Clone();
                Array.Sort(distances, sortedLayer);

                if (set.Equals(Dataset) && cacheFilename != null)
                {
                    try
                    {
                        Item[] cachedArray = new Item[cachedArrayLength];
                        Array.Copy(sortedLayer, cachedArray, cachedArray.Length);
                        cache.TryAdd(query.Id, cachedArray);
                    }
                    catch (OutOfMemoryException ex)
                    {
                        cache.Clear();
                        Console.WriteLine("==== CACHE EMPTIED ====");
                    }
                }
            }

            // filter dropped items
            List<Item> results = new List<Item>();
            foreach (Item item in sortedLayer)
            {
                if (!HasItemDroppedOut(item.Id) && !alreadyUsedItems.Contains(item))
                {
                    results.Add(item);
                }
#if VERBOSE
                    else
                    {
                        Console.WriteLine("Item dropped from display: {0}", item.Id);
                    }
#endif
                if (results.Count == nResults)
                {
                    return results.ToArray();
                }
            }
            return results.ToArray();
        }

        private static float[] ComputeDistancesDatasetCPU(Item query, Item[] set)
        {
            float[] distances = new float[set.Length];
            Parallel.For(0, set.Length, index =>
            {
                distances[index] = Item.GetDistanceSQR(query.Descriptor, set[index].Descriptor);
            });
            return distances;
        }
#if GPU
        private float[] ComputeDistancesDatasetGPU(Item query)
        {
            // gpu
            Task<float[]> task = Task<float[]>.Factory.StartNew(() =>
            {
                lock (gpuLock)
                {
                    float[] queryGpu = Gpu.Default.Allocate(query.Descriptor);
                    //float[] distancesGpu = Gpu.Default.Allocate<float>(gpuDatasetSize);
                    Gpu.Default.Launch<float[], float[][], float[], Func<float[], float[], float>>
                        (ComputeDistancesKernel, launchParam, queryGpu, subDatasetGpu, distancesGpu, Item.GetDistanceSQR);
                    Gpu.Free(queryGpu);
                    float[] distancesGpuResultTask = Gpu.CopyToHost(distancesGpu);
                    //Gpu.Free(distancesGpu);
                    return distancesGpuResultTask;
                }
            });

            // cpu
            //float[] distances = new float[Dataset.Length];
            //Parallel.For(gpuDatasetSize, distances.Length, index =>
            //{
            //    distances[index] = Item.GetDistanceSQR(query.Descriptor, Dataset[index].Descriptor);
            //});

            float[] distancesGpuResult = task.Result;
            //Array.Copy(distancesGpuResult, distances, distancesGpuResult.Length);

            return distancesGpuResult;
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
#endif

        public void SaveCache()
        {
            if (cacheFilename == null)  // cache disabled
            {
                return;
            }

            Console.WriteLine("Saving MLES cache.");
            using (BinaryWriter writer = new BinaryWriter(File.Open(cacheFilename, 
                FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                writer.Write(cache.Count);
                writer.Write(cachedArrayLength);

                foreach (int key in cache.Keys)
                {
                    writer.Write(key);
                    Item[] value = cache[key];
                    for (int i = 0; i < value.Length; i++)
                    {
                        writer.Write(value[i].Id);
                    }
                }
            }
        }

        protected void LoadCache()
        {
            if (cacheFilename == null || !File.Exists(cacheFilename))  // cache disabled
            {
                return;
            }

            Console.WriteLine("Loading MLES cache.");
            using (BinaryReader reader = new BinaryReader(File.Open(cacheFilename, 
                FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                int cacheSize = reader.ReadInt32();
                cachedArrayLength = reader.ReadInt32();

                for (int i = 0; i < cacheSize; i++)
                {
                    int key = reader.ReadInt32();
                    Item[] value = new Item[cachedArrayLength];
                    for (int j = 0; j < cachedArrayLength; j++)
                    {
                        value[j] = Dataset[reader.ReadInt32()];
                    }
                    cache.TryAdd(key, value);
                }
            }
        }

        public void Dispose()
        {
            SaveCache();
        }
    }
}
