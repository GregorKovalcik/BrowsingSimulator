using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowsingComparer
{
    class Program
    {
        static void Main(string[] args)
        {
            string simulationFolderA = args[0];
            string simulationFolderB = args[1];

            string[] fileNames = Directory.GetFiles(simulationFolderA);

            int counterA = 0;
            int counterB = 0;
            int sumCounter = 0;
            for (int i = 0; i < fileNames.Length; i++)
            {
                string fileName = Path.GetFileName(fileNames[i]);
                if (fileName.StartsWith("session"))
                {
                    string fileA = Path.Combine(simulationFolderA, fileName);
                    string fileB = Path.Combine(simulationFolderB, fileName);

                    int lineCountA = File.ReadLines(fileA).Count();
                    int lineCountB = File.ReadLines(fileB).Count();

                    if (lineCountA < lineCountB)
                    {
                        counterA++;
                        
                    }
                    else if (lineCountA > lineCountB)
                    {
                        counterB++;
                        
                    }
                    sumCounter++;
                }
            }

            Console.WriteLine("Result A:B:C = {0}:{1}:{2}", 
                counterA * 1.0 / sumCounter, 
                (sumCounter - counterA - counterB) * 1.0 / sumCounter, 
                counterB * 1.0 / sumCounter);

        }
    }
}
