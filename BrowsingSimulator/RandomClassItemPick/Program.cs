using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomClassItemPick
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDirectory = args[0];
            string outputFile = args[1];

            Random random = new Random(5334);

            string[] subdirectories = Directory.GetDirectories(inputDirectory);

            int idOffset = 0;
            int classId = 0;
            using (StreamWriter writer = new StreamWriter(outputFile))
            {
                for (int i = 0; i < subdirectories.Length; i++)
                {
                    string subDirectory = subdirectories[i];

                    // check if directory
                    FileAttributes attr = File.GetAttributes(subDirectory);
                    if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        throw new ArgumentException("Path is not a directory: " + subDirectory);
                    }

                    // count subdir files
                    int fileCount = Directory.GetFiles(subDirectory).Length;

                    // select random file
                    int randomId = random.Next(fileCount - 1);
                    int randomClassItemId = idOffset + randomId;
                    writer.WriteLine(randomClassItemId);

                    // increment variables
                    idOffset += fileCount;
                    classId++;
                }
            }
        }
    }
}
