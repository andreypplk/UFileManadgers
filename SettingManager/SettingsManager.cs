

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
//            // Обработка enum ДО проверки числовых типов
//            if (value.GetType().IsEnum)
//            {
//                // Получаем базовый тип enum и преобразуем
//                Type underlyingType = Enum.GetUnderlyingType(value.GetType());
//                return Convert.ChangeType(value, underlyingType);
//            }

//            return value switch
//            {
//                // Обработка специальных типов
//                Visibility visibility => visibility == Visibility.Visible ? "Visible" : "Collapsed",

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

//            // Обработка enum с поддержкой числовых значений
//            if (targetType.IsEnum)
//            {
//                try
//                {
//                    if (storedValue is string stringValue)
//                    {
//                        // Пытаемся распарсить как строку (например, "Panel0")
//                        return (T)Enum.Parse(targetType, stringValue);
//                    }
//                    else
//                    {
//                        // Если значение числовое, используем Enum.ToObject
//                        Type underlyingType = Enum.GetUnderlyingType(targetType);
//                        object numericValue = Convert.ChangeType(storedValue, underlyingType);
//                        return (T)Enum.ToObject(targetType, numericValue);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error converting to enum {targetType.Name}: {ex.Message}");
//                    return default(T);
//                }
//            }

//            // Обработка числовых типов, сохраненных как строки
//            if (storedValue is string stringValueNumeric)
//            {
//                if (targetType == typeof(float) &&
//                    float.TryParse(stringValueNumeric, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatResult))
//                {
//                    return (T)(object)floatResult;
//                }
//                else if (targetType == typeof(double) &&
//                         double.TryParse(stringValueNumeric, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleResult))
//                {
//                    return (T)(object)doubleResult;
//                }
//                else if (targetType == typeof(decimal) &&
//                         decimal.TryParse(stringValueNumeric, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalResult))
//                {
//                    return (T)(object)decimalResult;
//                }
//            }

//            // Стандартное преобразование
//            try
//            {
//                return (T)Convert.ChangeType(storedValue, targetType);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error converting {storedValue} to {targetType.Name}: {ex.Message}");
//                return default(T);
//            }
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

//using Microsoft.UI.Xaml;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Text.Json;
//using Windows.Storage;

//namespace SettingManager
//{
//    public class SettingsManager
//    {
//        private static SettingsManager _instance;
//        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
//        private readonly string _jsonFilePath;
//        private readonly object _fileLock = new object();

//        public static SettingsManager Instance => _instance ??= new SettingsManager();

//        //public SettingsManager()
//        //{
//        //    string folder;
//        //    try
//        //    {
//        //        // При обычном запуске без пакета Package.Current == null → папка с EXE
//        //        folder = Windows.ApplicationModel.Package.Current != null
//        //            ? ApplicationData.Current.LocalFolder.Path
//        //            : AppContext.BaseDirectory;
//        //    }
//        //    catch
//        //    {
//        //        folder = AppContext.BaseDirectory;
//        //    }
//        //    _jsonFilePath = Path.Combine(folder, "settings.json");
//        //    InitializeJsonFile();
//        //}
//        public SettingsManager()
//        {
//            // Всегда папка с exe (портативный режим)
//            string folder = AppContext.BaseDirectory;
//            _jsonFilePath = Path.Combine(folder, "settings.json");
//            InitializeJsonFile();
//        }
//        /// <summary>
//        /// Проверяет наличие JSON-файла, при необходимости создаёт и переносит данные из LocalSettings.
//        /// При повреждении файла восстанавливает его из LocalSettings.
//        /// </summary>
//        private void InitializeJsonFile()
//        {
//            lock (_fileLock)
//            {
//                try
//                {
//                    if (!File.Exists(_jsonFilePath))
//                    {
//                        TransferPropertyToJson(_jsonFilePath);
//                    }
//                    else
//                    {
//                        string jsonString = File.ReadAllText(_jsonFilePath);
//                        JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"JSON settings file is missing or corrupted: {ex.Message}. Restoring from LocalSettings.");
//                    try
//                    {
//                        if (File.Exists(_jsonFilePath))
//                            File.Delete(_jsonFilePath);
//                        TransferPropertyToJson(_jsonFilePath);
//                    }
//                    catch (Exception restoreEx)
//                    {
//                        Debug.WriteLine($"Failed to restore JSON settings file: {restoreEx.Message}. Falling back to LocalSettings only.");
//                    }
//                }
//            }
//        }

//        // ---------- Основные методы сохранения / загрузки ----------

//        public void SaveSetting(string key, object value)
//        {
//            if (string.IsNullOrWhiteSpace(key))
//                throw new ArgumentException("Key cannot be empty", nameof(key));

//            if (value == null)
//            {
//                RemoveSetting(key);
//                return;
//            }

//            try
//            {
//                object serializableValue = ConvertToSerializableValue(value);

//                if (serializableValue is IConvertible convertible &&
//                    IsNumericType(value.GetType()) &&
//                    Convert.ToDouble(convertible) < 0)
//                {
//                    Debug.WriteLine($"Attempt to save negative value for {key}: {value}");
//                    return;
//                }

//                SaveToJson(key, serializableValue);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error saving setting '{key}' to JSON: {ex.Message}. Falling back to LocalSettings.");
//                try
//                {
//                    localSettings.Values[key] = value;
//                }
//                catch (Exception localEx)
//                {
//                    Debug.WriteLine($"Error saving to LocalSettings: {localEx.Message}");
//                }
//            }
//        }

//        public T GetSetting<T>(string key, T defaultValue = default)
//        {
//            if (string.IsNullOrWhiteSpace(key))
//                throw new ArgumentException("Key cannot be empty", nameof(key));

//            try
//            {
//                if (TryGetValueFromJson(key, out object jsonValue) && jsonValue != null)
//                {
//                    return ConvertFromStoredValue<T>(jsonValue);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error reading '{key}' from JSON: {ex.Message}");
//            }

//            try
//            {
//                if (localSettings.Values.TryGetValue(key, out object localValue) && localValue != null)
//                {
//                    return ConvertFromStoredValue<T>(localValue);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error reading '{key}' from LocalSettings: {ex.Message}");
//            }

//            return defaultValue;
//        }

//        private void RemoveSetting(string key)
//        {
//            try
//            {
//                RemoveKeyFromJson(key);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error removing '{key}' from JSON: {ex.Message}");
//            }
//            finally
//            {
//                localSettings.Values.Remove(key);
//            }
//        }

//        // ---------- Работа с JSON-файлом ----------

//        private void SaveToJson(string key, object serializableValue)
//        {
//            lock (_fileLock)
//            {
//                var dict = LoadJsonDictionary();
//                dict[key] = serializableValue;
//                SaveSettingsToJson(_jsonFilePath, dict);
//            }
//        }

//        private void RemoveKeyFromJson(string key)
//        {
//            lock (_fileLock)
//            {
//                var dict = LoadJsonDictionary();
//                if (dict.Remove(key))
//                {
//                    SaveSettingsToJson(_jsonFilePath, dict);
//                }
//            }
//        }

//        private bool TryGetValueFromJson(string key, out object value)
//        {
//            lock (_fileLock)
//            {
//                var dict = LoadJsonDictionary();
//                return dict.TryGetValue(key, out value);
//            }
//        }

//        // ---------- Ключевой метод: загрузка словаря с конвертацией JsonElement ----------
//        private Dictionary<string, object> LoadJsonDictionary()
//        {
//            if (!File.Exists(_jsonFilePath))
//            {
//                InitializeJsonFile();
//            }
//            var rawDict = LoadSettingsFromJson<Dictionary<string, object>>(_jsonFilePath);
//            if (rawDict == null)
//                return new Dictionary<string, object>();

//            // Конвертируем все JsonElement в реальные типы
//            var result = new Dictionary<string, object>();
//            foreach (var kvp in rawDict)
//            {
//                result[kvp.Key] = ConvertJsonElement(kvp.Value);
//            }
//            return result;
//        }

//        /// <summary>
//        /// Рекурсивно преобразует объект JsonElement в соответствующий примитивный тип или структуру данных.
//        /// </summary>
//        private object ConvertJsonElement(object value)
//        {
//            if (value is JsonElement jsonElement)
//            {
//                switch (jsonElement.ValueKind)
//                {
//                    case JsonValueKind.String:
//                        return jsonElement.GetString();
//                    case JsonValueKind.Number:
//                        if (jsonElement.TryGetInt32(out int intVal))
//                            return intVal;
//                        if (jsonElement.TryGetInt64(out long longVal))
//                            return longVal;
//                        if (jsonElement.TryGetDouble(out double dblVal))
//                            return dblVal;
//                        return jsonElement.GetDecimal();
//                    case JsonValueKind.True:
//                        return true;
//                    case JsonValueKind.False:
//                        return false;
//                    case JsonValueKind.Null:
//                        return null;
//                    case JsonValueKind.Array:
//                        var list = new List<object>();
//                        foreach (var item in jsonElement.EnumerateArray())
//                        {
//                            list.Add(ConvertJsonElement(item));
//                        }
//                        return list;
//                    case JsonValueKind.Object:
//                        var dict = new Dictionary<string, object>();
//                        foreach (var property in jsonElement.EnumerateObject())
//                        {
//                            dict[property.Name] = ConvertJsonElement(property.Value);
//                        }
//                        return dict;
//                    default:
//                        return jsonElement.ToString();
//                }
//            }
//            return value;
//        }

//        // ---------- Вспомогательные методы ----------

//        private object ConvertToSerializableValue(object value)
//        {
//            if (value.GetType().IsEnum)
//            {
//                Type underlyingType = Enum.GetUnderlyingType(value.GetType());
//                return Convert.ChangeType(value, underlyingType);
//            }

//            return value switch
//            {
//                Visibility visibility => visibility == Visibility.Visible ? "Visible" : "Collapsed",
//                float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
//                double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
//                decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
//                _ => value
//            };
//        }

//        private T ConvertFromStoredValue<T>(object storedValue)
//        {
//            Type targetType = typeof(T);

//            if (targetType == typeof(Visibility))
//            {
//                return (T)(object)(storedValue.ToString() == "Visible" ?
//                    Visibility.Visible : Visibility.Collapsed);
//            }

//            if (targetType.IsEnum)
//            {
//                try
//                {
//                    if (storedValue is string stringValue)
//                    {
//                        return (T)Enum.Parse(targetType, stringValue);
//                    }
//                    else
//                    {
//                        Type underlyingType = Enum.GetUnderlyingType(targetType);
//                        object numericValue = Convert.ChangeType(storedValue, underlyingType);
//                        return (T)Enum.ToObject(targetType, numericValue);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error converting to enum {targetType.Name}: {ex.Message}");
//                    return default;
//                }
//            }

//            if (storedValue is string str)
//            {
//                if (targetType == typeof(float) &&
//                    float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
//                    return (T)(object)f;
//                if (targetType == typeof(double) &&
//                    double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
//                    return (T)(object)d;
//                if (targetType == typeof(decimal) &&
//                    decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal m))
//                    return (T)(object)m;
//            }

//            try
//            {
//                return (T)Convert.ChangeType(storedValue, targetType);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error converting {storedValue} to {targetType.Name}: {ex.Message}");
//                return default;
//            }
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

//        // ---------- Публичные методы переноса / сброса ----------

//        public void CleanInvalidSettings()
//        {
//            try
//            {
//                var keysToRemove = new List<string>();
//                var jsonDict = LoadJsonDictionary();
//                foreach (var kvp in jsonDict)
//                {
//                    if (IsInvalidValue(kvp.Value))
//                        keysToRemove.Add(kvp.Key);
//                }
//                foreach (var key in keysToRemove)
//                {
//                    jsonDict.Remove(key);
//                }
//                SaveSettingsToJson(_jsonFilePath, jsonDict);
//                keysToRemove.Clear();

//                foreach (var key in localSettings.Values.Keys)
//                {
//                    if (IsInvalidValue(localSettings.Values[key]))
//                        keysToRemove.Add(key);
//                }
//                foreach (var key in keysToRemove)
//                {
//                    localSettings.Values.Remove(key);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error cleaning settings: {ex.Message}");
//            }
//        }

//        private bool IsInvalidValue(object value)
//        {
//            return value switch
//            {
//                long l when l < 0 => true,
//                int i when i < 0 => true,
//                double d when d < 0 => true,
//                string s when (s.Contains("-") &&
//                    (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double dbl) && dbl < 0)) => true,
//                _ => false
//            };
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

//        public void ResetAllSettings()
//        {
//            localSettings.Values.Clear();
//            foreach (string containerKey in localSettings.Containers.Keys.ToList())
//            {
//                localSettings.DeleteContainer(containerKey);
//            }

//            lock (_fileLock)
//            {
//                try
//                {
//                    if (File.Exists(_jsonFilePath))
//                        File.Delete(_jsonFilePath);

//                    CheckAndCreateFile(_jsonFilePath);
//                    Debug.WriteLine("All settings reset successfully.");
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error resetting JSON settings: {ex.Message}");
//                }
//            }
//        }
//    }
//}


using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.Storage;

namespace SettingManager
{
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private readonly string _jsonFilePath;
        private readonly object _fileLock = new object();

        private const string PortableModeKey = "IsPortableMode";

        public static SettingsManager Instance => _instance ??= new SettingsManager();

        public SettingsManager()
        {
            string folder = GetStorageFolder();
            _jsonFilePath = Path.Combine(folder, "settings.json");
            InitializeJsonFile();
        }

        private static string GetStorageFolder()
        {
            if (localSettings.Values.TryGetValue(PortableModeKey, out object rawValue) && rawValue is bool portable)
            {
                return portable
                    ? AppContext.BaseDirectory
                    : ApplicationData.Current.LocalFolder.Path;
            }

            try
            {
                return Windows.ApplicationModel.Package.Current != null
                    ? ApplicationData.Current.LocalFolder.Path
                    : AppContext.BaseDirectory;
            }
            catch
            {
                return AppContext.BaseDirectory;
            }
        }

        public static void SetStorageMode(bool portable)
        {
            localSettings.Values[PortableModeKey] = portable;

            string currentFolder = GetStorageFolder();
            string targetFolder = portable
                ? AppContext.BaseDirectory
                : ApplicationData.Current.LocalFolder.Path;
            string currentFilePath = Path.Combine(currentFolder, "settings.json");
            string targetFilePath = Path.Combine(targetFolder, "settings.json");

            if (!string.Equals(currentFolder, targetFolder, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(currentFilePath))
            {
                try
                {
                    string targetDir = Path.GetDirectoryName(targetFilePath);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    File.Copy(currentFilePath, targetFilePath, overwrite: true);
                    Debug.WriteLine($"Settings file copied to {targetFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to migrate settings file: {ex.Message}");
                }
            }
        }

        public void ExportSettings(string destinationPath)
        {
            lock (_fileLock)
            {
                if (!File.Exists(_jsonFilePath))
                    throw new FileNotFoundException("Settings file not found.", _jsonFilePath);
                File.Copy(_jsonFilePath, destinationPath, overwrite: true);
            }
        }

        public bool ImportSettings(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Import file not found.", sourcePath);

            try
            {
                string json = File.ReadAllText(sourcePath);
                JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch
            {
                throw new InvalidDataException("Invalid settings file format.");
            }

            lock (_fileLock)
            {
                File.Copy(sourcePath, _jsonFilePath, overwrite: true);
            }

            try
            {
                TransferJsonToProperty(_jsonFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Could not sync to LocalSettings after import: {ex.Message}");
            }

            return true;
        }

        private void InitializeJsonFile()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!File.Exists(_jsonFilePath))
                    {
                        TransferPropertyToJson(_jsonFilePath);
                    }
                    else
                    {
                        string jsonString = File.ReadAllText(_jsonFilePath);
                        JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JSON settings file is missing or corrupted: {ex.Message}. Restoring from LocalSettings.");
                    try
                    {
                        if (File.Exists(_jsonFilePath))
                            File.Delete(_jsonFilePath);
                        TransferPropertyToJson(_jsonFilePath);
                    }
                    catch (Exception restoreEx)
                    {
                        Debug.WriteLine($"Failed to restore JSON settings file: {restoreEx.Message}. Falling back to LocalSettings only.");
                    }
                }
            }
        }

        public void SaveSetting(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            if (value == null)
            {
                RemoveSetting(key);
                return;
            }

            try
            {
                object serializableValue = ConvertToSerializableValue(value);

                if (serializableValue is IConvertible convertible &&
                    IsNumericType(value.GetType()) &&
                    Convert.ToDouble(convertible) < 0)
                {
                    Debug.WriteLine($"Attempt to save negative value for {key}: {value}");
                    return;
                }

                SaveToJson(key, serializableValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving setting '{key}' to JSON: {ex.Message}. Falling back to LocalSettings.");
                try
                {
                    localSettings.Values[key] = value;
                }
                catch (Exception localEx)
                {
                    Debug.WriteLine($"Error saving to LocalSettings: {localEx.Message}");
                }
            }
        }

        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be empty", nameof(key));

            try
            {
                if (TryGetValueFromJson(key, out object jsonValue) && jsonValue != null)
                {
                    return ConvertFromStoredValue<T>(jsonValue);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading '{key}' from JSON: {ex.Message}");
            }

            try
            {
                if (localSettings.Values.TryGetValue(key, out object localValue) && localValue != null)
                {
                    return ConvertFromStoredValue<T>(localValue);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading '{key}' from LocalSettings: {ex.Message}");
            }

            return defaultValue;
        }

        private void RemoveSetting(string key)
        {
            try
            {
                RemoveKeyFromJson(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing '{key}' from JSON: {ex.Message}");
            }
            finally
            {
                localSettings.Values.Remove(key);
            }
        }

        private void SaveToJson(string key, object serializableValue)
        {
            lock (_fileLock)
            {
                var dict = LoadJsonDictionary();
                dict[key] = serializableValue;
                SaveSettingsToJson(_jsonFilePath, dict);
            }
        }

        private void RemoveKeyFromJson(string key)
        {
            lock (_fileLock)
            {
                var dict = LoadJsonDictionary();
                if (dict.Remove(key))
                {
                    SaveSettingsToJson(_jsonFilePath, dict);
                }
            }
        }

        private bool TryGetValueFromJson(string key, out object value)
        {
            lock (_fileLock)
            {
                var dict = LoadJsonDictionary();
                return dict.TryGetValue(key, out value);
            }
        }

        private Dictionary<string, object> LoadJsonDictionary()
        {
            if (!File.Exists(_jsonFilePath))
            {
                InitializeJsonFile();
            }
            var rawDict = LoadSettingsFromJson<Dictionary<string, object>>(_jsonFilePath);
            if (rawDict == null)
                return new Dictionary<string, object>();

            var result = new Dictionary<string, object>();
            foreach (var kvp in rawDict)
            {
                result[kvp.Key] = ConvertJsonElement(kvp.Value);
            }
            return result;
        }

        private object ConvertJsonElement(object value)
        {
            if (value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        return jsonElement.GetString();
                    case JsonValueKind.Number:
                        if (jsonElement.TryGetInt32(out int intVal))
                            return intVal;
                        if (jsonElement.TryGetInt64(out long longVal))
                            return longVal;
                        if (jsonElement.TryGetDouble(out double dblVal))
                            return dblVal;
                        return jsonElement.GetDecimal();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.Array:
                        var list = new List<object>();
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            list.Add(ConvertJsonElement(item));
                        }
                        return list;
                    case JsonValueKind.Object:
                        var dict = new Dictionary<string, object>();
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            dict[property.Name] = ConvertJsonElement(property.Value);
                        }
                        return dict;
                    default:
                        return jsonElement.ToString();
                }
            }
            return value;
        }

        private object ConvertToSerializableValue(object value)
        {
            if (value.GetType().IsEnum)
            {
                Type underlyingType = Enum.GetUnderlyingType(value.GetType());
                return Convert.ChangeType(value, underlyingType);
            }

            return value switch
            {
                Visibility visibility => visibility == Visibility.Visible ? "Visible" : "Collapsed",
                float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
                double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
                decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
                _ => value
            };
        }

        private T ConvertFromStoredValue<T>(object storedValue)
        {
            Type targetType = typeof(T);

            if (targetType == typeof(Visibility))
            {
                return (T)(object)(storedValue.ToString() == "Visible" ?
                    Visibility.Visible : Visibility.Collapsed);
            }

            if (targetType.IsEnum)
            {
                try
                {
                    if (storedValue is string stringValue)
                    {
                        return (T)Enum.Parse(targetType, stringValue);
                    }
                    else
                    {
                        Type underlyingType = Enum.GetUnderlyingType(targetType);
                        object numericValue = Convert.ChangeType(storedValue, underlyingType);
                        return (T)Enum.ToObject(targetType, numericValue);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error converting to enum {targetType.Name}: {ex.Message}");
                    return default;
                }
            }

            if (storedValue is string str)
            {
                if (targetType == typeof(float) &&
                    float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    return (T)(object)f;
                if (targetType == typeof(double) &&
                    double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return (T)(object)d;
                if (targetType == typeof(decimal) &&
                    decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal m))
                    return (T)(object)m;
            }

            try
            {
                return (T)Convert.ChangeType(storedValue, targetType);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error converting {storedValue} to {targetType.Name}: {ex.Message}");
                return default;
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
                var jsonDict = LoadJsonDictionary();
                foreach (var kvp in jsonDict)
                {
                    if (IsInvalidValue(kvp.Value))
                        keysToRemove.Add(kvp.Key);
                }
                foreach (var key in keysToRemove)
                {
                    jsonDict.Remove(key);
                }
                SaveSettingsToJson(_jsonFilePath, jsonDict);
                keysToRemove.Clear();

                foreach (var key in localSettings.Values.Keys)
                {
                    if (IsInvalidValue(localSettings.Values[key]))
                        keysToRemove.Add(key);
                }
                foreach (var key in keysToRemove)
                {
                    localSettings.Values.Remove(key);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning settings: {ex.Message}");
            }
        }

        private bool IsInvalidValue(object value)
        {
            return value switch
            {
                long l when l < 0 => true,
                int i when i < 0 => true,
                double d when d < 0 => true,
                string s when (s.Contains("-") &&
                    (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double dbl) && dbl < 0)) => true,
                _ => false
            };
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

        public void ResetAllSettings()
        {
            localSettings.Values.Clear();
            foreach (string containerKey in localSettings.Containers.Keys.ToList())
            {
                localSettings.DeleteContainer(containerKey);
            }

            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_jsonFilePath))
                        File.Delete(_jsonFilePath);

                    CheckAndCreateFile(_jsonFilePath);
                    Debug.WriteLine("All settings reset successfully.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error resetting JSON settings: {ex.Message}");
                }
            }
        }
        public static bool IsPortableMode()
        {
            if (localSettings.Values.TryGetValue(PortableModeKey, out object rawValue) && rawValue is bool portable)
                return portable;

            try
            {
                return Windows.ApplicationModel.Package.Current == null;
            }
            catch
            {
                return true; // по умолчанию считаем портативным
            }
        }
    }
}