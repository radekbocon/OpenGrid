using System;
using System.Collections.Generic;

namespace SimLab.ViewModels;

public static class DataDownsampler
{
    public const int MaxRenderPoints = 3000;

    public static (double[] xs, double[] ys) Decimate(double[] xs, double[] ys, int maxPoints = MaxRenderPoints)
    {
        var n = xs.Length;
        if (n <= maxPoints || n < 4)
            return (xs, ys);

        int bucketCount = maxPoints / 2;
        int bucketSize = (int)Math.Ceiling((double)n / bucketCount);

        var resultXs = new List<double>(maxPoints);
        var resultYs = new List<double>(maxPoints);

        for (int i = 0; i < n; i += bucketSize)
        {
            int end = Math.Min(i + bucketSize, n);

            int minIdx = i;
            int maxIdx = i;
            for (int j = i + 1; j < end; j++)
            {
                if (ys[j] < ys[minIdx]) minIdx = j;
                if (ys[j] > ys[maxIdx]) maxIdx = j;
            }

            if (minIdx == maxIdx)
            {
                resultXs.Add(xs[i]);
                resultYs.Add(ys[i]);
            }
            else
            {
                if (minIdx < maxIdx)
                {
                    resultXs.Add(xs[minIdx]); resultYs.Add(ys[minIdx]);
                    resultXs.Add(xs[maxIdx]); resultYs.Add(ys[maxIdx]);
                }
                else
                {
                    resultXs.Add(xs[maxIdx]); resultYs.Add(ys[maxIdx]);
                    resultXs.Add(xs[minIdx]); resultYs.Add(ys[minIdx]);
                }
            }
        }

        return (resultXs.ToArray(), resultYs.ToArray());
    }
}
