using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using xxHashSharp;

namespace PatchGen
{
    [Serializable]
    public class FileStateInfo
    {
        public int chunksize = 500000;
        public string path;
        public string shortpath;
        public long len;
        public int chunkcount;
        public Dictionary<int, Chunk> chunks = new Dictionary<int, Chunk>();
        [NonSerialized]
        Stack<Chunk> tmpchunks = new Stack<Chunk>();
        [NonSerialized]
        bool reading = false;
     
        public delegate void FileScanProgress(double value);
        public event FileScanProgress onFileScanProgress;
        public void ScanFile()
        {
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read))
            using (BufferedStream bs = new BufferedStream(fs, chunksize))
            {
                len = fs.Length;
                if (len < chunksize)
                {
                    chunkcount = 1;
                }
                else
                {
                    double chkcnt = len / chunksize;
                    chkcnt = Math.Ceiling(chkcnt);
                    chunkcount = (int)chkcnt + 1;
                }
                byte[] buffer = new byte[chunksize];
                int bytesRead;
                int num = 0;
                reading = true;
                Thread t = new Thread(GetHashesAsync);
                t.Start();
                while ((bytesRead = bs.Read(buffer, 0, chunksize)) != 0) //reading only 50mb chunks at a time
                {
                    var stream = new BinaryReader(new MemoryStream(buffer));
                    Chunk chk = new Chunk();
                    chk.startposition = num * chunksize;
                    chk.len = bytesRead;
                    chk.data = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, chk.data, 0, bytesRead);
                    chk.num = num;
                    tmpchunks.Push(chk);
                    num++;
                    RepProgress(num, chunkcount);
                    GC.Collect();
                }
                reading = false;
            }
            do
            {
                Thread.Sleep(100);
            } while (tmpchunks.Count > 0 | GettingHashes);
        }

        public void RepProgress(int num, int chunkcount)
        {
            if (len > chunksize * 150)
            {
                double d = (double)num / (double)chunkcount;
                d = d * 100;
                d = Math.Ceiling(d);
                int dd = (int)d;
                if (d % 5 == 0)
                {
                    if (onFileScanProgress != null)
                    {
                        onFileScanProgress((int)d);
                    }
                }
            }
        }

        public FileStateInfo SampleFileStateInfo = null;
        public void CompareFile(FileStateInfo sample)
        {
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read))
            using (BufferedStream bs = new BufferedStream(fs, chunksize))
            {
                len = fs.Length;
                SampleFileStateInfo = sample;
                if (len < chunksize)
                {
                    chunkcount = 1;
                }
                else
                {
                    double chkcnt = len / chunksize;
                    chkcnt = Math.Ceiling(chkcnt);
                    chunkcount = (int)chkcnt + 1;
                }
                byte[] buffer = new byte[chunksize];
                int bytesRead;
                int num = 0;
                reading = true;
                Thread t = new Thread(GetHashesAsync);
                t.Start();
                while ((bytesRead = bs.Read(buffer, 0, chunksize)) != 0) //reading only 50mb chunks at a time
                {
                    var stream = new BinaryReader(new MemoryStream(buffer));
                    Chunk chk = new Chunk();
                    chk.startposition = num * chunksize;
                    chk.len = bytesRead;
                    chk.data = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, chk.data, 0, bytesRead);
                    chk.num = num;
                    if (chk.data == null)
                    {
                        //MessageBox.Show("ERROR DATA IS NULL");
                    }
                    tmpchunks.Push(chk);
                    num++;
                    RepProgress(num, chunkcount);
                    GC.Collect();
                }
                reading = false;
                do
                {
                    Thread.Sleep(100);
                } while (tmpchunks.Count > 0 | GettingHashes);
            }
        }

        public void GetHashesAsync()
        {
            do
            {
                GetHashes();
                Thread.Sleep(100);
            } while (reading | tmpchunks.Count > 0);
            GetHashes();
        }
        bool GettingHashes = false;
        public void GetHashes()
        {
            do
            {
                if (tmpchunks.Count > 0)
                {
                    GettingHashes = true;
                    List<Chunk> tmp = new List<Chunk>();
                    lock (tmpchunks)
                    {
                        for (int i = 0; i < tmpchunks.Count; i++)
                        {
                            var ch = tmpchunks.Pop();
                            if (ch != null)
                            {
                                tmp.Add(ch);
                            }

                        }
                    }
                    System.Threading.Tasks.Parallel.ForEach(tmp, chunk =>
                    {
                        chunk.hash = GetHash(chunk.data);
                        if (SampleFileStateInfo == null)
                        {
                            chunk.data = new byte[1];
                            chunk.data = null;
                            GC.Collect();
                            lock (chunks)
                            {
                                chunks.Add(chunk.num, chunk);
                            }
                        }
                        else
                        {
                            string Hash1 = chunk.hash;
                            if (SampleFileStateInfo.chunks.ContainsKey(chunk.num))
                            {
                                string Hash2 = SampleFileStateInfo.chunks[chunk.num].hash;
                                if (chunk.hash != SampleFileStateInfo.chunks[chunk.num].hash)
                                {
                                    lock (chunks)
                                    {
                                        chunks.Add(chunk.num, chunk);
                                    }
                                }
                            }
                            else
                            {
                                lock (chunks)
                                {
                                    chunks.Add(chunk.num, chunk);
                                }
                            }

                        }
                    });
                    GettingHashes = false;
                }
            } while (reading);
        }

        public string GetHash(byte[] chunkdata)
        {
            HashTableHashing.SuperFastHashSimple sfh = new HashTableHashing.SuperFastHashSimple();
            HashTableHashing.MurmurHash2Simple mm2h = new HashTableHashing.MurmurHash2Simple();
            xxHash xxhash = new xxHash();
            xxhash.Init();
            xxhash.Update(chunkdata, chunkdata.Count());
            string hash = "";
            uint h1 = sfh.Hash(chunkdata);
            uint h2 = mm2h.Hash(chunkdata);
            uint h3 = xxhash.Digest();
            BigInteger h = (BigInteger)h1 * (BigInteger)h2 * (BigInteger)h3;
            hash = h.ToString() + h1.ToString()+h2.ToString()+h3.ToString();
            // byte array representation of that string
            byte[] encodedhash = new UTF8Encoding().GetBytes(hash);
            // need MD5 to calculate the hash
            byte[] md5hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedhash);
            // string representation (similar to UNIX format)
            string encoded = BitConverter.ToString(md5hash)
                // without dashes
               .Replace("-", string.Empty)
                // make lowercase
               .ToLower();
            return encoded;
        }
    }

    [Serializable]
    public class Chunk
    {
        public int num;
        public long startposition;
        public long len;
        public byte[] data;
        public string hash;
    }
}
