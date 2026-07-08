// SizeHelper.cs
using System;

namespace ufm
{
    public static class SizeHelper
    {
        /// <summary>
        /// Извлекает размерную часть из полного ключа размера (например "Icons Medium" → "Medium").
        /// Корректно обрабатывает составные размеры: "Below Medium", "Above Medium", "Extra Small", "Extra Large".
        /// </summary>
        public static string ExtractSizePartFromFullKey(string fullSizeKey)
        {
            if (string.IsNullOrEmpty(fullSizeKey)) return "Medium";
            var parts = fullSizeKey.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // Проверяем составные трёхсловные размеры
            if (parts.Length >= 3)
            {
                string potential = $"{parts[parts.Length - 3]} {parts[parts.Length - 2]} {parts[parts.Length - 1]}";
                if (potential == "Below Medium" || potential == "Above Medium") return potential;
            }
            // Проверяем составные двусловные размеры
            if (parts.Length >= 2)
            {
                string potential = $"{parts[parts.Length - 2]} {parts[parts.Length - 1]}";
                if (potential == "Extra Small" || potential == "Extra Large") return potential;
            }
            // Иначе возвращаем последнее слово
            return parts[^1];
        }
    }
}