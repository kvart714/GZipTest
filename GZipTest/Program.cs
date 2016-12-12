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
    class Program
    {
        static FileStream INPUT_FILE;
        static FileStream OUTPUT_FILE;
        static CompressionMode MODE;
        static int BLOCK_SIZE;

        static void Main(string[] args)
        {
            DateTime start_time = DateTime.Now;

            #region валидация аргументов

            if (args.Length != 3) Error("Неверное количество аргументов");

            if (args[0] == "compress") MODE = CompressionMode.Compress;
            else
            if (args[0] == "decompress") MODE = CompressionMode.Decompress;
            else
                Error("Неверный аргумент командной строки");

            try
            {
                if (new FileInfo(args[1]).Length == 0) Error("Файл пуст");
            }
            catch (Exception e)
            {
                Error(e.Message);
            }

            #endregion
            
            if (MODE == CompressionMode.Compress) //--- рассчет оптимального размера блока. При распаковке блоки имеют динамический размер.
            {
                long fsize = new FileInfo(args[1]).Length;
                long optimal_size = Math.Min(fsize, 1024 * 1024 * 512) / Environment.ProcessorCount;
#if DEBUG
                optimal_size /= 2;      //--- при отладке требуется больше памяти.
#endif
                if (fsize < 1024) optimal_size = 1024;

                BLOCK_SIZE = (int)optimal_size;
            }

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Отмена");
                OUTPUT_FILE.Close();
                File.Delete(args[2]);
            };

            Console.Clear();
            
            try
            {
                INPUT_FILE = new FileStream(args[1], FileMode.Open);
                OUTPUT_FILE = new FileStream(args[2], FileMode.Create);
                Go();
            }
            catch (Exception e)
            {
                Error(e.Message);
            }

            INPUT_FILE.Close();
            OUTPUT_FILE.Close();
            
            Console.Write("Время: ");
            Console.WriteLine((DateTime.Now - start_time).ToString("mm\\:ss\\.ffffff"));
        }

        static Queue<IWorker> Workers = new Queue<IWorker>();
        
        /// <summary>
        /// Основной алгоритм программы
        /// </summary>
        static void Go()
        {
            long processed_size = 0;

            while (INPUT_FILE.Position < INPUT_FILE.Length)
            {
                if (MODE == CompressionMode.Compress)
                {
                    Workers.Enqueue(new CompressWorker(INPUT_FILE, BLOCK_SIZE));
                }
                else
                {
                    Workers.Enqueue(new DecompressWorker(INPUT_FILE));
                }

                if (Workers.Count >= Environment.ProcessorCount)
                {
                    processed_size += Workers.Dequeue().WaitAndWrite(OUTPUT_FILE);
                }
                UI(INPUT_FILE.Length, INPUT_FILE.Position, processed_size);
            }

            foreach (var worker in Workers)
            {
                processed_size += worker.WaitAndWrite(OUTPUT_FILE);
                
                UI(INPUT_FILE.Length, INPUT_FILE.Position, processed_size);
            }
        }

        /// <summary>
        /// Отображение текущего выполнения
        /// </summary>
        /// <param name="total">Размер исходного файла (байт)</param>
        /// <param name="write">Прочитано (байт)</param>
        /// <param name="processed">Обработано (байт)</param>
        static void UI(long total, long write, long processed)
        {
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            Console.WriteLine($"Прочитано {write} / {total} байт");

            string s;
            for (s = ""; s.Length < Console.WindowWidth * write / total; s += '█') ;
            Console.Write(s);

            Console.CursorTop = 3;
            Console.WriteLine($"\nОбработано: {processed} / {total} байт");
            for (s = ""; s.Length < Console.WindowWidth * processed / total; s += '█') ;
            Console.Write(s);

            Console.WriteLine();
        }

        /// <summary>
        /// Завершение программы с ошибкой
        /// </summary>
        /// <param name="message">Сообщение</param>
        static void Error(string message)
        {
            Console.WriteLine(message);
            Environment.Exit(1);
        }
    }
}