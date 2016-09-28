using System;
using System.Net;
using System.Net.Http;

public static HttpResponseMessage Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
    return req.CreateResponse(HttpStatusCode.OK);
}