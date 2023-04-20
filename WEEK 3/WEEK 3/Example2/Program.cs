// mutltithreading example
using System;
using System.Threading;

class Example2
{
    static void Main()
    {
        
        Thread t = new Thread(PrintNumbers);
        t.Start();
        Console.WriteLine("Starting...");
        PrintNumbers();
    }
    static void PrintNumbers()
    {
        
        for (int i = 1; i < 10; i++)
        {
            Console.WriteLine(i);
        }
    }
}
