using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateClassMappingFile
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputDirectory = args[0];
            string outputFile = args[1];
            
            string[] subdirectories = Directory.GetDirectories(inputDirectory);

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

                    // write class mapping for each file
                    for (int f = 0; f < fileCount; f++)
                    {
                        writer.WriteLine(classId);
                    }

                    // increment variable
                    classId++;
                }
            }
        }
    }
}
