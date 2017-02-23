using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace hashCaches
{
    struct Request
    {
        public int Video;
        public int Endpoint;
        public int Number;
    }

    static class Program
    {
        static int videosCount;
        static int endpointsCount;
        static int requestsCount;
        static int cachesCount;
        static int cacheCapacity;
        static Dictionary<int, Request[]> videoRequests;
        static Request[,][] cacheVideoRequests;
        static bool[,] cacheEndpointConnections;
        static void Main(string[] args)
        {
            var infile = args.Length == 0 ? "me_at_the_zoo.in" : args[0];

            int[] videoSizes;
            int[,] latencies;
            Request[] requests;
            using (var sr = new StreamReader(infile))
            {
                var line = sr.ReadLine();
                var parts = line.Split(' ');
                videosCount = int.Parse(parts[0]);
                endpointsCount = int.Parse(parts[1]);
                requestsCount = int.Parse(parts[2]);
                cachesCount = int.Parse(parts[3]);
                cacheCapacity = int.Parse(parts[4]);

                cacheVideoRequests = new Request[cachesCount,videosCount][];
                cacheEndpointConnections = new bool[cachesCount,endpointsCount];
                line = sr.ReadLine();
                videoSizes = line.Split(' ').Select(Int32.Parse).ToArray();
                latencies = new int[cachesCount + 1, endpointsCount];
                for (int e = 0; e < endpointsCount; e++)
                {
                    line = sr.ReadLine();
                    parts = line.Split(' ');
                    var dcLatency = Int32.Parse(parts[0]);
                    for (int c = 0; c <= cachesCount; c++)
                    {
                        latencies[c, e] = dcLatency;
                    }
                    var connCount = Int32.Parse(parts[1]);
                    for (int c = 0; c < connCount; c++)
                    {
                        line = sr.ReadLine();
                        parts = line.Split(' ');
                        var cacheNumber = Int32.Parse(parts[0]);
                        cacheEndpointConnections[cacheNumber, e] = true;
                        latencies[cacheNumber, e] = Int32.Parse(parts[1]);
                    }
                }

                requests = new Request[requestsCount];
                for (int r = 0; r < requestsCount; r++)
                {
                    line = sr.ReadLine();
                    parts = line.Split(' ');
                    requests[r] = new Request
                    {
                        Video = Int32.Parse(parts[0]),
                        Endpoint = Int32.Parse(parts[1]),
                        Number = Int32.Parse(parts[2]),
                    };
                }
                videoRequests = requests.GroupBy(r => r.Video).ToDictionary(r => r.Key, r => r.ToArray());
                for (int c = 0; c < cachesCount; c++)
                {
                    for (int v = 0; v < videosCount; v++)
                    {
                        cacheVideoRequests[c, v] =
                            requests.Where(r => r.Video == v && cacheEndpointConnections[c, r.Endpoint]).ToArray();
                    }
                }
            }

            bool[,] videoPlacements = new bool[cachesCount, videosCount];

            double[,] score = new double[cachesCount, videosCount];

            //for (int c = 0; c < cachesCount; c++)
            //{
            //    for (int v = 0; v < videosCount; v++)
            //    {
            //        double placementEconomy = 0;
            //        for (int r = 0; r < requestsCount; r++)
            //        {
            //            var request = requests[r];
            //            if (request.Video == v)
            //            {
            //                placementEconomy +=
            //                    request.Number * latencies[c, request.Endpoint] / videoSizes[v];
            //            }
            //
            //        }
            //        score[c, v] = placementEconomy;
            //    }
            //}

            int[] cacheFreeCapacities = new int[cachesCount];
            for (int c = 0; c < cachesCount; c++)
            {
                cacheFreeCapacities[c] = cacheCapacity;
            }
            Console.WriteLine("BEGIIN");
            for (int v = 0; v < videosCount; v++)
            {
                FillVideoEconomies(score, v, videoPlacements, latencies, requests, videoSizes);
            }
            while (true)
            {
                var bestEconomy = ArgMax(score);
                var candCache = bestEconomy[0];
                var candVideo = bestEconomy[1];
                if (score[candCache, candVideo] == 0)
                    break;
                if (videoPlacements[candCache, candVideo])
                {
                    score[candCache, candVideo] = 0;
                    continue;
                }
                if (videoSizes[candVideo] > cacheFreeCapacities[candCache])
                {
                    score[candCache, candVideo] = 0;
                    continue;
                }
                videoPlacements[candCache, candVideo] = true;
                cacheFreeCapacities[candCache] -= videoSizes[candVideo];
                for (int v = 0; v < videosCount; v++)
                {
                    FillVideoEconomies(score, v, videoPlacements, latencies, requests, videoSizes);
                }
            }

            using (var sw = new StreamWriter(Path.GetFileNameWithoutExtension(infile) + ".out"))
            {
                sw.WriteLine(cachesCount);
                for (int c = 0; c < cachesCount; c++)
                {
                    sw.Write("{0}", c);
                    for (int v = 0; v < videosCount; v++)
                    {
                        if (videoPlacements[c, v])
                        {
                            sw.Write(" {0}", v);
                        }
                    }
                    sw.WriteLine();
                }
            }
        }

        private static int[] ArgMax(double[,] score)
        {
            var result = new int[2];
            var top = -1D;
            for (int i = 0; i < score.GetLength(0); i++)
            {
                for (int j = 0; j < score.GetLength(1); j++)
                {
                    if (score[i, j] > top)
                    {
                        top = score[i, j];
                        result[0] = i;
                        result[1] = j;
                    }
                }
            }
            return result;
        }

        private static void FillVideoEconomies(double[,] score, int v, bool[,] videoPlacements, int[,] latencies, Request[] requests, int[] videoSizes)
        {
            for (int c = 0; c < cachesCount; c++)
            {
                int placementEconomy = 0;
                Request[] relevantRequests = cacheVideoRequests[c, v];
                if (relevantRequests == null) continue;
                
                Request[] thisVideoRequests;
                if (!videoRequests.TryGetValue(v, out thisVideoRequests))
                {
                    continue;
                }
                for (int r = 0; r < thisVideoRequests.Length; r++)
                {
                    var request = thisVideoRequests[r];
                    var thisCacheLatency = latencies[c, request.Endpoint];

                    if (latencies[cachesCount, request.Endpoint] == thisCacheLatency)
                        continue;
                    var otherBestLatency = Int32.MaxValue;
                    for (int c2 = 0; c2 < cachesCount; c2++)
                    {
                        if (videoPlacements[c2, v])
                        {
                            var candLatency = latencies[c2, request.Endpoint];
                            if (candLatency < otherBestLatency)
                                otherBestLatency = candLatency;
                        }
                    }
                    if (otherBestLatency <= thisCacheLatency)
                        continue;
                    int thisCacheGain;
                    if (otherBestLatency == Int32.MaxValue)
                        thisCacheGain = latencies[cachesCount, request.Endpoint] - thisCacheLatency;
                    else
                        thisCacheGain = otherBestLatency - thisCacheLatency;
                    placementEconomy +=
                        request.Number * thisCacheGain;
                }
                score[c, v] = (double)placementEconomy / videoSizes[v];
            }
        }
    }
}
