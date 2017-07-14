using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomSubsequenceGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            int startNumber, sequenceCount, subsequenceCount;

            try
            {
                startNumber = int.Parse(args[0]);
                sequenceCount = int.Parse(args[1]);
                subsequenceCount = int.Parse(args[2]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error parsing input arguments!");
                return;
            }

            Random random = new Random();
            LinkedList<int> numbers = new LinkedList<int>(Enumerable.Range(startNumber, sequenceCount));
            for (int i = 0; i < subsequenceCount; i++)
            {
                int randomId = random.Next(numbers.Count - 1);
                
                LinkedListNode<int> node = numbers.First;
                for (int j = 0; j < randomId; j++)
                {
                    node = node.Next;
                }

                Console.WriteLine(node.Value);
                numbers.Remove(node);
            }

        }
    }
}
