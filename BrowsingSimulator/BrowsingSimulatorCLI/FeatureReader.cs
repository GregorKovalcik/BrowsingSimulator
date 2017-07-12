using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DescriptorClusteringCLI
{
    public static class Distance
    {
        public static double L2(float[] a, float[] b)
        {
            double accumulator = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double difference = Math.Abs(a[i] - b[i]);
                accumulator += difference * difference;
            }

            return Math.Sqrt(accumulator);
        }
    }

    public struct Features
    {
        public float[] features;
        public long id;
        public double distance;

        public Features(float[] features, long id, double distance)
        {
            this.features = features;
            this.id = id;
            this.distance = distance;
        }
    }


    public class FeatureReader : IDisposable
    {
        private BinaryReader reader;
        private long featureDataStartOffset;
        private bool loadMetadata;

        public string NetworkName { get; private set; }
        public string NetworkLayer { get; private set; }

        public int VideoCount { get; private set; }
        public int ShotCount { get; private set; }
        public int FeatureCount { get; private set; }
        public int FeatureDimension { get; private set; }

        public int[] VideoOffsets { get; private set; }
        public int[] ShotOffsets { get; private set; }
        public int[] VideoShotOffsets { get; private set; }

        public int[] FeatureToVideoId { get; private set; }
        public int[] FeatureToShotId { get; private set; }
        public int[] FeatureToFrameId { get; private set; }


        public FeatureReader(string filename, bool loadMetadata = true)
        {
            reader = new BinaryReader(File.OpenRead(filename));
            this.loadMetadata = loadMetadata;
            ReadHeader();
        }

        public void Dispose()
        {
            reader.Dispose();
        }


        private void ReadHeader()
        {
            if (loadMetadata)
            {
                char[] magicHeader = reader.ReadChars(16);
                NetworkName = new string(reader.ReadChars(16)).Trim();
                NetworkLayer = new string(reader.ReadChars(16)).Trim();

                VideoCount = reader.ReadInt32();
                ShotCount = reader.ReadInt32();
            }
            FeatureCount = reader.ReadInt32();
            FeatureDimension = reader.ReadInt32();
            if (loadMetadata)
            {
                VideoOffsets = new int[VideoCount];
                ShotOffsets = new int[ShotCount];
                VideoShotOffsets = new int[VideoCount];

                FeatureToVideoId = new int[FeatureCount];
                FeatureToShotId = new int[FeatureCount];
                FeatureToFrameId = new int[FeatureCount];

                for (int i = 0; i < VideoCount; i++)
                {
                    VideoOffsets[i] = reader.ReadInt32();
                }
                for (int i = 0; i < ShotCount; i++)
                {
                    ShotOffsets[i] = reader.ReadInt32();
                }
                for (int i = 0; i < VideoCount; i++)
                {
                    VideoShotOffsets[i] = reader.ReadInt32();
                }

                for (int i = 0; i < FeatureCount; i++)
                {
                    FeatureToVideoId[i] = reader.ReadInt32();
                }
                for (int i = 0; i < FeatureCount; i++)
                {
                    FeatureToShotId[i] = reader.ReadInt32();
                }
                for (int i = 0; i < FeatureCount; i++)
                {
                    FeatureToFrameId[i] = reader.ReadInt32();
                }
            }
            int metadataSize =
                3 * 16                      // text header
                + 4 * sizeof(int)           // videoCount, shotCount, featureCount, featureDimension
                + VideoCount * sizeof(int)  // video offsets
                + ShotCount * sizeof(int)   // shot offsets
                + VideoCount * sizeof(int)  // video-shot mapping
                + 3 * FeatureCount * sizeof(int);

            const int blockSize = 4096;     // aligned to block size

            if (loadMetadata)
            {
                featureDataStartOffset = ((metadataSize / blockSize) + 1) * blockSize;
            }
            else
            {
                featureDataStartOffset = 2 * sizeof(int);
            }
        }

        public float[] GetFeatures(int featuresId)
        {
            reader.BaseStream.Seek(featureDataStartOffset + (long)featuresId * FeatureDimension * sizeof(float), SeekOrigin.Begin);
            byte[] bytes = reader.ReadBytes(FeatureDimension * sizeof(float));
            float[] floats = new float[FeatureDimension];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

            return floats;
        }

        public float[][] GetVideoFeatures(int videoId)
        {
            long offset = featureDataStartOffset + (long)VideoOffsets[videoId] * FeatureDimension * sizeof(float);
            int count = GetFeatureCount(videoId);
            float[][] result = new float[count][];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                byte[] bytes = reader.ReadBytes(FeatureDimension * sizeof(float));
                result[i] = new float[FeatureDimension];
                Buffer.BlockCopy(bytes, 0, result[i], 0, bytes.Length);
            }
            return result;
        }

        public float[][] GetShotFeatures(int videoId, int shotId)
        {
            int shotOffset = VideoShotOffsets[videoId] + shotId;
            int featureOffset = ShotOffsets[shotOffset];
            long offset = featureDataStartOffset + (long)featureOffset * FeatureDimension * sizeof(float);
            int count = GetFeatureCount(videoId, shotId);
            float[][] result = new float[count][];

            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < count; i++)
            {
                byte[] bytes = reader.ReadBytes(FeatureDimension * sizeof(float));
                result[i] = new float[FeatureDimension];
                Buffer.BlockCopy(bytes, 0, result[i], 0, bytes.Length);
            }
            return result;
        }


        public int GetVideoId(int featureId)
        {
            return FeatureToVideoId[featureId];
        }

        public int GetShotId(int featureId)
        {
            return FeatureToShotId[featureId];
        }

        public int GetFrameId(int featureId)
        {
            return FeatureToFrameId[featureId];
        }

        public int GetFeatureId(int videoId, int shotId, int videoFrameId)
        {
            int shotOffset = VideoShotOffsets[videoId] + shotId;
            int featureOffset = ShotOffsets[shotOffset] + videoFrameId;
            return featureOffset;
        }

        public int GetFeatureCount(int videoId)
        {
            if (videoId == VideoOffsets.Length - 1)
            {
                return FeatureCount - VideoOffsets[videoId];
            }
            else
            {
                return VideoOffsets[videoId + 1] - VideoOffsets[videoId];
            }
        }

        public int GetFeatureCount(int videoId, int shotId)
        {
            int shotOffset = VideoShotOffsets[videoId] + shotId;
            int featureOffset = ShotOffsets[shotOffset];

            // no features for this shot
            if (featureOffset == -1)
            {
                return 0;
            }

            // find the next shot offset
            int nextShotOffset = shotOffset + 1;
            while (ShotOffsets[nextShotOffset] == -1 && (nextShotOffset != ShotOffsets.Length))
            {
                nextShotOffset++;
            }

            if (shotOffset == ShotOffsets.Length - 1 || nextShotOffset == ShotOffsets.Length)
            {
                return FeatureCount - featureOffset;
            }
            else
            {
                return ShotOffsets[nextShotOffset] - featureOffset;
            }
        }

        public int GetShotCount(int videoId)
        {
            int featureOffset = VideoShotOffsets[videoId];
            if (videoId == VideoShotOffsets.Length - 1)
            {
                return ShotCount - featureOffset;
            }
            else
            {
                return VideoShotOffsets[videoId + 1] - featureOffset;
            }
        }


        public LinkedList<Features> FindSimilar(int featureID, int kResults)
        {
            float[] queryFeature = GetFeatures(featureID);
            LinkedList<Features> results = new LinkedList<Features>();

            reader.BaseStream.Seek(featureDataStartOffset, SeekOrigin.Begin);
            for (int i = 0; i < FeatureCount; i++)
            {
                if (i % 10000 == 0) Console.WriteLine("Checking feature ID: " + i);
                byte[] bytes = reader.ReadBytes(FeatureDimension * sizeof(float));
                float[] floats = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

                double distance = Distance.L2(queryFeature, floats);

                if (results.Count == 0)
                {
                    results.AddFirst(new Features(floats, i, distance));
                }
                else if (results.Count < kResults || distance < results.Last.Value.distance)
                {
                    LinkedListNode<Features> node = results.First;
                    while (node != null && node.Value.distance < distance)
                    {
                        node = node.Next;
                    }
                    if (node != null)
                    {
                        results.AddBefore(node, new Features(floats, i, distance));
                    }
                    else
                    {
                        results.AddLast(new Features(floats, i, distance));
                    }

                    if (results.Count > kResults)
                    {
                        results.RemoveLast();
                    }
                }
            }

            return results;
        }

        public LinkedList<Features>[] FindSimilars(int[] featureIDs, int kResults)
        {
            float[][] queryFeatures = new float[featureIDs.Length][];
            for (int i = 0; i < queryFeatures.Length; i++)
            {
                //queryFeatures[i] = new float[featureDimension];
                queryFeatures[i] = GetFeatures(featureIDs[i]);
            }

            LinkedList<Features>[] results = new LinkedList<Features>[featureIDs.Length];
            for (int i = 0; i < featureIDs.Length; i++)
            {
                results[i] = new LinkedList<Features>();
            }

            reader.BaseStream.Seek(featureDataStartOffset, SeekOrigin.Begin);
            for (int i = 0; i < FeatureCount; i++)
            {
                if (i % 10000 == 0)
                {
                    Console.WriteLine("Checking feature ID: " + i);
                }
                byte[] bytes = reader.ReadBytes(FeatureDimension * sizeof(float));
                float[] floats = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

                double[] distances = new double[featureIDs.Length];
                Parallel.For(0, featureIDs.Length, j =>
                {
                    distances[j] = Distance.L2(queryFeatures[j], floats);

                    if (results[j].Count == 0)
                    {
                        results[j].AddFirst(new Features(floats, i, distances[j]));
                    }
                    else if (results[j].Count < kResults || distances[j] < results[j].Last.Value.distance)
                    {
                        LinkedListNode<Features> node = results[j].First;
                        while (node != null && node.Value.distance < distances[j])
                        {
                            node = node.Next;
                        }
                        if (node != null)
                        {
                            results[j].AddBefore(node, new Features(floats, i, distances[j]));
                        }
                        else
                        {
                            results[j].AddLast(new Features(floats, i, distances[j]));
                        }

                        if (results[j].Count > kResults)
                        {
                            results[j].RemoveLast();
                        }
                    }
                });
            }


            return results;
        }

        public LinkedList<Features>[][] FindSimilarsIgnoreNullRadius(int[] featureIDs, int kResults, int nRadiuses, float increment, float bias)
        {
            float[][] queryFeatures = new float[featureIDs.Length][];
            for (int i = 0; i < queryFeatures.Length; i++)
            {
                //queryFeatures[i] = new float[4096];
                queryFeatures[i] = GetFeatures(featureIDs[i]);
            }

            LinkedList<Features>[][] results = new LinkedList<Features>[nRadiuses][];
            for (int i = 0; i < nRadiuses; i++)
            {
                results[i] = new LinkedList<Features>[featureIDs.Length];
                for (int j = 0; j < featureIDs.Length; j++)
                {
                    results[i][j] = new LinkedList<Features>();
                }
            }


            reader.BaseStream.Seek(featureDataStartOffset, SeekOrigin.Begin);
            for (int iFeature = 0; iFeature < FeatureCount; iFeature++)
            {
                if (iFeature % 1000 == 0)
                {
                    Console.WriteLine("Checking feature ID: " + iFeature);
                }
                byte[] bytes = reader.ReadBytes(FeatureDimension * sizeof(float));
                float[] floats = new float[FeatureDimension];
                Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);

                double nullDistance = Distance.L2(new float[FeatureDimension], floats);

                Parallel.For(0, featureIDs.Length, index =>
                {
                    for (int iRadius = 0; iRadius < nRadiuses; iRadius++)
                    {
                        if (nullDistance < (iRadius * increment) + bias)
                        {
                            continue;
                        }

                        double distance = Distance.L2(queryFeatures[index], floats);

                        if (results[iRadius][index].Count == 0)
                        {
                            results[iRadius][index].AddFirst(new Features(floats, iFeature, distance));
                        }
                        else if (results[iRadius][index].Count < kResults || distance < results[iRadius][index].Last.Value.distance)
                        {
                            LinkedListNode<Features> node = results[iRadius][index].First;
                            while (node != null && node.Value.distance < distance)
                            {
                                node = node.Next;
                            }
                            if (node != null)
                            {
                                results[iRadius][index].AddBefore(node, new Features(floats, iFeature, distance));
                            }
                            else
                            {
                                results[iRadius][index].AddLast(new Features(floats, iFeature, distance));
                            }

                            if (results[iRadius][index].Count > kResults)
                            {
                                results[iRadius][index].RemoveLast();
                            }
                        }
                    }
                });
            }


            return results;
        }

    }
}
