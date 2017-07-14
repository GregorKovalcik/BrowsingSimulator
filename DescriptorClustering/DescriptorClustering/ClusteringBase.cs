#define PARALLEL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering
{
    public abstract class ClusteringBase
    {
        public Descriptor[] Descriptors { get; protected set; }

        public ClusteringBase(Descriptor[] descriptors)
        {
            Descriptors = descriptors;
        }
        
        protected Centroid[] GenerateRandomSeeds(int seedCount, int randomSeed)
        {
            if (seedCount <= 0 || seedCount > Descriptors.Length)
            {
                throw new ArgumentOutOfRangeException("Seed count out of range!");
            }

            Centroid[] result = new Centroid[seedCount];
            LinkedList<Descriptor> descriptors = new LinkedList<Descriptor>(Descriptors);
            Random random = new Random(randomSeed);

            for (int i = 0; i < seedCount; i++)
            {
                int randomId = random.Next(descriptors.Count - 1);
                LinkedListNode<Descriptor> randomDescriptorNode = descriptors.First;
                for (int j = 0; j < randomId; j++)
                {
                    randomDescriptorNode = randomDescriptorNode.Next;
                }
                result[i] = new Centroid(i, randomDescriptorNode.Value);
                descriptors.Remove(randomDescriptorNode);
            }

            return result;
        }


        protected static void AssignClosestDescriptors(Centroid[] centroids)
        {
#if PARALLEL
            Parallel.ForEach(centroids, centroid => {
#else
            for (int i = 0; i < centroids.Length; i++)
            {
                Centroid centroid = centroids[i];
#endif
                double smallestDistance = double.MaxValue;
                Descriptor closestDescriptor = null;
                foreach (Descriptor descriptor in centroid.Descriptors)
                {
                    double distance = Descriptor.GetDistanceSQR(centroid.Mean.Values, descriptor.Values);  // TODO: change to Func<>
                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        closestDescriptor = descriptor;
                    }
                }
                centroid.AssignClosestDescriptor(closestDescriptor);
#if VERBOSE
                Console.WriteLine("Descriptor {0} assigned to cluster: {1}", closestDescriptor.Id, centroid.Id);
#endif
            }
#if PARALLEL
            );
#endif
#if DEBUG
            TestUniqueCentroidAssignment(centroids);
#endif
        }

        protected static void TestUniqueCentroidAssignment(Centroid[] centroids)
        {
            HashSet<int> hashSet = new HashSet<int>();
            foreach (Centroid centroid in centroids)
            {
                if (hashSet.Contains(centroid.Mean.Id))
                {
                    throw new ArgumentException("Centroid double assignment detected! ID: " + centroid.Mean.Id);
                }
                else
                {
                    hashSet.Add(centroid.Mean.Id);
                }
            }

            Console.WriteLine("Descriptor assignment tested, no duplicates found.");
        }

        //protected static void LogProgress(string id, int processedCount, int totalCount,
        //    Stopwatch stopwatch, ref long lastMiliseconds, int workUnitSize = 100)
        //{
        //    if (processedCount % workUnitSize == 0)
        //    {
        //        long elapsedMiliseconds = stopwatch.ElapsedMilliseconds;
        //        long workUnitMiliseconds = elapsedMiliseconds - lastMiliseconds;
        //        lastMiliseconds = elapsedMiliseconds;
        //        long milisecondsRemaining = (totalCount - processedCount) / workUnitSize * workUnitMiliseconds;
        //        long totalMiliseconds = elapsedMiliseconds + milisecondsRemaining;
        //        long itemsPerSecond = workUnitSize / (workUnitMiliseconds / 1000);

        //        TimeSpan tE = TimeSpan.FromMilliseconds(elapsedMiliseconds);
        //        TimeSpan tT = TimeSpan.FromMilliseconds(totalMiliseconds);
        //        TimeSpan tR = TimeSpan.FromMilliseconds(milisecondsRemaining);

        //        if (id != "" && id != null)
        //        {
        //            id += ":\n";
        //        }
        //        else
        //        {
        //            id = "";
        //        }

        //        Console.WriteLine("{0}{1} / {2} iterations ({3}/s). Time: {4} / {5} ({6} remaining).",
        //            id, processedCount, totalCount, itemsPerSecond,
        //            string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
        //            tE.Hours, tE.Minutes, tE.Seconds),
        //            string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
        //            tT.Hours, tT.Minutes, tT.Seconds),
        //            string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
        //            tR.Hours, tR.Minutes, tR.Seconds));
        //    }
        //}
    }
}
