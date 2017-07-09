using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Simple
{
    public abstract class ClusteringSimpleBase : ClusteringBase
    {
        public Centroid[] Centroids { get; protected set; }

        public ClusteringSimpleBase(Descriptor[] descriptors) : base(descriptors)
        {
            Centroids = null;
        }

        public virtual double[] Clusterize(double seedPercentage, int iterationCount, int randomSeed)  // TODO: callback
        {
            if (seedPercentage > 1 || seedPercentage <= 0)
            {
                throw new ArgumentException("Seed percentage out of range (0, 1]!");
            }

            int seedCount = (int)(Descriptors.Length * seedPercentage);
            seedCount = (seedCount != 0) ? seedCount : 1;

            return Clusterize(seedCount, iterationCount, randomSeed);
        }

        public virtual double[] Clusterize(int seedCount, int iterationCount, int randomSeed)  // TODO: callback
        {
            Centroids = GenerateRandomSeeds(seedCount, randomSeed);

#if VERBOSE
            Console.WriteLine("Running {0} iterations:", iterationCount);
#endif

            double[] updateDeltas = new double[iterationCount];
            for (int i = 0; i < iterationCount; i++)
            {
#if VERBOSE
                Console.WriteLine("Iteration {0}, assign phase.", i);
#endif

                Assign();

#if VERBOSE
                Console.WriteLine("Iteration {0}, update phase.", i);
#endif

                updateDeltas[i] = Update();

                int droppedCount = DropEmptyCentroids();
#if VERBOSE
                Console.WriteLine("Iteration {0}, dropping phase: {1} dropped.", i, droppedCount);
#endif

                // LogProgress(id, i + 1, imageFilelist.Count, stopwatch, ref lastMiliseconds);
            }
#if VERBOSE
            Console.WriteLine("Iterating finished. Asigning the closest descriptors.");
#endif

            AssignClosestDescriptors(Centroids, Descriptors);

            return updateDeltas;
        }

        protected abstract void Assign();

        protected abstract double Update();

        protected abstract int DropEmptyCentroids();
    }
}
