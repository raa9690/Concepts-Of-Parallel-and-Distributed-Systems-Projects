using System;
using System.Threading;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace PrimeGen
{
    /// <summary>
    /// Commandline driven multithreaded program that generates prime numbers. 
    /// Inputs are bitLength, which represents how many bits the prime number(s) 
    /// generated will have, and an optional count argument that determines 
    /// how many prime number(s) to generate.
    /// </summary>
    class PrimeGen
    {
        // bit length of the prime number(s) generated
        static int bitLength;
        // if count is not defined, it will default to 1
        static int count  = 1;
        // variable used to generate the random numbers, since it requires a byte amount, not bit amount
        static int byteLength;
        // currentPrimeCount to keep track of how many prime numbers have been outputed
        static int currentPrimeCount = 0;
        // simple lock for outputting prime numbers
        static Object lockObject = new Object();

        /// <summary>
        /// Reads a request for an amount of prime numbers of a certain bit length. 
        /// Implemented using Miller-Rabin primality test, as well as 
        /// multithreading. Prints the prime's and how long it took to stdout
        /// </summary>
        /// <param name="args">Array of strings from input of size 1 or two. 
        /// first will always be the bitsize of the prime numbers requested, 
        /// and the second optional argument is how many prime numbers to generate.</param>
        static void Main(string[] args)
        {
            if (args.Length > 2 || args.Length < 1)
            {
                Console.WriteLine("Incorrect amount of arguments, expected 1 or 2\nUsage: <bits> <count=1>\r\n- bits - the number of bits of the prime number, this must be a\r\nmultiple of 8, and at least 32 bits.\r\n- count - the number of prime numbers to generate, defaults to 1");
                return;
            }
            else 
            {
                if (!int.TryParse(args[0],out bitLength))
                {
                    Console.WriteLine("First argument must be a valid integer\nUsage: <bits> <count=1>\r\n- bits - the number of bits of the prime number, this must be a\r\nmultiple of 8, and at least 32 bits.\r\n- count - the number of prime numbers to generate, defaults to 1");
                    return;
                }
                // get how many bytes we need
                byteLength = bitLength / 8;
                if (bitLength % 8 != 0)
                {
                    // because integer division is a floor division, if we have a remainder, increase byteLength by 1
                    byteLength++;
                }
            }
            if (args.Length == 2)
            {
                if (!int.TryParse(args[1], out count))
                {
                    Console.WriteLine("Second argument must be a valid integer\nUsage: <bits> <count=1>\r\n- bits - the number of bits of the prime number, this must be a\r\nmultiple of 8, and at least 32 bits.\r\n- count - the number of prime numbers to generate, defaults to 1");
                    return;
                }
            }

            RandomNumberGenerator random = RandomNumberGenerator.Create();

            Console.WriteLine("BitLength : " + bitLength + " bits");
            Console.WriteLine(count);

            // generate a stopWatch right before the bulk of the functionality
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // generate the prime numbers (replace Single with Multi)
            outputPrimeMultiThreaded();

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Console.WriteLine("Time to Generate : " + ts);

        }

        /// <summary>
        /// Tests whether a given number is likely a prime number using the 
        /// Miller-Rabin primality test.
        /// </summary>
        /// <param name="value">Value of the number being tested for primality.</param>
        /// <param name="k">Number of witnesses used for the primality test.</param>
        /// <returns>Whether or not the value is prime.</returns>
        static Boolean IsProbablyPrime(BigInteger value, int k = 10) 
        {
            // simple check to confirm the value is an odd integer
            // includes simple checks for composite numbers (multiples of prime numbers 2, 3, 5, 7)
            // this is meant to impove performance
            if (value < 3 || value % 2 == 0 
                || value % 3 == 0 
                || value % 5 == 0 
                || value % 7 == 0 
                || value % 9 == 0
                || value % 11 == 0
                || value % 13 == 0
                || value % 17 == 0
                || value % 19 == 0) 
            {
                return false;
            }

            // now setup the required values for the Miller-Rabin primality algorithm
            BigInteger n = value;
            // d starts as equal to n, then as we divide by 2, we check if d is odd
            BigInteger d = n;
            // as we divide d, find the largest odd value of d, with
            // r will be defined by n - 1 = 2^r * d where d is odd,
            // and both d and r are integers
            BigInteger r = 0;
            // do while loop as r > 0
            do 
            {
                d = d / 2; // effectively factor out a 2, and increase the exponent r
                r++;
            } while (d % 2 == 0);

            // with n, d & r defined, find a
            BigInteger a = 0;
            while (a < 2 || a > n - 2) 
            {
                // generate an a that is in the range [2, n - 2] (both inclusive)
                a = new BigInteger(RandomNumberGenerator.GetBytes(byteLength));
            }
            BigInteger x = 0;
            while (k > 0) // witness loop
            {
                // having a, d & n, generate x in the form: x = a^d mod n
                x = BigInteger.ModPow(a, d, n);

                // first escape check for current loop
                if (x == 1 || x == n - 1) 
                {
                    k--;
                    continue;
                }

                // define rCounter to use as a counter for the internal while
                // loops while not changing r
                BigInteger rCounter= r;
                while (rCounter > 1) // r loop
                {
                    // x = x^2 mod n
                    x = BigInteger.ModPow(x,2,n);
                    // if x ==  n-1 break out of the internal r loop
                    if (x == n - 1) break;
                    rCounter--;
                }
                // if we broke out of the r loop, then we continue the witness
                // loop, if not, return false (the number is composite) 
                if (x != n - 1) 
                {
                    return false;    
                }
                k--;
            }
            // if we reach the end of the witness loop without returning false
            // then assume the number is likely a prime
            return true; 
        }

        /// <summary>
        /// Generates a prime number by looping generating a random number of 
        /// a given bytesize, testing if it's prime, if so, return it, else,
        /// continue the loop.
        /// </summary>
        /// <returns>A number very likely to be prime.</returns>
        static BigInteger generateProbablyPrime() {
            BigInteger possiblePrime = 0;
            do
            {
                // generate a random array of bytes
                // and garentee it's  greater than 1 (if less than or equal to 1, it'll never be prime)
                possiblePrime = new BigInteger(RandomNumberGenerator.GetBytes(byteLength));
                // simple cleanup of negative numbers
                if (possiblePrime < 0) possiblePrime = possiblePrime * -1;
            } while (!IsProbablyPrime(possiblePrime));
            return possiblePrime;
        }

        /// <summary>
        /// Function that outputs a given count of prime numbers of a given 
        /// bitLength. Implemented using single threading.
        /// </summary>
        static void outputPrimeSingleThreaded() {
            while (currentPrimeCount < count)
            {
                // generate a random BigInteger, and see if it's probably a prime
                // uses byteLength + 1 since BigInteger is signed, and 1 byte is used for the sign
                var possiblePrime = generateProbablyPrime();
                currentPrimeCount++;
                Console.WriteLine("\n" + currentPrimeCount + ": " + possiblePrime);
            }
        }

        /// <summary>
        /// Function that outputs a given count of prime numbers of a given bitLength. 
        /// Impemented using multithreading. Uses ThreadPool to optimize how 
        /// many threads are created and managed.
        /// </summary>
        static void outputPrimeMultiThreaded() { 
            // create lots of threads
            for (int i = 0; i < 100; i++) {
                ThreadPool.QueueUserWorkItem(new WaitCallback(outputPrime));
            }
            // loop until the correct number of prime numbers have been outputed, then exit
            while (currentPrimeCount < count) 
            {
                // just wait, do nothing, not much to see here
            }
            return;
        }

        /// <summary>
        /// Function that outputs a single prime number, then increases the 
        /// currentPrimeCount.
        /// </summary>
        /// <param name="callback">An object containing information to be used by the callback method.</param>
        static void outputPrime(object callback) 
        {
            while (currentPrimeCount < count)
            {
                // find the number that is highly likely to be prime
                BigInteger possiblePrime = generateProbablyPrime();

                lock (lockObject)
                {
                    // sanity check before outputing
                    if (currentPrimeCount < count) {
                        currentPrimeCount++;
                        Console.WriteLine("\n" + currentPrimeCount + ": " + possiblePrime);
                    }
                }
            }
            return;
        }

        
    }
}