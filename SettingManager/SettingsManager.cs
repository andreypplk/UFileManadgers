

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Windows.Storage;
using Microsoft.UI.Xaml;

namespace SettingManager
{
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public static SettingsManager Instance => _instance ??= new SettingsManager();

        public void SaveSetting(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            if (value == null)
            {
                localSettings.Values.Remove(key);
                return;
            }

            try
            {
                object serializableValue = ConvertToSerializableValue(value);

                // Проверка на отрицательные значения для числовых типов
                if (serializableValue is IConvertible convertible &&
                    IsNumericType(value.GetType()) &&
                    Convert.ToDouble(convertible) < 0)
                {
                    Debug.WriteLine($"Attempt to save negative value for {key}: {value}");
                    return;
                }

                localSettings.Values[key] = serializableValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving setting '{key}': {ex.Message}");
            }
        }

        private object ConvertToSerializableValue(object value)
        {
            // Обработка enum ДО проверки числовых типов
            if (value.GetType().IsEnum)
            {
                // Получаем базовый тип enum и преобразуем
                Type underlyingType = Enum.GetUnderlyingType(value.GetType());
                return Convert.ChangeType(value, underlyingType);
            }

            return value switch
            {
                // Обработка специальных типов
                Visibility visibility => visibility == Visibility.Visible ? "Visible" : "Collapsed",

                // Числовые типы сохраняем как строки для избежания проблем сериализации
                float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
                double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
                decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),

                // Для остальных типов используем как есть
                _ => value
            };
        }

        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            try
            {
                if (!localSettings.Values.TryGetValue(key, out object value) || value == null)
                    return defaultValue;

                return ConvertFromStoredValue<T>(value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting setting '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        private T ConvertFromStoredValue<T>(object storedValue)
        {
            Type targetType = typeof(T);

            // Обработка специальных типов
            if (targetType == typeof(Visibility))
            {
                return (T)(object)(storedValue.ToString() == "Visible" ?
                    Visibility.Visible : Visibility.Collapsed);
            }

            // Обработка enum с поддержкой числовых значений
            if (targetType.IsEnum)
            {
                try
                {
                    if (storedValue is string stringValue)
                    {
                        // Пытаемся распарсить как строку (например, "Panel0")
                        return (T)Enum.Parse(targetType, stringValue);
                    }
                    else
                    {
                        // Если значение числовое, используем Enum.ToObject
                        Type underlyingType = Enum.GetUnderlyingType(targetType);
                        object numericValue = Convert.ChangeType(storedValue, underlyingType);
                        return (T)Enum.ToObject(targetType, numericValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error converting to enum {targetType.Name}: {ex.Message}");
                    return default(T);
                }
            }

            // Обработка числовых типов, сохраненных как строки
            if (storedValue is string stringValueNumeric)
            {
                if (targetType == typeof(float) &&
                    float.TryParse(stringValueNumeric, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatResult))
                {
                    return (T)(object)floatResult;
                }
                else if (targetType == typeof(double) &&
                         double.TryParse(stringValueNumeric, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleResult))
                {
                    return (T)(object)doubleResult;
                }
                else if (targetType == typeof(decimal) &&
                         decimal.TryParse(stringValueNumeric, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalResult))
                {
                    return (T)(object)decimalResult;
                }
            }

            // Стандартное преобразование
            try
            {
                return (T)Convert.ChangeType(storedValue, targetType);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error converting {storedValue} to {targetType.Name}: {ex.Message}");
                return default(T);
            }
        }

        private bool IsNumericType(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.SByte or TypeCode.Byte or
                TypeCode.Int16 or TypeCode.UInt16 or
                TypeCode.Int32 or TypeCode.UInt32 or
                TypeCode.Int64 or TypeCode.UInt64 or
                TypeCode.Single or TypeCode.Double or
                TypeCode.Decimal => true,
                _ => false
            };
        }

        public void CleanInvalidSettings()
        {
            try
            {
                var keysToRemove = new List<string>();
                foreach (var key in localSettings.Values.Keys)
                {
                    var value = localSettings.Values[key];
                    bool shouldRemove = value switch
                    {
                        long l when l < 0 => true,
                        int i when i < 0 => true,
                        double d when d < 0 => true,
                        string s when (s.Contains("-") &&
                            (float.TryParse(s, out float f) && f < 0 ||
                             double.TryParse(s, out double dbl) && dbl < 0)) => true,
                        _ => false
                    };

                    if (shouldRemove)
                    {
                        keysToRemove.Add(key);
                        Debug.WriteLine($"Flagged invalid setting for removal: {key} = {value}");
                    }
                }

                foreach (var key in keysToRemove)
                {
                    localSettings.Values.Remove(key);
                    Debug.WriteLine($"Removed invalid setting: {key}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning settings: {ex.Message}");
            }
        }

        public void SaveSettingsToJson<T>(string filePath, T settings)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

            if (settings == null)
                throw new ArgumentNullException(nameof(settings), "Объект настроек не может быть null.");

            Debug.WriteLine($"Сохранение настроек в JSON файл: {filePath}");

            try
            {
                string jsonString = JsonSerializer.Serialize(settings);
                CheckAndCreateFile(filePath);
                File.WriteAllText(filePath, jsonString);

                string savedJsonString = File.ReadAllText(filePath);
                if (savedJsonString != jsonString)
                    throw new InvalidOperationException("Ошибка: не удалось подтвердить запись данных в файл.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении настроек в JSON: {ex}");
                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
                throw;
            }
        }

        public T LoadSettingsFromJson<T>(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл настроек не найден.", filePath);

            Debug.WriteLine($"Загрузка настроек из JSON файла: {filePath}");

            try
            {
                string jsonString = File.ReadAllText(filePath);
                Debug.WriteLine($"Содержимое JSON: {jsonString}");
                Debug.WriteLine($"Десериализация JSON в {typeof(T).Name}");
                return JsonSerializer.Deserialize<T>(jsonString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке настроек из JSON: {ex}");
                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
                throw;
            }
        }

        public void TransferPropertyToJson(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

            Debug.WriteLine($"Перенос настроек из локального контейнера в JSON файл: {filePath}");

            try
            {
                var settingsDictionary = new Dictionary<string, object>();
                foreach (var key in localSettings.Values.Keys)
                {
                    settingsDictionary[key] = localSettings.Values[key];
                }

                string jsonString = JsonSerializer.Serialize(settingsDictionary);
                CheckAndCreateFile(filePath);
                File.WriteAllText(filePath, jsonString);

                string savedJsonString = File.ReadAllText(filePath);
                if (settingsDictionary != JsonSerializer.Deserialize<Dictionary<string, object>>(savedJsonString))
                    throw new InvalidOperationException("Ошибка: не удалось подтвердить запись данных в файл.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переносе настроек в JSON: {ex}");
                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
                throw;
            }
        }

        public void TransferJsonToProperty(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл JSON не найден.", filePath);

            Debug.WriteLine($"Перенос настроек из JSON файла в локальный контейнер: {filePath}");

            try
            {
                string jsonString = File.ReadAllText(filePath);
                var settingsDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

                foreach (var kvp in settingsDictionary)
                {
                    localSettings.Values[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переносе настроек в локальный контейнер: {ex}");
                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
                throw;
            }
        }

        private void CheckAndCreateFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"Файл не существует. Создаем файл: {filePath}");

                try
                {
                    File.WriteAllText(filePath, "{}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при создании файла: {ex}");
                    Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
                    throw;
                }
            }
        }
    }
}