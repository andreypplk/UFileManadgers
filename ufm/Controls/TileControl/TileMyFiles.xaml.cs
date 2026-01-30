
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
    public sealed partial class TileMyFiles : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;

        public TileMyFiles()
        {
            this.InitializeComponent();

            if (BorderTileFolders != null)
            {
                _scaleAnimator = new ScaleAnimator(BorderTileFolders);
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
            base.OnDisplayModeChanged();

            if (DisplayMode == "Vertical")
            {
                HorizontalLayout.Visibility = Visibility.Collapsed;
                VerticalLayout.Visibility = Visibility.Visible;
                ListLayout.Visibility = Visibility.Collapsed;
            }
            else if (DisplayMode == "List")
            {
                HorizontalLayout.Visibility = Visibility.Collapsed;
                VerticalLayout.Visibility = Visibility.Collapsed;
                ListLayout.Visibility = Visibility.Visible;
            }
            else
            {
                HorizontalLayout.Visibility = Visibility.Visible;
                VerticalLayout.Visibility = Visibility.Collapsed;
                ListLayout.Visibility = Visibility.Collapsed;
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
                    SetElementsVisibility(false);
                    break;

                case "small":
                    SetElementsVisibility(false);
                    break;

                case "medium":
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (ItemsCountText != null) ItemsCountText.FontSize = 10;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 10;
                    if (AttributesText != null) AttributesText.FontSize = 10;
                    break;

                case "large":
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (ItemsCountText != null) ItemsCountText.FontSize = 12;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 12;
                    if (AttributesText != null) AttributesText.FontSize = 12;
                    break;

                case "extra large":
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (HorizontalTextBlock != null) HorizontalTextBlock.FontSize = 16;
                    if (VerticalTextBlock != null) VerticalTextBlock.FontSize = 16;
                    if (ListTextBlock != null) ListTextBlock.FontSize = 16;
                    if (ItemsCountText != null) ItemsCountText.FontSize = 14;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 14;
                    if (AttributesText != null) AttributesText.FontSize = 12;
                    break;

                default:
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    break;
            }
        }

        private void SetElementsVisibility(
            bool isVisible,
            bool showDetails = true,
            bool showAttributes = true)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            if (ItemsCountText != null)
                ItemsCountText.Visibility = showDetails ? visibility : Visibility.Collapsed;

            if (LastModifiedText != null)
                LastModifiedText.Visibility = showDetails ? visibility : Visibility.Collapsed;

            if (AttributesText != null)
                AttributesText.Visibility = showAttributes ? visibility : Visibility.Collapsed;
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            UpdateDetailsVisibility();
        }
    }
}