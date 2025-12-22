////using System;
////using System.Collections.Generic;
////using System.Diagnostics;
////using System.IO;
////using System.Text.Json;
////using Windows.Storage;

////namespace SettingManager
////{
////    // Основной класс для работы с настройками
////    public class SettingsManager
////    {
////        // Приватный статический экземпляр класса
////        private static SettingsManager _instance;

////        // Локальный контейнер для хранения настроек
////        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

////        // Приватный конструктор для предотвращения создания экземпляров извне
////        public SettingsManager() { }

////        // Статическое свойство для доступа к экземпляру
////        public static SettingsManager Instance
////        {
////            get
////            {
////                if (_instance == null)
////                {
////                    _instance = new SettingsManager();
////                }
////                return _instance;
////            }
////        }

////        public void SaveSetting(string key, object value)
////        {
////            if (string.IsNullOrWhiteSpace(key))
////                throw new ArgumentException("Key cannot be empty", nameof(key));

////            if (value == null)
////                throw new ArgumentNullException(nameof(value));

////            // Валидация числовых значений
////            if (value is int intValue)
////            {
////                if (intValue < 0)
////                {
////                    Debug.WriteLine($"Attempt to save negative value for {key}: {intValue}");
////                    return;
////                }
////                localSettings.Values[key] = (long)intValue; // WinRT требует long
////            }
////            else if (value is double doubleValue && doubleValue < 0)
////            {
////                Debug.WriteLine($"Attempt to save negative value for {key}: {doubleValue}");
////                return;
////            }
////            else
////            {
////                localSettings.Values[key] = value;
////            }
////        }
////        //Существующий метод для получения object
////        public object GetSetting(string key)
////        {
////            if (string.IsNullOrWhiteSpace(key))
////                throw new ArgumentException("Key cannot be empty", nameof(key));

////            return localSettings.Values.TryGetValue(key, out object value) ? value : null;
////        }
////        public T GetSetting<T>(string key, T defaultValue = default)
////        {
////            if (string.IsNullOrWhiteSpace(key))
////                throw new ArgumentException("Key cannot be empty", nameof(key));

////            try
////            {
////                if (!localSettings.Values.ContainsKey(key))
////                    return defaultValue;

////                object value = localSettings.Values[key];

////                // Специальная обработка для числовых типов
////                if (typeof(T) == typeof(int) || typeof(T) == typeof(double))
////                {
////                    // Проверяем, что значение не null и может быть преобразовано
////                    if (value == null)
////                        return defaultValue;

////                    // Дополнительная проверка для отрицательных значений
////                    if (value is int intValue && intValue < 0)
////                        return defaultValue;

////                    if (value is double doubleValue && doubleValue < 0)
////                        return defaultValue;
////                }

////                // Обработка enum
////                if (typeof(T).IsEnum)
////                    return (T)Enum.Parse(typeof(T), value.ToString());

////                // Обработка nullable типов
////                Type underlyingType = Nullable.GetUnderlyingType(typeof(T));
////                if (underlyingType != null)
////                {
////                    return (T)Convert.ChangeType(value, underlyingType);
////                }

////                return (T)Convert.ChangeType(value, typeof(T));
////            }
////            catch (Exception ex)
////            {
////                Debug.WriteLine($"Error getting setting '{key}': {ex.Message}");
////                return defaultValue;
////            }
////        }
////        //public T GetSetting<T>(string key, T defaultValue = default)
////        //{
////        //    if (string.IsNullOrWhiteSpace(key))
////        //        throw new ArgumentException("Key cannot be empty", nameof(key));

////        //    var value = GetSetting(key);

////        //    if (value == null)
////        //        return defaultValue;

////        //    // Обработка enum
////        //    if (typeof(T).IsEnum)
////        //        return (T)Enum.Parse(typeof(T), value.ToString());

////        //    // Обработка nullable типов
////        //    Type underlyingType = Nullable.GetUnderlyingType(typeof(T));
////        //    if (underlyingType != null)
////        //    {
////        //        return (T)Convert.ChangeType(value, underlyingType);
////        //    }

////        //    return (T)Convert.ChangeType(value, typeof(T));
////        //}

////        public void CleanInvalidSettings()
////        {
////            try
////            {
////                var keysToRemove = new List<string>();
////                foreach (var key in localSettings.Values.Keys)
////                {
////                    var value = localSettings.Values[key];
////                    bool shouldRemove = value switch
////                    {
////                        long l when l < 0 => true,
////                        int i when i < 0 => true,
////                        double d when d < 0 => true,
////                        _ => false
////                    };

////                    if (shouldRemove)
////                    {
////                        keysToRemove.Add(key);
////                        Debug.WriteLine($"Flagged invalid setting for removal: {key} = {value}");
////                    }
////                }

////                foreach (var key in keysToRemove)
////                {
////                    localSettings.Values.Remove(key);
////                    Debug.WriteLine($"Removed invalid setting: {key}");
////                }
////            }
////            catch (Exception ex)
////            {
////                Debug.WriteLine($"Error cleaning settings: {ex.Message}");
////            }
////        }

////        // Сохранение объекта настроек в JSON файл
////        public void SaveSettingsToJson<T>(string filePath, T settings)
////        {
////            // Проверяем, что путь к файлу не пустой
////            if (string.IsNullOrWhiteSpace(filePath))
////                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

////            // Проверяем, что объект настроек не равен null
////            if (settings == null)
////                throw new ArgumentNullException(nameof(settings), "Объект настроек не может быть null.");

////            // Выводим сообщение в лог о сохранении настроек
////            Debug.WriteLine($"Сохранение настроек в JSON файл: {filePath}");

////            try
////            {
////                // Сериализуем объект настроек в JSON строку
////                string jsonString = JsonSerializer.Serialize(settings);

////                // Проверяем и создаем файл, если его нет
////                CheckAndCreateFile(filePath);

////                // Записываем JSON строку в файл
////                File.WriteAllText(filePath, jsonString);

////                // Читаем содержимое файла для проверки корректности записи
////                string savedJsonString = File.ReadAllText(filePath);
////                if (savedJsonString != jsonString)
////                    throw new InvalidOperationException("Ошибка: не удалось подтвердить запись данных в файл.");
////            }
////            catch (Exception ex)
////            {
////                // Выводим ошибку в лог
////                Debug.WriteLine($"Ошибка при сохранении настроек в JSON: {ex}");
////                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");

////                // Повторно выбрасываем исключение
////                throw;
////            }
////        }

////        // Загрузка настроек из JSON файла
////        public T LoadSettingsFromJson<T>(string filePath)
////        {
////            // Проверяем, что путь к файлу не пустой
////            if (string.IsNullOrWhiteSpace(filePath))
////                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

////            // Проверяем, что файл существует
////            if (!File.Exists(filePath))
////                throw new FileNotFoundException("Файл настроек не найден.", filePath);

////            // Выводим сообщение в лог о загрузке настроек
////            Debug.WriteLine($"Загрузка настроек из JSON файла: {filePath}");

////            try
////            {
////                // Читаем содержимое файла
////                string jsonString = File.ReadAllText(filePath);

////                // Выводим содержимое JSON в лог
////                Debug.WriteLine($"Содержимое JSON: {jsonString}");

////                // Десериализуем JSON строку в объект
////                Debug.WriteLine($"Десериализация JSON в {typeof(T).Name}");
////                return JsonSerializer.Deserialize<T>(jsonString);
////            }
////            catch (Exception ex)
////            {
////                // Выводим ошибку в лог
////                Debug.WriteLine($"Ошибка при загрузке настроек из JSON: {ex}");
////                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");

////                // Повторно выбрасываем исключение
////                throw;
////            }
////        }

////        // Перенос всех настроек из локального контейнера в JSON файл
////        public void TransferPropertyToJson(string filePath)
////        {
////            // Проверяем, что путь к файлу не пустой
////            if (string.IsNullOrWhiteSpace(filePath))
////                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

////            // Выводим сообщение в лог о переносе настроек
////            Debug.WriteLine($"Перенос настроек из локального контейнера в JSON файл: {filePath}");

////            try
////            {
////                // Создаем словарь для хранения всех настроек
////                var settingsDictionary = new Dictionary<string, object>();

////                // Перебираем все ключи и значения в локальном контейнере
////                foreach (var key in localSettings.Values.Keys)
////                {
////                    settingsDictionary[key] = localSettings.Values[key];
////                }

////                // Сериализуем словарь в JSON строку
////                string jsonString = JsonSerializer.Serialize(settingsDictionary);

////                // Проверяем и создаем файл, если его нет
////                CheckAndCreateFile(filePath);

////                // Записываем JSON строку в файл
////                File.WriteAllText(filePath, jsonString);

////                // Читаем содержимое файла для проверки корректности записи
////                string savedJsonString = File.ReadAllText(filePath);
////                if (settingsDictionary != JsonSerializer.Deserialize<Dictionary<string, object>>(savedJsonString))
////                    throw new InvalidOperationException("Ошибка: не удалось подтвердить запись данных в файл.");
////            }
////            catch (Exception ex)
////            {
////                // Выводим ошибку в лог
////                Debug.WriteLine($"Ошибка при переносе настроек в JSON: {ex}");
////                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");

////                // Повторно выбрасываем исключение
////                throw;
////            }
////        }

////        // Перенос всех настроек из JSON файла в локальный контейнер
////        public void TransferJsonToProperty(string filePath)
////        {
////            // Проверяем, что путь к файлу не пустой
////            if (string.IsNullOrWhiteSpace(filePath))
////                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

////            // Проверяем, что файл существует
////            if (!File.Exists(filePath))
////                throw new FileNotFoundException("Файл JSON не найден.", filePath);

////            // Выводим сообщение в лог о переносе настроек
////            Debug.WriteLine($"Перенос настроек из JSON файла в локальный контейнер: {filePath}");

////            try
////            {
////                // Читаем содержимое файла
////                string jsonString = File.ReadAllText(filePath);

////                // Десериализуем JSON строку в словарь
////                var settingsDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

////                // Перебираем все пары ключ-значение и сохраняем их в локальный контейнер
////                foreach (var kvp in settingsDictionary)
////                {
////                    localSettings.Values[kvp.Key] = kvp.Value;
////                }
////            }
////            catch (Exception ex)
////            {
////                // Выводим ошибку в лог
////                Debug.WriteLine($"Ошибка при переносе настроек в локальный контейнер: {ex}");
////                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");

////                // Повторно выбрасываем исключение
////                throw;
////            }
////        }

////        // Проверка и создание файла, если он не существует
////        private void CheckAndCreateFile(string filePath)
////        {
////            // Проверяем, что путь к файлу не пустой
////            if (string.IsNullOrWhiteSpace(filePath))
////                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

////            // Проверяем, существует ли файл
////            if (!File.Exists(filePath))
////            {
////                // Выводим сообщение в лог о создании файла
////                Debug.WriteLine($"Файл не существует. Создаем файл: {filePath}");

////                try
////                {
////                    // Создаем файл с пустым JSON объектом
////                    File.WriteAllText(filePath, "{}");
////                }
////                catch (Exception ex)
////                {
////                    // Выводим ошибку в лог
////                    Debug.WriteLine($"Ошибка при создании файла: {ex}");
////                    Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");

////                    // Повторно выбрасываем исключение
////                    throw;
////                }
////            }
////        }
////    }
////}
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.Text.Json;
//using Windows.Storage;
//using Microsoft.UI.Xaml;

//namespace SettingManager
//{
//    public class SettingsManager
//    {
//        private static SettingsManager _instance;
//        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

//        public static SettingsManager Instance => _instance ??= new SettingsManager();

//        public void SaveSetting(string key, object value)
//        {
//            if (string.IsNullOrWhiteSpace(key))
//                throw new ArgumentException("Key cannot be empty", nameof(key));

//            if (value == null)
//            {
//                localSettings.Values.Remove(key);
//                return;
//            }

//            try
//            {
//                object serializableValue = ConvertToSerializableValue(value);

//                // Проверка на отрицательные значения для числовых типов
//                if (serializableValue is IConvertible convertible &&
//                    IsNumericType(value.GetType()) &&
//                    Convert.ToDouble(convertible) < 0)
//                {
//                    Debug.WriteLine($"Attempt to save negative value for {key}: {value}");
//                    return;
//                }

//                localSettings.Values[key] = serializableValue;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error saving setting '{key}': {ex.Message}");
//            }
//        }

//        private object ConvertToSerializableValue(object value)
//        {
//            return value switch
//            {
//                // Обработка специальных типов
//                Visibility visibility => visibility == Visibility.Visible ? "Visible" : "Collapsed",
//                Enum enumValue => enumValue.ToString(),

//                // Числовые типы сохраняем как строки для избежания проблем сериализации
//                float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
//                double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
//                decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),

//                // Для остальных типов используем как есть
//                _ => value
//            };
//        }

//        public T GetSetting<T>(string key, T defaultValue = default)
//        {
//            if (string.IsNullOrWhiteSpace(key))
//                throw new ArgumentException("Key cannot be empty", nameof(key));

//            try
//            {
//                if (!localSettings.Values.TryGetValue(key, out object value) || value == null)
//                    return defaultValue;

//                return ConvertFromStoredValue<T>(value);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error getting setting '{key}': {ex.Message}");
//                return defaultValue;
//            }
//        }

//        private T ConvertFromStoredValue<T>(object storedValue)
//        {
//            Type targetType = typeof(T);

//            // Обработка специальных типов
//            if (targetType == typeof(Visibility))
//            {
//                return (T)(object)(storedValue.ToString() == "Visible" ?
//                    Visibility.Visible : Visibility.Collapsed);
//            }

//            if (targetType.IsEnum)
//            {
//                return (T)Enum.Parse(targetType, storedValue.ToString());
//            }

//            // Обработка числовых типов, сохраненных как строки
//            if (storedValue is string stringValue)
//            {
//                if (targetType == typeof(float) &&
//                    float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatResult))
//                {
//                    return (T)(object)floatResult;
//                }
//                else if (targetType == typeof(double) &&
//                         double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleResult))
//                {
//                    return (T)(object)doubleResult;
//                }
//                else if (targetType == typeof(decimal) &&
//                         decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalResult))
//                {
//                    return (T)(object)decimalResult;
//                }
//            }

//            // Стандартное преобразование
//            return (T)Convert.ChangeType(storedValue, targetType);
//        }

//        private bool IsNumericType(Type type)
//        {
//            return Type.GetTypeCode(type) switch
//            {
//                TypeCode.SByte or TypeCode.Byte or
//                TypeCode.Int16 or TypeCode.UInt16 or
//                TypeCode.Int32 or TypeCode.UInt32 or
//                TypeCode.Int64 or TypeCode.UInt64 or
//                TypeCode.Single or TypeCode.Double or
//                TypeCode.Decimal => true,
//                _ => false
//            };
//        }

//        public void CleanInvalidSettings()
//        {
//            try
//            {
//                var keysToRemove = new List<string>();
//                foreach (var key in localSettings.Values.Keys)
//                {
//                    var value = localSettings.Values[key];
//                    bool shouldRemove = value switch
//                    {
//                        long l when l < 0 => true,
//                        int i when i < 0 => true,
//                        double d when d < 0 => true,
//                        string s when (s.Contains("-") &&
//                            (float.TryParse(s, out float f) && f < 0 ||
//                             double.TryParse(s, out double dbl) && dbl < 0)) => true,
//                        _ => false
//                    };

//                    if (shouldRemove)
//                    {
//                        keysToRemove.Add(key);
//                        Debug.WriteLine($"Flagged invalid setting for removal: {key} = {value}");
//                    }
//                }

//                foreach (var key in keysToRemove)
//                {
//                    localSettings.Values.Remove(key);
//                    Debug.WriteLine($"Removed invalid setting: {key}");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error cleaning settings: {ex.Message}");
//            }
//        }

//        public void SaveSettingsToJson<T>(string filePath, T settings)
//        {
//            if (string.IsNullOrWhiteSpace(filePath))
//                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

//            if (settings == null)
//                throw new ArgumentNullException(nameof(settings), "Объект настроек не может быть null.");

//            Debug.WriteLine($"Сохранение настроек в JSON файл: {filePath}");

//            try
//            {
//                string jsonString = JsonSerializer.Serialize(settings);
//                CheckAndCreateFile(filePath);
//                File.WriteAllText(filePath, jsonString);

//                string savedJsonString = File.ReadAllText(filePath);
//                if (savedJsonString != jsonString)
//                    throw new InvalidOperationException("Ошибка: не удалось подтвердить запись данных в файл.");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка при сохранении настроек в JSON: {ex}");
//                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
//                throw;
//            }
//        }

//        public T LoadSettingsFromJson<T>(string filePath)
//        {
//            if (string.IsNullOrWhiteSpace(filePath))
//                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

//            if (!File.Exists(filePath))
//                throw new FileNotFoundException("Файл настроек не найден.", filePath);

//            Debug.WriteLine($"Загрузка настроек из JSON файла: {filePath}");

//            try
//            {
//                string jsonString = File.ReadAllText(filePath);
//                Debug.WriteLine($"Содержимое JSON: {jsonString}");
//                Debug.WriteLine($"Десериализация JSON в {typeof(T).Name}");
//                return JsonSerializer.Deserialize<T>(jsonString);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка при загрузке настроек из JSON: {ex}");
//                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
//                throw;
//            }
//        }

//        public void TransferPropertyToJson(string filePath)
//        {
//            if (string.IsNullOrWhiteSpace(filePath))
//                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

//            Debug.WriteLine($"Перенос настроек из локального контейнера в JSON файл: {filePath}");

//            try
//            {
//                var settingsDictionary = new Dictionary<string, object>();
//                foreach (var key in localSettings.Values.Keys)
//                {
//                    settingsDictionary[key] = localSettings.Values[key];
//                }

//                string jsonString = JsonSerializer.Serialize(settingsDictionary);
//                CheckAndCreateFile(filePath);
//                File.WriteAllText(filePath, jsonString);

//                string savedJsonString = File.ReadAllText(filePath);
//                if (settingsDictionary != JsonSerializer.Deserialize<Dictionary<string, object>>(savedJsonString))
//                    throw new InvalidOperationException("Ошибка: не удалось подтвердить запись данных в файл.");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка при переносе настроек в JSON: {ex}");
//                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
//                throw;
//            }
//        }

//        public void TransferJsonToProperty(string filePath)
//        {
//            if (string.IsNullOrWhiteSpace(filePath))
//                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

//            if (!File.Exists(filePath))
//                throw new FileNotFoundException("Файл JSON не найден.", filePath);

//            Debug.WriteLine($"Перенос настроек из JSON файла в локальный контейнер: {filePath}");

//            try
//            {
//                string jsonString = File.ReadAllText(filePath);
//                var settingsDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

//                foreach (var kvp in settingsDictionary)
//                {
//                    localSettings.Values[kvp.Key] = kvp.Value;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка при переносе настроек в локальный контейнер: {ex}");
//                Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
//                throw;
//            }
//        }

//        private void CheckAndCreateFile(string filePath)
//        {
//            if (string.IsNullOrWhiteSpace(filePath))
//                throw new ArgumentException("Путь к файлу не может быть пустым или null.", nameof(filePath));

//            if (!File.Exists(filePath))
//            {
//                Debug.WriteLine($"Файл не существует. Создаем файл: {filePath}");

//                try
//                {
//                    File.WriteAllText(filePath, "{}");
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Ошибка при создании файла: {ex}");
//                    Debug.WriteLine($"Стек трассировки: {ex.StackTrace}");
//                    throw;
//                }
//            }
//        }
//    }
//}


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