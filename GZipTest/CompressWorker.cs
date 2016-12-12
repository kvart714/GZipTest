using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZipTest
{
    public class CompressWorker : IWorker
    {
        MemoryStream ms = new MemoryStream();
        Thread thread;
        Exception exception;
        public CompressWorker(Stream input, int size)
        {
            var buffer = new byte[size];
            processed_size = input.Read(buffer, 0, size);

            thread = new Thread(() =>
            {
                try
                {
                    using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                    {
                        gzip.Write(buffer, 0, processed_size);              //--- write data
                    }
                    ms.Position = 4;                                        //--- write block size
                    ms.Write(BitConverter.GetBytes((int)ms.Length), 0, 4);
                    ms.Position = 0;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            });
            thread.Start();
        }
        
        int processed_size;
        public int WaitAndWrite(Stream output)
        {
            thread.Join();
            if (exception != null) throw exception;
            ms.CopyTo(output);
            return processed_size;
        }
    }
}