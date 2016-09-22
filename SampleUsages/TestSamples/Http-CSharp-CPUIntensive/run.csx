using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host;

public static int[,] CreateRandomMatrix(int size, int seed, int valueMin, int valueMax)
{
    Random rng = new Random(seed);
    var matrix = new int[size, size];
    for (var i = 0; i < size; i++)
    {
        for (var j = 0; j < size; j++)
        {
            matrix[i, j] = rng.Next(valueMin, valueMax);
        }
    }
    return matrix;
}

public static IEnumerable<int> GetRowFromMatrix(int[,] matrix, int row)
{
    for (int j = 0; j < matrix.GetLength(1); j++)
    {
        yield return matrix[row, j];
    }
}

public static IEnumerable<int> GetColumnFromMatrix(int[,] matrix, int col)
{
    for (int i = 0; i < matrix.GetLength(0); i++)
    {
        yield return matrix[i, col];
    }
}

public static int[,] MultiplyMatrix(int[,] matrixA, int[,] matrixB)
{
    int[,] result = new int[matrixA.GetLength(0), matrixB.GetLength(1)];
    for (var i = 0; i < matrixA.GetLength(0); i++)
    {
        for (var j = 0; j < matrixA.GetLength(1); j++)
        {
            var row = GetRowFromMatrix(matrixA, i);
            var col = GetColumnFromMatrix(matrixB, j);
            var rowXCol = Enumerable.Zip(row, col, (ix, xj) => ix * xj);
            result[i, j] = rowXCol.Sum();
        }
    }
    return result;
}

public static void PrintMatrix(TraceWriter log, int[,] matrix)
{
    for (var i = 0; i < matrix.GetLength(0); i++)
    {
        var row = "";
        for (var j = 0; j < matrix.GetLength(1); j++)
        {
            row += " " + matrix[i, j];
        }

        log.Info(row);
    }
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    int size = await req.Content.ReadAsAsync<int>();
    int seed = 123;
    int valueMin = 0;
    int valueMax = 101;

    int[,] matrix = CreateRandomMatrix(size, seed, valueMin, valueMax);
    seed = 2 * seed;
    int[,] matrix2 = CreateRandomMatrix(size, seed, valueMin, valueMax);
    int[,] result = MultiplyMatrix(matrix, matrix2);
    //log.Info("Matrix1: ");
    //PrintMatrix(log, matrix);
    //log.Info("Matrix2: ");
    //PrintMatrix(log, matrix2);
    //log.Info("Result: ");
    //PrintMatrix(log, result);
}