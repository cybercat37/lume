using System;

class Program
{
    static void Main()
    {
        var x = 1;
        {
            Console.WriteLine("hi");
            x = 2;
        }
        Console.WriteLine(x);
    }
}
