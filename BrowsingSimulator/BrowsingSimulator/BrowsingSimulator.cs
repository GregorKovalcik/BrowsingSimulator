using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowsingSimulator
{
    public class BrowsingSimulatorEngine
    {
        public MLES Mles { get; protected set; }
        public BrowsingSession[] BrowsingSessions { get; protected set; }

        public BrowsingSimulatorEngine(MLES mles)
        {
            Mles = mles;
        }

        public void RunSimulations(int[] randomDistinctIds, int displaySize, int zoomingStep, int browsingCoherence, 
            float dropFactor = 1.0f, int maxBrowsingDepth = 20, int randomSeed = 5334)
        {
            BrowsingSessions = 
                GenerateBrowsingSessions(randomDistinctIds, displaySize, zoomingStep, browsingCoherence, dropFactor, randomSeed);

#if DEBUG
            for (int index = 0; index < browsingSessions.Length; index++)
#else
            Parallel.For(0, BrowsingSessions.Length, index =>
#endif
            {
                BrowsingSession session = BrowsingSessions[index];
                Console.WriteLine("Launching session ID: {0}. Searched item is in {1} -> {2} -> {3}",
                    session.Id, 
                    session.SearchedItem.ParentItem.ParentItem.LayerLocalId, 
                    session.SearchedItem.ParentItem.LayerLocalId, 
                    session.SearchedItem.LayerLocalId);

                do
                {
                    float itemDistance = session.SelectRandomItemAndGenerateNewDisplay();
                    Console.WriteLine("Session ID: {0}, browsing depth: {1}, item distance: {2}",
                        session.Id, session.BrowsingDepth, itemDistance);
                } while (session.BrowsingDepth < maxBrowsingDepth && !session.DisplayContainsSearchedItem());
                
            }
#if !DEBUG
            );
#endif
        }

        protected BrowsingSession[] GenerateBrowsingSessions(int[] randomDistinctIds, 
            int displaySize, int zoomingStep, int browsingCoherence, float dropFactor, int randomSeed)
        {
            BrowsingSession[] browsingSessions = new BrowsingSession[randomDistinctIds.Length];
            Random random = new Random(randomSeed);
            for (int i = 0; i < randomDistinctIds.Length; i++)
            {
                Item searchedItem = Mles.Dataset[randomDistinctIds[i]];
                browsingSessions[i] = new BrowsingSession(i, displaySize, zoomingStep, browsingCoherence, dropFactor,
                    random.Next(), Mles, searchedItem);
                browsingSessions[i].LoadZeroPageDisplay(Mles.Layers[0]);
            }
            return browsingSessions;
        }


//        protected void LoadZeroPageDisplay(BrowsingSession[] browsingSessions)
//        {
//#if DEBUG
//            for (int index = 0; index < browsingSessions.Length; index++)
//#else
//            Parallel.For(0, browsingSessions.Length, index =>
//#endif
//            {
//                for (int i = 0; i < Mles.Layers[0].Length; i++)
//                {
//                    browsingSessions[index].LoadZeroPageDisplay(Mles.Layers[0]);
//                }
//            }
//#if !DEBUG
//            );
//#endif
//        }
    }
}
