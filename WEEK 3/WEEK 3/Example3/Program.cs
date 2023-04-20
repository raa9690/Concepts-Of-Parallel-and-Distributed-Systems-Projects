// thread sleeping for 2 seconds...
using System;
using System.Threading;

class Example2
{
    static void Main()
    {
        
        Thread t = new Thread(PrintNumbersWithDelay);
        t.Start();
        PrintNumbers(); 
    }
    static void PrintNumbers()
    {
        Console.WriteLine("Starting...");
        for (int i = 1; i < 10; i++)
        {
            Console.WriteLine(i);
        }
    }
    static void PrintNumbersWithDelay()
    {
        Console.WriteLine("Starting...");
        for (int i = 1; i < 10; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Console.WriteLine(i);
        }
    }
}
