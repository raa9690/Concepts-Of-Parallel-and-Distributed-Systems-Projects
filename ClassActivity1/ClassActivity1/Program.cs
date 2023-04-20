using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;

namespace ClassActivity1
{
    class Program
    {
        // input array of stuff to do an operation on
        private int[,] inputArray = new int[5, 2] { {1, 11},{2, 12}, {3, 13}, {4, 14}, {5, 15} };
        // output array of the results of the operation
        private int[,] outputArray = new int[5, 2] { {0, 0 }, {0, 0 }, {0, 0 }, {0, 0},{0, 0} };
        static void Main(string[] args)
        {
            Thread writer = new Thread(ApplyOperation);

        }

        static void ApplyOperation() {
            for (int i = 0; i < 5; i++) {
                int result = operate(inputArray[i]);
            }
        }

        static void ReadResult() {
            
        }

        public int operate(int[] args) {
            int total = 0;
            for (int i = 0; i < args.Length; i++)
            {
                total += args[i];
            }
            return total;
        }
    }
}