using System;

class Program
{
    static int add(int x, int y)
    {
        return x + y;
    }

    static void Main()
    {
        var f = (int x) => x + 1;
        Console.WriteLine(add(1, 2));
        Console.WriteLine(f(2));
        Console.WriteLine(((int x) => x + 1)(3));
        Console.WriteLine(Console.ReadLine());
    }
}
