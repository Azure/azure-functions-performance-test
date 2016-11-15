using System;
using System.Net;
using System.Net.Http;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    int sizeMb = await req.Content.ReadAsAsync<int>();
    var arrSize = sizeMb * 1024 * 1024;
    var bytes = new byte[arrSize];
    for (int i = 0; i < arrSize; i += 256)
    {
        bytes[i] = (byte)(1);
    }
    return req.CreateResponse(HttpStatusCode.OK);
}

