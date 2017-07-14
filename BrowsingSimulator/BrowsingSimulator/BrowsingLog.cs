using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowsingSimulator
{
    public class BrowsingLog
    {
        public int Id { get; private set; }
        public Item[] Display { get; private set; }
        public Item SelectedItem { get; private set; }
        public float SelectedItemDropProbability { get; private set; }
        public float SearchedItemDistance { get; private set; }
        public int LayerDepth { get; private set; }
        public int BrowsingDepth { get; private set; }

        public BrowsingLog(int id, Item[] display, Item selectedItem, float selectedItemDropProbability, float searchedItemDistance,
            int layerDepth, int browsingDepth)
        {
            Id = id;
            Display = display;
            SelectedItem = selectedItem;
            SelectedItemDropProbability = selectedItemDropProbability;
            SearchedItemDistance = searchedItemDistance;
            LayerDepth = layerDepth;
            BrowsingDepth = browsingDepth;
        }
    }
}
