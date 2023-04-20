using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Project1
{
    class Program
    {
        static void Main(string[] args) {
            if (args.Length != 2)
            {
                Console.Out.WriteLine("Usage: du [-s] [-p] [-b] <path>\r\nSummarize disk usage of the set of FILEs, recursively for directories.\r\n\r\nYou MUST specify one of the parameters, -s, -p, or -b\r\n-s       Run in single threaded mode\r\n-p       Run in parallel mode (uses all available processors)\r\n-b       Run in both single threaded and parallel mode.\r\n         Runs parallel follow by sequential mode");
            }
            var tag = args[0];
            // test the file path immediately to see whether it exists or not
            var file_path = args[1];

            if (tag == "-s")
            {
                Console.Out.WriteLine("trying to read files singlethreaded");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                long[] output = SReadDirectory(file_path);
                stopwatch.Stop();
                printResults("Single", output, stopwatch.Elapsed);
            }
            else if (tag == "-d")
            {
                Console.Out.WriteLine("trying to read files multithreaded");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                long[] output = TReadDirectory(file_path);
                stopwatch.Stop();
                printResults("Parallel", output, stopwatch.Elapsed);
            }
            else if (tag == "-b")
            {
                Console.Out.WriteLine("trying to read files both singlethreaded and multithreaded");
                Stopwatch stopwatchS = new Stopwatch();
                stopwatchS.Start();
                long[] outputS = SReadDirectory(file_path);
                stopwatchS.Stop();
                printResults("Single", outputS, stopwatchS.Elapsed);
                Console.Out.WriteLine("Next");
                Stopwatch stopwatchT = new Stopwatch();
                stopwatchT.Start();
                long[] outputT = TReadDirectory(file_path);
                stopwatchT.Stop();
                printResults("Parallel", outputT, stopwatchT.Elapsed);
            }
            else 
            {
                Console.Out.WriteLine("Usage: du [-s] [-p] [-b] <path>\r\nSummarize disk usage of the set of FILEs, recursively for directories.\r\n\r\nYou MUST specify one of the parameters, -s, -p, or -b\r\n-s       Run in single threaded mode\r\n-p       Run in parallel mode (uses all available processors)\r\n-b       Run in both single threaded and parallel mode.\r\n         Runs parallel follow by sequential mode");
            }
            
        }

        /// <summary>Read through all files and folders in the filepath using a threaded ForEach, 
        /// getting how many folders, files and bytes were read, returned as a size 3 array of longs.
        /// </summary>
        /// <param name="file_path"></param>
        static long[] TReadDirectory(String file_path)
        {
            // integer array to hold home many folders, files and bytes were read in, in that order
            long[] outputArray = new long[3];
            outputArray[0] = 0;
            outputArray[1] = 0;
            outputArray[2] = 0;
            object lockObject1 = new object();
            object lockObject2 = new object();
            object lockObject3 = new object();  
            // also initiate the array to contain all paths to recurse, outside of the try catch to avoid scope
            // issues
            string[] allPathsFromDirectory = null;
            try
            {
                allPathsFromDirectory = Directory.GetFileSystemEntries(file_path);
                // if no exception was thrown, it has successfully read itself
                outputArray[0]++;
            }
            // won't ignore files it can't read, but it will catch the exception, then 
            catch (System.Security.SecurityException)
            {
                // can't read the bath, so return the empty array, no folders nor files nor bytes were read
                return outputArray;
            }
            // same senario
            catch (System.UnauthorizedAccessException)
            {
                return outputArray;
            }


            Parallel.ForEach(allPathsFromDirectory, path =>
            {
                // get the attributes of the path
                var attr = File.GetAttributes(path);
                // if it has a directory path, increase folder path by 1 and recurse
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    long[] recurseOutput = TReadDirectory(path);
                    // lock required
                    Interlocked.Add(ref outputArray[0], recurseOutput[0]);
                    Interlocked.Add(ref outputArray[1], recurseOutput[1]);
                    Interlocked.Add(ref outputArray[2], recurseOutput[2]);

                }
                else
                {
                    var info = new FileInfo(path);
                    // lock required
                    Interlocked.Increment(ref outputArray[1]);
                    Interlocked.Add(ref outputArray[2], info.Length);
                }
            });
            return outputArray;
        }

        /// <summary>Read through all files and folders in the filepath using a regular ForEach, 
        /// getting how many folders, files and bytes were read, returned as a size 3 array of longs.
        /// </summary>
        /// <param name="file_path"></param>
        static long[] SReadDirectory(String file_path)
        {
            // integer array to hold home many folders, files and bytes were read in, in that order
            long[] outputArray = new long[3];
            outputArray[0] = 0;
            outputArray[1] = 0;
            outputArray[2] = 0;
            // also initiate the array to contain all paths to recurse, outside of the try catch to avoid scope
            // issues
            string[] allPathsFromDirectory = null;
            try
            {
                allPathsFromDirectory = Directory.GetFileSystemEntries(file_path);
                // if no exception was thrown, it has successfully read itself
                outputArray[0]++;
            }
            // won't ignore files it can't read, but it will catch the exception, then 
            catch (System.Security.SecurityException)
            {
                // can't read the bath, so return the empty array, no folders nor files nor bytes were read
                return outputArray;
            }
            // same senario
            catch (System.UnauthorizedAccessException) 
            {
                return outputArray;
            }

            foreach (string path in allPathsFromDirectory) {
                // get the attributes of the path
                var attr = File.GetAttributes(path);
                // if it has a directory path, increase folder path by 1 and recurse
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    long[] recurseOutput = SReadDirectory(path);
                    outputArray[0] += recurseOutput[0];
                    outputArray[1] += recurseOutput[1];
                    outputArray[2] += recurseOutput[2];
                }
                else {
                    var info = new FileInfo(path);
                    outputArray[1]++;
                    outputArray[2] += info.Length;
                }
            }
            return outputArray;
        }

        /// <summary>Given a name of the method used, a long[] outputArray, and a TimeSpan value, 
        /// print out the results in a meaningful manner.
        /// </summary>
        /// <param name="method">
        /// String name of the method used
        /// </param>
        /// <param name="outputArray">
        /// long[] of output values for an amount of folders, files and bytes read
        /// </param>
        /// <param name="timeElapsed">
        /// TimeSpan amount used to print out how long the method took from start to finish, using StopWatch
        /// </param>
        static void printResults(String method, long[] outputArray, TimeSpan timeElapsed) {
            double timeElapsedSeconds = timeElapsed.TotalSeconds;
            Console.Out.WriteLine(method + " Calculated in: " + timeElapsedSeconds + "s");
            Console.Out.WriteLine(outputArray[0] + " folders, " + outputArray[1] + " files, " + outputArray[2] + " bytes");
        }
    }
}