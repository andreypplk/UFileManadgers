using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;

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

        // Реализация абстрактных свойств
        protected override TextBlock GetHorizontalTextBlock() => HorizontalTextBlock;
        protected override TextBlock GetVerticalTextBlock() => VerticalTextBlock;
        protected override TextBlock GetListTextBlock() => ListTextBlock;

        protected override TextBox GetHorizontalEditBox() => HorizontalEditBox;
        protected override TextBox GetVerticalEditBox() => VerticalEditBox;
        protected override TextBox GetListEditBox() => ListEditBox;

        protected override FrameworkElement GetHorizontalLayout() => HorizontalLayout;
        protected override FrameworkElement GetVerticalLayout() => VerticalLayout;
        protected override FrameworkElement GetListLayout() => ListLayout;

        // Переопределяем CanEdit
        public override bool CanEdit => true;

        // Переопределение методов для специфичной логики
        protected override void OnStartEditing()
        {
            // Скрываем дополнительные элементы при редактировании
            if (ItemsCountText != null) ItemsCountText.Visibility = Visibility.Collapsed;
            if (LastModifiedText != null) LastModifiedText.Visibility = Visibility.Collapsed;
            if (AttributesText != null) AttributesText.Visibility = Visibility.Collapsed;
        }

        protected override void OnFinishEditing()
        {
            // Восстанавливаем дополнительные элементы
            UpdateDetailsVisibility();
        }

        protected override void OnCancelChanges()
        {
            // Дополнительная логика отмены изменений (если нужна)
        }

        protected override void OnSaveChanges(string newText)
        {
            // Дополнительная логика сохранения изменений (если нужна)
        }

        // Обработчики событий
        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Автоматическая фокусировка не нужна - она делается в StartEditing
        }

        protected override void OnDisplayModeChanged()
        {
            Debug.WriteLine($"[TileMyFoldersUc] OnDisplayModeChanged: DisplayMode = {DisplayMode}");

            try
            {
                if (HorizontalLayout == null || VerticalLayout == null || ListLayout == null)
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Макеты не инициализированы");
                    return;
                }

                if (DisplayMode == "Vertical")
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка вертикального режима");
                    HorizontalLayout.Visibility = Visibility.Collapsed;
                    VerticalLayout.Visibility = Visibility.Visible;
                    ListLayout.Visibility = Visibility.Collapsed;
                }
                else if (DisplayMode == "List")
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка лист режима");
                    HorizontalLayout.Visibility = Visibility.Collapsed;
                    VerticalLayout.Visibility = Visibility.Collapsed;
                    ListLayout.Visibility = Visibility.Visible;
                }
                else
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка горизонтального режима");
                    HorizontalLayout.Visibility = Visibility.Visible;
                    VerticalLayout.Visibility = Visibility.Collapsed;
                    ListLayout.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в OnDisplayModeChanged: {ex.Message}");
            }
        }

        private void UpdateDetailsVisibility()
        {
            if (IsEditing) return;

            if (DisplayMode == "List")
            {
                SetElementsVisibility(false);
                return;
            }

            switch (Size?.ToLower())
            {
                case "extra small":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Small");
                    SetElementsVisibility(false);
                    break;

                case "small":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Small");
                    SetElementsVisibility(false);
                    if (HorizontalTextBlock != null)
                    {
                        HorizontalTextBlock.VerticalAlignment = VerticalAlignment.Center;
                        HorizontalTextBlock.Margin = new Thickness(10, 0, 8, 0);
                    }
                    break;

                case "medium":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Medium");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (ItemsCountText != null) ItemsCountText.FontSize = 10;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 10;
                    if (AttributesText != null) AttributesText.FontSize = 10;
                    break;

                case "large":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Large");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (ItemsCountText != null) ItemsCountText.FontSize = 12;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 12;
                    if (AttributesText != null) AttributesText.FontSize = 12;
                    break;

                case "extra large":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Large");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (HorizontalTextBlock != null) HorizontalTextBlock.FontSize = 16;
                    if (VerticalTextBlock != null) VerticalTextBlock.FontSize = 16;
                    if (ListTextBlock != null) ListTextBlock.FontSize = 16;
                    if (ItemsCountText != null) ItemsCountText.FontSize = 14;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 14;
                    if (AttributesText != null) AttributesText.FontSize = 12;
                    break;

                default:
                    Debug.WriteLine($"[TileMyFoldersUc] Неизвестный размер: {Size}");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    break;
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

                if (ItemsCountText != null)
                {
                    ItemsCountText.Visibility = showDetails ? visibility : Visibility.Collapsed;
                }

                if (LastModifiedText != null)
                {
                    LastModifiedText.Visibility = showDetails ? visibility : Visibility.Collapsed;
                }

                if (AttributesText != null)
                {
                    AttributesText.Visibility = showAttributes ? visibility : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в SetElementsVisibility: {ex.Message}");
            }
        }

        protected override void UpdateSize()
        {
            Debug.WriteLine($"[TileMyFoldersUc] UpdateSize: Size = {Size}");
            base.UpdateSize();
            UpdateDetailsVisibility();
        }
    }
}