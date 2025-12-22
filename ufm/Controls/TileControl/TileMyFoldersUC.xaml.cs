using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace ufm
{
    public sealed partial class TileMyFoldersUc : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;

        public TileMyFoldersUc()
        {
            Debug.WriteLine($"[TileMyFoldersUc] Конструктор вызван");
            this.InitializeComponent();

            if (BorderTileFolders != null)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Инициализация ScaleAnimator");
                _scaleAnimator = new ScaleAnimator(BorderTileFolders);
            }
            else
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка: BorderTileFolders не найден");
            }
        }

        protected override void OnDisplayModeChanged()
        {
            Debug.WriteLine($"[TileMyFoldersUc] OnDisplayModeChanged: DisplayMode = {DisplayMode}");
            base.OnDisplayModeChanged();

            try
            {
                if (HorizontalLayout == null || VerticalLayout == null)
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Макеты не инициализированы");
                    return;
                }

                // Переключение между режимами
                if (DisplayMode == "Vertical")
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка вертикального режима");
                    HorizontalLayout.Visibility = Visibility.Collapsed;
                    VerticalLayout.Visibility = Visibility.Visible;
                }
                else
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка горизонтального режима");
                    HorizontalLayout.Visibility = Visibility.Visible;
                    VerticalLayout.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в OnDisplayModeChanged: {ex.Message}");
            }
        }

        protected override void UpdateSize()
        {
            Debug.WriteLine($"[TileMyFoldersUc] UpdateSize: Size = {Size}");
            base.UpdateSize();

            try
            {
                if (FolderNameText == null || ItemsCountText == null || LastModifiedText == null)
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Основные элементы не инициализированы");
                    return;
                }

                Debug.WriteLine($"[TileMyFoldersUc] Текущие параметры: FontSize={FontSize}, IconWidth={IconWidth}, IconHeight={IconHeight}");

                // Настройка видимости и размеров в зависимости от выбранного размера
                switch (Size?.ToLower())
                {
                    case "extra small":
                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Small");
                        SetElementsVisibility(false);
                        break;

                    case "small":
                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Small");
                        SetElementsVisibility(false);
                        FolderNameText.VerticalAlignment = VerticalAlignment.Center;
                        FolderNameText.Margin = new Thickness(10, 0, 8, 0);
                        break;

                    case "medium":
                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Medium");
                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
                        ItemsCountText.FontSize = 10;
                        LastModifiedText.FontSize = 10;
                        AttributesText.FontSize = 10;
                        break;

                    case "large":
                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Large");
                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
                        ItemsCountText.FontSize = 12;
                        LastModifiedText.FontSize = 12;
                        AttributesText.FontSize = 12;
                        break;

                    case "extra large":
                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Large");
                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
                        FolderNameText.FontSize = 16;
                        ItemsCountText.FontSize = 14;
                        LastModifiedText.FontSize = 14;
                        AttributesText.FontSize = 12;
                        break;

                    default:
                        Debug.WriteLine($"[TileMyFoldersUc] Неизвестный размер: {Size}");
                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
                        break;
                }

                Debug.WriteLine($"[TileMyFoldersUc] Обновленные параметры: " +
                               $"FolderNameText.FontSize={FolderNameText?.FontSize}, " +
                               $"ItemsCountText.Visibility={ItemsCountText?.Visibility}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в UpdateSize: {ex.Message}");
            }
        }

        private void SetElementsVisibility(
            bool isVisible,
            bool showDetails = true,
            bool showAttributes = true)
        {
            try
            {
                var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                Debug.WriteLine($"[TileMyFoldersUc] SetElementsVisibility: " +
                               $"isVisible={isVisible}, showDetails={showDetails}, showAttributes={showAttributes}");

                if (ItemsCountText != null)
                {
                    ItemsCountText.Visibility = visibility;
                    Debug.WriteLine($"[TileMyFoldersUc] ItemsCountText.Visibility = {ItemsCountText.Visibility}");
                }

                if (LastModifiedText != null)
                {
                    LastModifiedText.Visibility = visibility;
                    Debug.WriteLine($"[TileMyFoldersUc] LastModifiedText.Visibility = {LastModifiedText.Visibility}");
                }

                if (AttributesText != null)
                {
                    AttributesText.Visibility = showAttributes ? visibility : Visibility.Collapsed;
                    Debug.WriteLine($"[TileMyFoldersUc] AttributesText.Visibility = {AttributesText.Visibility}");
                }

                if (!showDetails)
                {
                    if (ItemsCountText != null) ItemsCountText.Visibility = Visibility.Collapsed;
                    if (LastModifiedText != null) LastModifiedText.Visibility = Visibility.Collapsed;
                    if (AttributesText != null) AttributesText.Visibility = Visibility.Collapsed;
                    Debug.WriteLine($"[TileMyFoldersUc] Все детали скрыты (showDetails=false)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в SetElementsVisibility: {ex.Message}");
            }
        }
    }
}