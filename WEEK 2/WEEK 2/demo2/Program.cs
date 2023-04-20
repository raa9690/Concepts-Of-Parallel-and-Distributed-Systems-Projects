using System;
using System.IO;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var di = Directory.GetFiles(".");
            foreach (var d in di)
            {
                var f = new FileInfo(d);
                f.Refresh();
                var fileTime = f.LastWriteTime.ToString("MMM dd HH:mm");
                System.Console.WriteLine("{0, 10} {1} {2}", f.Length, fileTime, f);
            }
           
        }
    }
}
