using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowsingSimulator
{
    public class Item
    {
        public int Id { get; protected set; }
        public int LayerLocalId { get; protected set; }
        public float[] Descriptor { get; protected set; }
        public Item ParentItem { get; protected set; }
        public Item[] ClusterItems { get; protected set; }

        public Item(int id, int layerLocalId, float[] descriptor, Item[] clusterItems)
        {
            Id = id;
            LayerLocalId = layerLocalId;
            Descriptor = descriptor;
            ClusterItems = clusterItems;
            ParentItem = null;
        }

        public void SetParentItem(Item parentItem)
        {
            ParentItem = parentItem;
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
    }
}
