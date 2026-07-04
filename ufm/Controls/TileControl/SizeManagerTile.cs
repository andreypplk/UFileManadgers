using System.Collections.Generic;
using System.Diagnostics;

namespace ufm
{
    internal class SizeManagerTile
    {
        private static readonly Dictionary<string, (int Width, int Height, int IconWidth, int IconHeight, int FontSize, string FontFamily)> _sizes = new()
        {
            // Основные размеры
            ["Extra Small"] = (240, 30, 24, 24, 12, "Segoe UI"),
            ["Small"] = (280, 45, 32, 32, 14, "Segoe UI"),
            ["Medium"] = (320, 60, 42, 42, 16, "Segoe UI"),
            ["Large"] = (380, 75, 64, 64, 18, "Segoe UI"),
            ["Extra Large"] = (440, 90, 96, 96, 20, "Segoe UI"),

            // Дерево
            ["Tree Tiny"] = (200, 28, 20, 20, 10, "Segoe UI"),
            ["Tree Extra Small"] = (240, 30, 24, 24, 12, "Segoe UI"),
            ["Tree Small"] = (280, 45, 32, 32, 14, "Segoe UI"),
            ["Tree Below Medium"] = (300, 52, 36, 36, 15, "Segoe UI"),
            ["Tree Medium"] = (320, 60, 42, 42, 16, "Segoe UI"),
            ["Tree Above Medium"] = (350, 68, 53, 53, 17, "Segoe UI"),
            ["Tree Large"] = (380, 75, 64, 64, 18, "Segoe UI"),
            ["Tree Extra Large"] = (440, 90, 96, 96, 20, "Segoe UI"),
            ["Tree Huge"] = (480, 110, 110, 110, 22, "Segoe UI"),

            // Иконки
            ["Icons Tiny"] = (100, 100, 70, 70, 10, "Segoe UI"),
            ["Icons Extra Small"] = (120, 120, 90, 90, 10, "Segoe UI"),
            ["Icons Small"] = (140, 140, 110, 110, 10, "Segoe UI"),
            ["Icons Below Medium"] = (160, 160, 130, 130, 12, "Segoe UI"),
            ["Icons Medium"] = (180, 180, 150, 150, 14, "Segoe UI"),
            ["Icons Above Medium"] = (200, 200, 170, 170, 16, "Segoe UI"),
            ["Icons Large"] = (220, 220, 190, 190, 18, "Segoe UI"),
            ["Icons Extra Large"] = (240, 240, 210, 210, 20, "Segoe UI"),
            ["Icons Huge"] = (260, 260, 230, 230, 22, "Segoe UI"),

            // Списки
            //["List Tiny"] = (130, 130, 80, 80, 12, "Segoe UI"),
            //["List Extra Small"] = (240, 30, 24, 24, 12, "Segoe UI"),
            //["List Small"] = (280, 45, 32, 32, 14, "Segoe UI"),
            //["List Below Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
            //["List Medium"] = (320, 60, 42, 42, 16, "Segoe UI"),
            //["List Above Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
            //["List Large"] = (380, 75, 64, 64, 18, "Segoe UI"),
            //["List Extra Large"] = (440, 90, 96, 96, 20, "Segoe UI"),
            //["List Huge"] = (220, 220, 196, 196, 20, "Segoe UI"),

            // Списки (двухстрочный вариант)
            ["List Tiny"] = (280, 48, 24, 24, 11, "Segoe UI"),          // Две строки текста
            ["List Extra Small"] = (300, 52, 26, 26, 12, "Segoe UI"),   // Две строки текста
            ["List Small"] = (320, 56, 28, 28, 13, "Segoe UI"),         // Две строки текста
            ["List Below Medium"] = (340, 60, 30, 30, 14, "Segoe UI"),  // Две строки текста
            ["List Medium"] = (360, 64, 32, 32, 15, "Segoe UI"),        // Две строки текста
            ["List Above Medium"] = (380, 68, 34, 34, 16, "Segoe UI"),  // Две строки текста
            ["List Large"] = (400, 72, 36, 36, 17, "Segoe UI"),         // Две строки текста
            ["List Extra Large"] = (420, 76, 38, 38, 18, "Segoe UI"),   // Две строки текста
            ["List Huge"] = (440, 80, 40, 40, 19, "Segoe UI"),          // Две строки текста

            // Компактные списки
            ["CompList Extra Small"] = (130, 130, 80, 80, 12, "Segoe UI"),
            ["CompList Small"] = (145, 145, 100, 100, 14, "Segoe UI"),
            ["CompList Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
            ["CompList Large"] = (200, 200, 164, 164, 18, "Segoe UI"),
            ["CompList Extra Large"] = (220, 220, 196, 196, 20, "Segoe UI"),

            // Плитки
            ["Tile Extra Small"] = (130, 130, 80, 80, 12, "Segoe UI"),
            ["Tile Small"] = (145, 145, 100, 100, 14, "Segoe UI"),
            ["Tile Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
            ["Tile Large"] = (200, 200, 164, 164, 18, "Segoe UI"),
            ["Tile Extra Large"] = (220, 220, 196, 196, 20, "Segoe UI"),

            // Таблицы
            ["Table Extra Small"] = (130, 130, 80, 80, 12, "Segoe UI"),
            ["Table Small"] = (145, 145, 100, 100, 14, "Segoe UI"),
            ["Table Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
            ["Table Large"] = (200, 200, 164, 164, 18, "Segoe UI"),
            ["Table Extra Large"] = (220, 220, 196, 196, 20, "Segoe UI")
        };

        //public static (int Width, int Height, int IconWidth, int IconHeight, int FontSize, string FontFamily) GetSize(string size)
        //{
        //    if (_sizes.TryGetValue(size, out var result))
        //    {
        //        return result;
        //    }

        //    Debug.WriteLine($"КАКОГО ДЕФОЛТ: {size}");
        //    return _sizes["Medium"]; // Возвращаем Medium как размер по умолчанию
        //}
        public static (int Width, int Height, int IconWidth, int IconHeight, int FontSize, string FontFamily) GetSize(string size)
        {
            if (string.IsNullOrEmpty(size))
            {
                Debug.WriteLine($"[SizeManager] Null/empty size → fallback to 'Medium'");
                return _sizes["Medium"];
            }

            if (_sizes.TryGetValue(size, out var result))
                return result;

            Debug.WriteLine($"[SizeManager] Key '{size}' not found → fallback to 'Medium'");
            return _sizes["Medium"];
        }
    }
}


//using System.Collections.Generic;
//using System.Diagnostics;

//namespace ufm
//{
//    internal class SizeManagerTile
//    {
//        private static readonly Dictionary<string, (int Width, int Height, int IconWidth, int IconHeight, int FontSize, string FontFamily)> _sizes = new()
//        {
//            // Дерево
//            ["Tree Tiny"] = (200, 28, 20, 20, 10, "Segoe UI"),
//            ["Tree Extra Small"] = (240, 30, 24, 24, 12, "Segoe UI"),
//            ["Tree Small"] = (280, 45, 32, 32, 14, "Segoe UI"),
//            ["Tree Below Medium"] = (300, 52, 36, 36, 15, "Segoe UI"),
//            ["Tree Medium"] = (320, 60, 42, 42, 16, "Segoe UI"),
//            ["Tree Above Medium"] = (350, 68, 53, 53, 17, "Segoe UI"),
//            ["Tree Large"] = (380, 75, 64, 64, 18, "Segoe UI"),
//            ["Tree Extra Large"] = (440, 90, 96, 96, 20, "Segoe UI"),
//            ["Tree Huge"] = (480, 110, 110, 110, 22, "Segoe UI"),

//            // Иконки
//            ["Icons Tiny"] = (100, 100, 70, 70, 10, "Segoe UI"),
//            ["Icons Extra Small"] = (120, 120, 90, 90, 10, "Segoe UI"),
//            ["Icons Small"] = (140, 140, 110, 110, 10, "Segoe UI"),
//            ["Icons Below Medium"] = (160, 160, 130, 130, 12, "Segoe UI"),
//            ["Icons Medium"] = (180, 180, 150, 150, 14, "Segoe UI"),
//            ["Icons Above Medium"] = (200, 200, 170, 170, 16, "Segoe UI"),
//            ["Icons Large"] = (220, 220, 190, 190, 18, "Segoe UI"),
//            ["Icons Extra Large"] = (240, 240, 210, 210, 20, "Segoe UI"),
//            ["Icons Huge"] = (260, 260, 230, 230, 22, "Segoe UI"),

//            // Списки
//            ["List Tiny"] = (280, 48, 24, 24, 11, "Segoe UI"),
//            ["List Extra Small"] = (300, 52, 26, 26, 12, "Segoe UI"),
//            ["List Small"] = (320, 56, 28, 28, 13, "Segoe UI"),
//            ["List Below Medium"] = (340, 60, 30, 30, 14, "Segoe UI"),
//            ["List Medium"] = (360, 64, 32, 32, 15, "Segoe UI"),
//            ["List Above Medium"] = (380, 68, 34, 34, 16, "Segoe UI"),
//            ["List Large"] = (400, 72, 36, 36, 17, "Segoe UI"),
//            ["List Extra Large"] = (420, 76, 38, 38, 18, "Segoe UI"),
//            ["List Huge"] = (440, 80, 40, 40, 19, "Segoe UI"),

//            // Компактные списки
//            ["CompList Extra Small"] = (130, 130, 80, 80, 12, "Segoe UI"),
//            ["CompList Small"] = (145, 145, 100, 100, 14, "Segoe UI"),
//            ["CompList Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
//            ["CompList Large"] = (200, 200, 164, 164, 18, "Segoe UI"),
//            ["CompList Extra Large"] = (220, 220, 196, 196, 20, "Segoe UI"),

//            // Плитки
//            ["Tile Extra Small"] = (130, 130, 80, 80, 12, "Segoe UI"),
//            ["Tile Small"] = (145, 145, 100, 100, 14, "Segoe UI"),
//            ["Tile Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
//            ["Tile Large"] = (200, 200, 164, 164, 18, "Segoe UI"),
//            ["Tile Extra Large"] = (220, 220, 196, 196, 20, "Segoe UI"),

//            // Таблицы
//            ["Table Extra Small"] = (130, 130, 80, 80, 12, "Segoe UI"),
//            ["Table Small"] = (145, 145, 100, 100, 14, "Segoe UI"),
//            ["Table Medium"] = (180, 160, 100, 100, 12, "Segoe UI"),
//            ["Table Large"] = (200, 200, 164, 164, 18, "Segoe UI"),
//            ["Table Extra Large"] = (220, 220, 196, 196, 20, "Segoe UI")
//        };

//        public static (int Width, int Height, int IconWidth, int IconHeight, int FontSize, string FontFamily) GetSize(string size)
//        {
//            if (_sizes.TryGetValue(size, out var result))
//            {
//                return result;
//            }

//            Debug.WriteLine($"КАКОГО ДЕФОЛТ: {size}");
//            return _sizes["Tree Medium"]; // теперь дефолт с префиксом
//        }
//    }
//}