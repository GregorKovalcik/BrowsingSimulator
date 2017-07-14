using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering
{
    public class Descriptor
    {
        public int Id { get; private set; }
        public float[] Values { get; private set; }
        public int Dimension
        {
            get
            {
                return (Values != null) ? Values.Length : 0;
            }
        }
        public Centroid Centroid { get; set; }

        public Descriptor(int id, float[] values)
        {
            Id = id;
            Values = values;
        }

        public Descriptor(float[] values)
        {
            Id = -1;
            Values = values;
        }

        public Descriptor(int id, int descriptorDimension)
        {
            Id = id;
            Values = new float[descriptorDimension];
        }


        public void Multiply(float value)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] *= value;
            }
        }

        public static Descriptor operator +(Descriptor a, Descriptor b)
        {
            if (a.Values.Length != b.Values.Length)
            {
                throw new ArgumentException("Descriptors have a different length!");
            }

            Descriptor result = new Descriptor(new float[a.Values.Length]);
            for (int i = 0; i < a.Values.Length; i++)
            {
                result.Values[i] = a.Values[i] + b.Values[i];
            }

            return result;
        }

        public static double GetDistance(float[] a, float[] b)
        {
            double accumulator = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double value = b[i] - a[i];
                accumulator += value * value;
            }
            return Math.Sqrt(accumulator);
        }

        public static float GetDistanceSQR(float[] a, float[] b)
        {
            float accumulator = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float value = b[i] - a[i];
                accumulator += value * value;
            }
            return accumulator;
        }

        public Descriptor Clone()
        {
            return new Descriptor(Id, (float[])Values.Clone());
        }



        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
