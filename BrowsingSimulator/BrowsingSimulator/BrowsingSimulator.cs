#define PARALLEL

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

#if PARALLEL
            Parallel.For(0, BrowsingSessions.Length, index =>
#else
            for (int index = 0; index < BrowsingSessions.Length; index++)
#endif
            {
                try
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
                    } while (session.BrowsingDepth < maxBrowsingDepth && !session.ItemFound);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            }
#if PARALLEL
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

        
        public void SaveSessionLogs(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);

            // store each session in a separate file
            foreach (BrowsingSession session in BrowsingSessions)
            {
                SaveSessionLog(session, outputDirectory);
            }

            ComputeAndPrintBrowsingHistogram(outputDirectory);
        }

        public void SaveSessionLog(BrowsingSession session, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);

            string sessionFileName = "session_" + session.Id.ToString("000000") + ".log";
            string sessionFilePath = Path.Combine(outputDirectory, sessionFileName);
            using (StreamWriter writer = new StreamWriter(sessionFilePath))
            {
                PrintSessionInfo(session, writer);

                foreach (BrowsingLog log in session.Logs)
                {
                    PrintDisplayInfo(log, writer);
                }
            }
        }

        private static void PrintSessionInfo(BrowsingSession session, StreamWriter writer)
        {
            writer.Write("ID:" + session.Id.ToString("000000"));
            writer.Write(";");
            writer.Write("display_size:" + session.DisplaySize);
            writer.Write(";");
            writer.Write("browsing_depth:" + session.BrowsingDepth);
            writer.Write(";");
            writer.Write("zooming_step:" + session.ZoomingStep);
            writer.Write(";");
            writer.Write("browsing_coherence:" + session.BrowsingCoherence);
            writer.Write(";");
            writer.Write("drop_factor:" + session.DropFactor);
            writer.Write(";");
            writer.Write("searched_item:" + session.SearchedItem.Id.ToString("000000"));
            writer.Write(";");
            writer.Write("mles_path:" 
                + session.SearchedItem.ParentItem.ParentItem.LayerLocalId + "->"
                + session.SearchedItem.ParentItem.LayerLocalId + "->"
                + session.SearchedItem.LayerLocalId);

            writer.WriteLine();
        }

        private static void PrintDisplayInfo(BrowsingLog log, StreamWriter writer)
        {
            writer.Write("ID:" + log.Id.ToString("000000"));
            writer.Write(";");
            writer.Write("layer_depth:" + log.LayerDepth.ToString("00"));
            writer.Write(";");
            writer.Write("browsing_depth:" + log.BrowsingDepth.ToString("00"));
            writer.Write(";");
            writer.Write("selected_item:" + log.SelectedItem.Id.ToString("000000"));
            writer.Write(";");
            writer.Write("searched_item_distance:" + log.SearchedItemDistance.ToString("00000000.00"));
            writer.Write(";display:");

            bool isFirst = true;
            foreach (Item item in log.Display)
            {
                if (!isFirst)
                {
                    writer.Write(":");
                }
                writer.Write(item.Id.ToString("000000"));
                isFirst = false;
            }

            writer.WriteLine();
        }

        private void ComputeAndPrintBrowsingHistogram(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);

            // count maximal browsing depth
            int maxBrowsingDepth = 0;
            foreach (BrowsingSession session in BrowsingSessions)
            {
                if (session.BrowsingDepth > maxBrowsingDepth)
                {
                    maxBrowsingDepth = session.BrowsingDepth;
                }
            }

            // create histogram
            float[] histogram = new float[maxBrowsingDepth];
            int itemFoundCount = 0;
            foreach (BrowsingSession session in BrowsingSessions)
            {
                histogram[session.BrowsingDepth - 1]++;
                if (session.ItemFound)
                {
                    itemFoundCount++;
                }
            }

            // write result
            string fileName = "browsing_histogram.log";
            string filePath = Path.Combine(outputDirectory, fileName);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine(itemFoundCount + "/" + BrowsingSessions.Length);

                for (int i = 0; i < histogram.Length; i++)
                {
                    writer.WriteLine("{0}:{1}", i, histogram[i] / BrowsingSessions.Length);
                }
            }
        }

    }
}
