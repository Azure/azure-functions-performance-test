using System;

public static void Run(string sizeMb, out string output, TraceWriter log)
{
    var bytes = new byte[sizeMb * 1024 * 1024];    
    output = sizeMb;
}