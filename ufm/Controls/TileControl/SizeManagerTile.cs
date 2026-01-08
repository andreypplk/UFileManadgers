//using System.Diagnostics;

//namespace ufm
//{
//    internal class SizeManagerTile
//    {
//        public static (int Width, int Height, int IconWidth, int IconHeight, int FontSize, string FontFamily) GetSize(string size)
//        {
//            switch (size)
//            {
//                case "Extra Small":
//                    return (240, 30, 24, 24, 12, "Segoe UI");
//                case "Small":
//                    return (280, 45, 32, 32, 14, "Segoe UI");
//                case "Medium":
//                    return (320, 60, 42, 42, 16, "Segoe UI");
//                case "Large":
//                    return (380, 75, 64, 64, 18, "Segoe UI");
//                case "Extra Large":
//                    return (440, 90, 96, 96, 20, "Segoe UI");
//                case "Icons Tiny":
//                    return (100, 100, 70, 70, 9, "Segoe UI");
//                case "Icons Extra Small":
//                    return (120, 120, 90, 90, 9, "Segoe UI");
//                case "Icons Small":
//                    return (140, 140, 110, 110, 10, "Segoe UI");
//                case "Icons Below Medium":
//                    return (160, 160, 130, 130, 12, "Segoe UI");
//                case "Icons Medium":
//                    return (180, 180, 150, 150, 14, "Segoe UI");
//                case "Icons Above Medium":
//                    return (200, 200, 170, 170, 16, "Segoe UI");
//                case "Icons Large":
//                    return (220, 220, 190, 190, 18, "Segoe UI");
//                case "Icons Extra Large":
//                    return (240, 240, 210, 210, 20, "Segoe UI");
//                case "Icons Huge":
//                    return (260, 260, 230, 230, 22, "Segoe UI");
//                case "List Extra Small":
//                    return (130, 130, 80, 80, 12, "Segoe UI");
//                case "List Small":
//                    return (145, 145, 100, 100, 14, "Segoe UI");
//                case "List Medium":
//                    return (180, 160, 100, 100, 12, "Segoe UI");
//                case "List Large":
//                    return (200, 200, 164, 164, 18, "Segoe UI");
//                case "List Extra Large":
//                    return (220, 220, 196, 196, 20, "Segoe UI");
//                case "CompList Extra Small":
//                    return (130, 130, 80, 80, 12, "Segoe UI");
//                case "CompList Small":
//                    return (145, 145, 100, 100, 14, "Segoe UI");
//                case "CompList Medium":
//                    return (180, 160, 100, 100, 12, "Segoe UI");
//                case "CompList Large":
//                    return (200, 200, 164, 164, 18, "Segoe UI");
//                case "CompList Extra Large":
//                    return (220, 220, 196, 196, 20, "Segoe UI");
//                case "Tile Extra Small":
//                    return (130, 130, 80, 80, 12, "Segoe UI");
//                case "Tile Small":
//                    return (145, 145, 100, 100, 14, "Segoe UI");
//                case "Tile Medium":
//                    return (180, 160, 100, 100, 12, "Segoe UI");
//                case "Tile Large":
//                    return (200, 200, 164, 164, 18, "Segoe UI");
//                case "Tile Extra Large":
//                    return (220, 220, 196, 196, 20, "Segoe UI");
//                case "Table Extra Small":
//                    return (130, 130, 80, 80, 12, "Segoe UI");
//                case "Table Small":
//                    return (145, 145, 100, 100, 14, "Segoe UI");
//                case "Table Medium":
//                    return (180, 160, 100, 100, 12, "Segoe UI");
//                case "Table Large":
//                    return (200, 200, 164, 164, 18, "Segoe UI");
//                case "Table Extra Large":
//                    return (220, 220, 196, 196, 20, "Segoe UI");
//                default:
//                    Debug.WriteLine("КАКОГО ДЕФОЛТ");
//                    return (320, 60, 42, 42, 16, "Segoe UI");
//            }
//        }
//    }
//}




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

        public static (int Width, int Height, int IconWidth, int IconHeight, int FontSize, string FontFamily) GetSize(string size)
        {
            if (_sizes.TryGetValue(size, out var result))
            {
                return result;
            }

            Debug.WriteLine($"КАКОГО ДЕФОЛТ: {size}");
            return _sizes["Medium"]; // Возвращаем Medium как размер по умолчанию
        }
    }
}