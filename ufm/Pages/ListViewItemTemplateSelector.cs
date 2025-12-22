using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System;

namespace ufm
{
    public partial class ListViewItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MyComputerTemplate { get; set; }
        public DataTemplate DriveTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ExplorerItemViewModel vm)
            {
                try
                {
                    // Используем switch с when для структурной проверки
                    switch (vm)
                    {
                        case var _ when IsMyComputer(vm):
                            return MyComputerTemplate;
                        case var _ when IsDrive(vm.FilePath):
                            return DriveTemplate;
                        case var _ when Directory.Exists(vm.FilePath):
                            return FolderTemplate;
                        case var _ when File.Exists(vm.FilePath):
                            return FileTemplate;
                    }
                }
                catch (Exception ex)
                {
                    // Логирование ошибки
                    Console.WriteLine($"Ошибка при выборе шаблона: {ex.Message}");
                }
            }

            // Возвращаем базовый шаблон, если элемент не распознан
            return base.SelectTemplateCore(item, container);
        }

        private bool IsMyComputer(ExplorerItemViewModel vm)
        {
            return vm.Name == "Мой Компьютер" || vm.FilePath == "Мой Компьютер";
        }

        private bool IsDrive(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                // Оптимизация: проверка формата пути перед вызовом GetPathRoot
                if (filePath.Length == 3 && filePath[1] == ':' && filePath[2] == '\\')
                {
                    return true;
                }

                return filePath == Path.GetPathRoot(filePath);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (PathTooLongException)
            {
                return false;
            }
        }
    }
}

