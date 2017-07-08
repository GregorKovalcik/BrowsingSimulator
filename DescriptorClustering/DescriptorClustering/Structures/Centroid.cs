using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering
{
    public class Centroid
    {
        public int Id { get; private set; }
        public Descriptor Mean { get; private set; }
        public List<Descriptor> Descriptors { get; private set; }
        private object lockObject = new object();

        public Centroid()
        {
            Id = -1;
            Mean = null;
            Descriptors = new List<Descriptor>();
        }

        public Centroid(int id, Descriptor seed)
        {
            Id = id;
            Mean = seed;
            Descriptors = new List<Descriptor>();
        }

        public void AddDescriptorConcurrent(Descriptor descriptor)
        {
            lock (lockObject)
            {
                Descriptors.Add(descriptor);
            }
        }

        public void ClearDescriptors()
        {
            Descriptors.Clear();
        }

        public Descriptor ComputeMean()
        {
            if (Descriptors.Count == 0)
            {
                Mean = null;
            }
            else
            {
                Descriptor sum = new Descriptor(new float[Descriptors[0].Values.Length]);
                foreach (Descriptor descriptor in Descriptors)
                {
                    sum += descriptor;
                }

                sum.Multiply(1.0f / Descriptors.Count);
                Mean = sum;
            }

            return Mean;
        }

        public void AssignClosestDescriptor(Descriptor descriptor)
        {
            Mean = descriptor;
        }
    }
}
