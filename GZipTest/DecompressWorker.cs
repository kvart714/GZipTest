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
    public class DecompressWorker : IWorker
    {
        byte[] out_buf;
        int size;
        Thread thread;
        Exception exception;
        public DecompressWorker(Stream input)
        {
            var buffer = new byte[8];                           //--- read block size
            input.Read(buffer, 0, 8);
            input.Seek(-8, SeekOrigin.Current);
            int block_size = BitConverter.ToInt32(buffer, 4);

            buffer = new byte[block_size];                      //--- read data
            processed_size = input.Read(buffer, 0, block_size);

            int buf_size = BitConverter.ToInt32(buffer, buffer.Length - 4);
            out_buf = new byte[buf_size];
            thread = new Thread(() =>
            {
                try
                {
                    using (var ms = new MemoryStream(buffer))
                    using (var gzip = new GZipStream(ms, CompressionMode.Decompress, true))
                    {
                        size = gzip.Read(out_buf, 0, buf_size);
                    }
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
            output.Write(out_buf, 0, size);
            return processed_size;
        }
    }
}