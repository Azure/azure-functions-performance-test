using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    int size = await req.Content.ReadAsAsync<int>();
    int seed = 123;
    int valueMin = 0;
    int valueMax = 101;

    int[][] matrix = CreateRandomMatrix(size, seed, valueMin, valueMax);
    seed = 2 * seed;
    int[][] matrix2 = CreateRandomMatrix(size, seed, valueMin, valueMax);
    int[][] result = MultiplyMatrix(matrix, matrix2);

    return req.CreateResponse(HttpStatusCode.OK);
}

public static int[][] CreateRandomMatrix(int size, int seed, int valueMin, int valueMax)
{
    Random rng = new Random(seed);
    var matrix = new int[size][];
    for (var i = 0; i < size; i++)
    {
        var row = new int[size];
        for (var j = 0; j < size; j++)
        {
            row[j] = rng.Next(valueMin, valueMax);
        }

        matrix[i] = row;
    }
    return matrix;
}

public static int[][] MultiplyMatrix(int[][] matrixA, int[][] matrixB)
{
    int[][] result = new int[matrixA.GetLength(0)][];
    int elements = matrixB.GetLength(0);
    for (var i = 0; i < matrixA.GetLength(0); i++)
    {
        result[i] = new int[elements];
        for (var j = 0; j < matrixA.GetLength(0); j++)
        {
            var row = matrixA[i];
            var col = GetColumn(matrixB, j);

            result[i][j] = MultiplyRowAndColumn(row, col);
        }
    }
    return result;
}

private static int MultiplyRowAndColumn(int[] row, int[] col)
{
    int sum = 0;
    for (int b = 0; b < row.Length; b++)
    {
        sum += row[b] * col[b];
    }

    return sum;
}

private static int[] GetColumn(int[][] matrixB, int j)
{
    var result = new int[matrixB.Length];
    for (int i = 0; i < matrixB.Length; i++)
    {
        result[i] = matrixB[i][j];
    }

    return result;
}

public static void PrintMatrix(int[][] matrix)
{
    for (var i = 0; i < matrix.Length; i++)
    {
        var row = "";
        for (var j = 0; j < matrix.Length; j++)
        {
            row += " " + matrix[i][j];
        }

        Console.WriteLine(row);
    }
}