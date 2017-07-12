using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowsingSimulator
{
    public class BrowsingSession
    {
        public int Id { get; protected set; }
        public int DisplaySize { get; protected set; }
        public int ZoomingStep { get; protected set; }
        public int CurrentZoomingStep { get; protected set; }
        public int BrowsingCoherence { get; protected set; }
        public int LayerDepth { get; protected set; }
        public float DropFactor { get; protected set; }
        public MLES Mles { get; protected set; }
        public Item SearchedItem { get; protected set; }
        public int BrowsingDepth { get; protected set; }

        public List<Item> Display { get; protected set; }
        public LinkedList<BrowsingLog> Log { get; protected set; }

        public Dictionary<int, int> DisplayedCounter { get; protected set; }
        private Random random;


        public BrowsingSession(int id, int displaySize, int zoomingStep, int browsingCoherence, float dropFactor, int randomSeed, MLES mles,
            Item searchedItem)
        {
            Id = id;
            DisplaySize = displaySize;
            ZoomingStep = zoomingStep;
            CurrentZoomingStep = 0;
            BrowsingCoherence = browsingCoherence;
            DropFactor = dropFactor;
            LayerDepth = 0;
            random = new Random(randomSeed);
            DisplayedCounter = new Dictionary<int, int>();
            Mles = mles;
            SearchedItem = searchedItem;
            Display = new List<Item>();
            BrowsingDepth = 0;
            Log = new LinkedList<BrowsingLog>();
        }

        public void LoadZeroPageDisplay(IEnumerable<Item> zeroPageItems)
        {
            Display.AddRange(zeroPageItems);
            IncrementDisplayedCount(zeroPageItems);
        }


        public float SelectRandomItemAndGenerateNewDisplay()
        {
            Item query = SelectRandomItem();
            Console.WriteLine("Layer {0}, selected {1}", LayerDepth, query.LayerLocalId);
            Log.AddLast(new BrowsingLog(Display.ToArray(), query, LayerDepth, BrowsingDepth));

            // zoom vs pan
            if ((LayerDepth < Mles.Layers.Length - 1) && (CurrentZoomingStep++) % ZoomingStep == 0)
            {
                // zoom
                LayerDepth++;
                GenerateNewDisplayZoom(query, LayerDepth, DisplaySize);
            }
            else
            {
                // pan
                GenerateNewDisplayPan(query, LayerDepth, DisplaySize);
            }

            BrowsingDepth++;
            return Item.GetDistanceSQR(query.Descriptor, SearchedItem.Descriptor);
        }


        public bool DisplayContainsSearchedItem()
        {
            foreach (Item item in Display)
            {
                if (item.Id == SearchedItem.Id)
                {
                    return true;
                }
            }
            return false;
        }


        protected Item SelectRandomItem()
        {
            Item[] nearestItems = Mles.SearchKNN(SearchedItem, Display.ToArray(), BrowsingCoherence, item => false);
            if (nearestItems.Length == 1)
            {
                return nearestItems[0];
            }
            else
            {
                return nearestItems[random.Next(nearestItems.Length - 1)];
            }
        }


        protected void GenerateNewDisplayZoom(Item query, int layerId, int nResults)
        {
            Display.Clear();

            Item[] zoomItems = Mles.SearchKNN(query, query.ClusterItems, nResults, HasItemDroppedOut);
            List<Item> displayItems = new List<Item>(zoomItems);
            LoopAddLayerItems(query, layerId, nResults, displayItems);
            IncrementDisplayedCount(displayItems);

            Display = displayItems;
        }


        protected void GenerateNewDisplayPan(Item query, int layerId, int nResults)
        {
            Display.Clear();

            List<Item> displayItems = new List<Item>();
            LoopAddLayerItems(query, layerId, nResults, displayItems);
            IncrementDisplayedCount(displayItems);

            Display = displayItems;
        }


        protected void IncrementDisplayedCount(IEnumerable<Item> displayItems)
        {
            foreach (Item item in displayItems)
            {
                if (DisplayedCounter.ContainsKey(item.Id))
                {
                    DisplayedCounter[item.Id]++;
                }
                else
                {
                    DisplayedCounter.Add(item.Id, 1);
                }
            }
        }


        protected void LoopAddLayerItems(Item query, int layerId, int nResults, List<Item> displayItems)
        {
            int loopWatchdog = 0;
            while (displayItems.Count < nResults)
            {
                // not enough items in zoomed cluster, add additional files from outside the cluster but from the same layer
                int nAdditionalResults = nResults - displayItems.Count;
                Item[] additionalItems = Mles.SearchKNN(query, layerId, nAdditionalResults, HasItemDroppedOut, displayItems);
                displayItems.AddRange(additionalItems);

                if (loopWatchdog++ > 10)
                {
                    throw new NotImplementedException("Trouble filling whole display...");
                }
            }
        }


        protected bool HasItemDroppedOut(int id)
        {
            if (!DisplayedCounter.ContainsKey(id))  // the first hit
            {
                return false;
            }
            else                                    // the second and other hits
            {
                int hitCount = DisplayedCounter[id];
                double dropProbability = 1 - (1 / (hitCount * DropFactor));
                bool hasItemDroppedOut = random.NextDouble() < dropProbability;

                return hasItemDroppedOut;
            }
        }
        
    }

    public class BrowsingLog
    {
        public Item[] Display { get; private set; }
        public Item SelectedItem { get; private set; }
        public int LayerDepth { get; private set; }
        public int BrowsingDepth { get; private set; }

        public BrowsingLog(Item[] display, Item selectedItem, int layerDepth, int browsingDepth)
        {
            Display = display;
            SelectedItem = selectedItem;
            LayerDepth = layerDepth;
            BrowsingDepth = browsingDepth;
        }
    }
}
