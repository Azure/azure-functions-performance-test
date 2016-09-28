using System;

public static void Run(string sizeMb, out string output, TraceWriter log)
{    
    var bytes = new byte[int.Parse(sizeMb) * 1024 * 1024];    
    output = sizeMb;
}