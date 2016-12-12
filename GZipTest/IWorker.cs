using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipTest
{
    /// <summary>
    /// Считывает блок данных из входного потока и обрабатывает их в отдельном треде
    /// </summary>
    public interface IWorker
    {
        /// <summary>
        /// Дожидается завершения обработки и записывает их в поток вывода
        /// </summary>
        /// <param name="output">Поток вывода</param>
        /// <returns>Количество обработанных данных (байт)</returns>
        int WaitAndWrite(Stream output);
    }
}