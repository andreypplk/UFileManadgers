//using System.Diagnostics;
//using System.Linq;
//using CommunityToolkit.WinUI;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Windows.System;
//using System.IO;
//using System.Threading.Tasks;
//using System;
//using Core_FileManagement;

//namespace ufm
//{
//    public sealed partial class TileMyDriveUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;

//        public TileMyDriveUc()
//        {
//            this.InitializeComponent();

//            if (BorderTileMyDriveUC != null)
//            {
//                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
//            }
//        }

//        // Реализация абстрактных свойств
//        protected override TextBlock GetHorizontalTextBlock() => HorizontalTextBlock;
//        protected override TextBlock GetVerticalTextBlock() => VerticalTextBlock;
//        protected override TextBlock GetListTextBlock() => ListTextBlock;

//        protected override TextBox GetHorizontalEditBox() => HorizontalEditBox;
//        protected override TextBox GetVerticalEditBox() => VerticalEditBox;
//        protected override TextBox GetListEditBox() => ListEditBox;

//        protected override FrameworkElement GetHorizontalLayout() => HorizontalLayout;
//        protected override FrameworkElement GetVerticalLayout() => VerticalLayout;
//        protected override FrameworkElement GetListLayout() => ListLayout;

//        // Переопределяем CanEdit
//        public override bool CanEdit => true;

//        // Переопределение методов для специфичной логики
//        protected override void OnStartEditing()
//        {
//            // Скрываем дополнительные элементы при редактировании
//            if (progressBar != null) progressBar.Visibility = Visibility.Collapsed;
//            if (BorderTotalSizeString != null) BorderTotalSizeString.Visibility = Visibility.Collapsed;
//            if (GridUsedSpaceString != null) GridUsedSpaceString.Visibility = Visibility.Collapsed;
//            if (tbFreeSpaceString != null) tbFreeSpaceString.Visibility = Visibility.Collapsed;
//            if (tbUsedSpaceSString != null) tbUsedSpaceSString.Visibility = Visibility.Collapsed;
//        }

//        protected override void OnFinishEditing()
//        {
//            // Восстанавливаем дополнительные элементы
//            progressBar.Visibility = Visibility.Visible;
//            BorderTotalSizeString.Visibility = Visibility.Visible;
//            GridUsedSpaceString.Visibility = Visibility.Visible;
//            tbFreeSpaceString.Visibility = Visibility.Visible;
//            tbUsedSpaceSString.Visibility = Visibility.Visible;

//            UpdateSize(); // Обновляем размеры после завершения редактирования
//        }

//        protected override void OnCancelChanges()
//        {
//            // Дополнительная логика отмены изменений (если нужна)
//        }

//        protected override void OnSaveChanges(string newText)
//        {
//            // Дополнительная логика сохранения изменений (если нужна)
//        }

//        // Обработчики событий
//        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
//        {
//            // Автоматическая фокусировка не нужна - она делается в StartEditing
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            base.OnDisplayModeChanged();

//            if (DisplayMode == "Vertical")
//            {
//                HorizontalLayout.Visibility = Visibility.Collapsed;
//                VerticalLayout.Visibility = Visibility.Visible;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }
//            else if (DisplayMode == "List")
//            {
//                HorizontalLayout.Visibility = Visibility.Collapsed;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Visible;
//            }
//            else
//            {
//                HorizontalLayout.Visibility = Visibility.Visible;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }
//        }

//        protected override void UpdateSize()
//        {
//            base.UpdateSize();

//            // Проверка на null элементов управления
//            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
//                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
//            {
//                return;
//            }

//            // Если в режиме редактирования - выходим
//            if (IsEditing)
//            {
//                return;
//            }

//            // Скрываем для List режима
//            if (DisplayMode == "List")
//            {
//                SetElementVisibility(false, 0, 0);
//                return;
//            }

//            // Приводим Size к нижнему регистру для унификации
//            switch (Size.ToLower())
//            {
//                case "tiny":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "extra small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "medium":
//                    BorderTotalSizeString.Height = 45;
//                    BorderTotalSizeString.Width = 40;
//                    SetElementVisibility(true, 10, 18);
//                    break;
//                case "large":
//                    BorderTotalSizeString.Height = 75;
//                    BorderTotalSizeString.Width = 45;
//                    SetElementVisibility(true, 12, 20);
//                    break;
//                case "extra large":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                case "huge":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                default:
//                    // Обработка неизвестного размера
//                    //Debug.WriteLine($"[TileMyDriveUc] Неизвестный размер: {Size}");
//                    break;
//            }
//        }

//        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
//        {
//            progressBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
//            BorderTotalSizeString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
//            GridUsedSpaceString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
//            tbFreeSpaceString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
//            tbUsedSpaceSString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

//            double actualFontSize = isVisible ? fontSize : 1;
//            tbFreeSpaceString.FontSize = actualFontSize;
//            tbUsedSpaceSString.FontSize = actualFontSize;
//            tbTotalSizeString.FontSize = actualFontSize;

//            var indicator = GetProgressBarIndicator();
//            if (indicator != null)
//            {
//                indicator.Height = isVisible ? indHeight : 0;
//            }
//        }

//        private Border GetProgressBarIndicator()
//        {
//            if (progressBar == null) return null;

//            return UIHelper.GetDescendantsOfType<Border>(progressBar)
//                .FirstOrDefault(b => b.Name == "Indicator");
//        }
//    }

//    public static class VisibilityExtensions
//    {
//        public static Visibility ToVisibility(this bool isVisible) =>
//            isVisible ? Visibility.Visible : Visibility.Collapsed;
//    }
//}

using System.Diagnostics;
using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using System.IO;
using System.Threading.Tasks;
using System;
using Core_FileManagement;

namespace ufm
{
    public sealed partial class TileMyDriveUc : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;

        public TileMyDriveUc()
        {
            this.InitializeComponent();

            if (BorderTileMyDriveUC != null)
            {
                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
            }
        }

        protected override TextBlock GetHorizontalTextBlock() => HorizontalTextBlock;
        protected override TextBlock GetVerticalTextBlock() => VerticalTextBlock;
        protected override TextBlock GetListTextBlock() => ListTextBlock;

        protected override TextBox GetHorizontalEditBox() => HorizontalEditBox;
        protected override TextBox GetVerticalEditBox() => VerticalEditBox;
        protected override TextBox GetListEditBox() => ListEditBox;

        protected override FrameworkElement GetHorizontalLayout() => HorizontalLayout;
        protected override FrameworkElement GetVerticalLayout() => VerticalLayout;
        protected override FrameworkElement GetListLayout() => ListLayout;

        public override bool CanEdit => true;

        protected override void OnStartEditing()
        {
            if (progressBar != null) progressBar.Visibility = Visibility.Collapsed;
            if (BorderTotalSizeString != null) BorderTotalSizeString.Visibility = Visibility.Collapsed;
            if (GridUsedSpaceString != null) GridUsedSpaceString.Visibility = Visibility.Collapsed;
            if (tbFreeSpaceString != null) tbFreeSpaceString.Visibility = Visibility.Collapsed;
            if (tbUsedSpaceSString != null) tbUsedSpaceSString.Visibility = Visibility.Collapsed;
        }

        protected override void OnFinishEditing()
        {
            progressBar.Visibility = Visibility.Visible;
            BorderTotalSizeString.Visibility = Visibility.Visible;
            GridUsedSpaceString.Visibility = Visibility.Visible;
            tbFreeSpaceString.Visibility = Visibility.Visible;
            tbUsedSpaceSString.Visibility = Visibility.Visible;

            UpdateSize();
        }

        protected override void OnCancelChanges() { }
        protected override void OnSaveChanges(string newText) { }

        public void EditTextBox_Loaded(object sender, RoutedEventArgs e) { }

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

        protected override void UpdateSize()
        {
            base.UpdateSize();

            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
                return;

            if (IsEditing) return;

            if (DisplayMode == "List")
            {
                SetElementVisibility(false, 0, 0);
                return;
            }

            string sizePart = SizeHelper.ExtractSizePartFromFullKey(Size).ToLower();

            switch (sizePart)
            {
                case "tiny":
                case "extra small":
                    BorderTotalSizeString.Height = 1;
                    BorderTotalSizeString.Width = 1;
                    SetElementVisibility(false, 0, 0);
                    break;
                case "small":
                    BorderTotalSizeString.Height = 35;
                    BorderTotalSizeString.Width = 30;
                    SetElementVisibility(true, 8, 16);
                    break;
                case "below medium":
                    BorderTotalSizeString.Height = 35;
                    BorderTotalSizeString.Width = 30;
                    SetElementVisibility(true, 9, 17);
                    break;

                case "medium":
                    BorderTotalSizeString.Height = 45;
                    BorderTotalSizeString.Width = 40;
                    SetElementVisibility(true, 10, 18);
                    break;

                case "above medium":
                    BorderTotalSizeString.Height = 60;
                    BorderTotalSizeString.Width = 42;
                    SetElementVisibility(true, 11, 19);
                    break;

                case "large":
                    BorderTotalSizeString.Height = 75;
                    BorderTotalSizeString.Width = 45;
                    SetElementVisibility(true, 12, 20);
                    break;

                case "extra large":
                case "huge":
                    BorderTotalSizeString.Height = 85;
                    BorderTotalSizeString.Width = 50;
                    SetElementVisibility(true, 14, 22);
                    break;

                default:
                    break;
            }
        }

        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
        {
            progressBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            BorderTotalSizeString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            GridUsedSpaceString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            tbFreeSpaceString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            tbUsedSpaceSString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            double actualFontSize = isVisible ? fontSize : 1;
            tbFreeSpaceString.FontSize = actualFontSize;
            tbUsedSpaceSString.FontSize = actualFontSize;
            tbTotalSizeString.FontSize = actualFontSize;

            var indicator = GetProgressBarIndicator();
            if (indicator != null)
            {
                indicator.Height = isVisible ? indHeight : 0;
            }
        }

        private Border GetProgressBarIndicator()
        {
            if (progressBar == null) return null;

            return UIHelper.GetDescendantsOfType<Border>(progressBar)
                .FirstOrDefault(b => b.Name == "Indicator");
        }
    }

    public static class VisibilityExtensions
    {
        public static Visibility ToVisibility(this bool isVisible) =>
            isVisible ? Visibility.Visible : Visibility.Collapsed;
    }
}