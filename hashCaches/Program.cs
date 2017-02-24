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

    struct Point
    {
        public int c;
        public int v;
        public double score;
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
            var defaultFile = "videos_worth_spreading.in";//"me_at_the_zoo.in"
            var infile = args.Length == 0 ? defaultFile : args[0];

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

                cacheVideoRequests = new Request[cachesCount, videosCount][];
                cacheEndpointConnections = new bool[cachesCount, endpointsCount];
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
                    foreach (var videoRequest in videoRequests)
                    {
                        cacheVideoRequests[c, videoRequest.Key] =
                            videoRequest.Value.Where(r => cacheEndpointConnections[c, r.Endpoint]).ToArray();
                    }
                }
            }

            bool[,] videoPlacements = new bool[cachesCount, videosCount];

            double[,] score = new double[cachesCount, videosCount];

            int[] cacheFreeCapacities = new int[cachesCount];
            for (int c = 0; c < cachesCount; c++)
            {
                cacheFreeCapacities[c] = cacheCapacity;
            }
            Console.WriteLine("BEGIIN");

            var order = new List<Point>();
            for (int v = 0; v < videosCount; v++)
            {
                FillVideoEconomies(score, v, videoPlacements, latencies, requests, videoSizes, order);
            }
            Console.WriteLine("Economies");
            order.Sort((p1, p2) => -p1.score.CompareTo(p2.score));
            Console.WriteLine("Sorted");
            while (true)
            {
                Console.WriteLine("round");
                bool somethingdone = false;
                for (int i = 0; i < 1000 && i < order.Count; i++)
                {
                    var candCache = order[i].c;
                    var candVideo = order[i].v;
                    var sss = order[i].score;
                    if (sss == 0)
                        goto done;
                    if (videoPlacements[candCache, candVideo])
                    {
                        //score[candCache, candVideo] = 0;
                        continue;
                    }
                    if (videoSizes[candVideo] > cacheFreeCapacities[candCache])
                    {
                        //score[candCache, candVideo] = 0;
                        continue;
                    }
                    somethingdone = true;
                    videoPlacements[candCache, candVideo] = true;
                    cacheFreeCapacities[candCache] -= videoSizes[candVideo];
                }
                if (!somethingdone) goto done;
                Console.WriteLine("recalc");
                order.Clear();
                for (int v = 0; v < videosCount; v++)
                {
                    FillVideoEconomies(score, v, videoPlacements, latencies, requests, videoSizes, order);
                }
                order.Sort((p1, p2) => -p1.score.CompareTo(p2.score));
            }
            done:
            //while (true)
            //{
            //    var bestEconomy = ArgMax(score);
            //    var candCache = bestEconomy[0];
            //    var candVideo = bestEconomy[1];
            //    if (score[candCache, candVideo] == 0)
            //        break;
            //    if (videoPlacements[candCache, candVideo])
            //    {
            //        score[candCache, candVideo] = 0;
            //        continue;
            //    }
            //    if (videoSizes[candVideo] > cacheFreeCapacities[candCache])
            //    {
            //        score[candCache, candVideo] = 0;
            //        continue;
            //    }
            //    videoPlacements[candCache, candVideo] = true;
            //    cacheFreeCapacities[candCache] -= videoSizes[candVideo];
            //    //for (int v = 0; v < videosCount; v++)
            //    //{
            //    //FillVideoEconomies(score, candVideo, videoPlacements, latencies, requests, videoSizes);
            //    //}
            //}

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

        private static void FillVideoEconomies(double[,] score, int v, bool[,] videoPlacements, int[,] latencies, Request[] requests, int[] videoSizes, List<Point> order)
        {
            for (int c = 0; c < cachesCount; c++)
            {
                int placementEconomy = 0;
                Request[] relevantRequests = cacheVideoRequests[c, v];
                if (relevantRequests == null) continue;

                for (int r = 0; r < relevantRequests.Length; r++)
                {
                    var request = relevantRequests[r];
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
                var sss = (double)placementEconomy / videoSizes[v];
                //score[c, v] = sss;
                order.Add(new Point
                {
                    c = c,
                    v = v,
                    score = sss,
                });
            }
        }
    }
}
