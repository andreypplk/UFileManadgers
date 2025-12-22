using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ufm
{
    internal class NativeHelper
    {
        // Константы для кодов ошибок
        public const int ERROR_SUCCESS = 0; // Успешное выполнение
        public const int ERROR_INSUFFICIENT_BUFFER = 122; // Недостаточный буфер
        public const int APPMODEL_ERROR_NO_PACKAGE = 15700; // Приложение не упаковано

        // Импорт функции GetCurrentPackageId из библиотеки api-ms-win-appmodel-runtime-l1-1-1
        [DllImport("api-ms-win-appmodel-runtime-l1-1-1", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern uint GetCurrentPackageId(ref int pBufferLength, out byte pBuffer);

        // Свойство для проверки, упаковано ли приложение
        public static bool IsAppPackaged
        {
            get
            {
                int bufferSize = 0; // Инициализация размера буфера
                byte byteBuffer = 0; // Инициализация буфера
                uint lastError = NativeHelper.GetCurrentPackageId(ref bufferSize, out byteBuffer); // Вызов функции для получения текущего идентификатора пакета
                bool isPackaged = true; // Переменная для хранения результата

                // Проверка, если ошибка указывает на отсутствие пакета
                if (lastError == NativeHelper.APPMODEL_ERROR_NO_PACKAGE)
                {
                    isPackaged = false; // Приложение не упаковано
                }
                return isPackaged; // Возвращение результата
            }
        }

    }
}
