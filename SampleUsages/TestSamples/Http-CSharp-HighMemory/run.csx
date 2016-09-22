using System;
using System.Net;
using System.Net.Http;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    int sizeMb = await req.Content.ReadAsAsync<int>();
    var bytes = new byte[sizeMb * 1024 * 1024];    
    return req.CreateResponse(HttpStatusCode.OK);
}

