using System;

public static void Run(string sizeMb, out string output, TraceWriter log)
{
    var arrSize = int.Parse(sizeMb) * 1024 * 1024;
    var bytes = new byte[arrSize];
    for (int i = 0; i < arrSize; i++)
    {
        bytes[i] = (byte)(i % 255);
    }
    output = sizeMb;
}