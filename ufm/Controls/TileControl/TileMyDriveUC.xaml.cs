
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

        protected override void OnDisplayModeChanged()
        {
            base.OnDisplayModeChanged();

            // Переключение между режимами
            if (DisplayMode == "Vertical")
            {
                HorizontalLayout.Visibility = Visibility.Collapsed;
                VerticalLayout.Visibility = Visibility.Visible;
            }
            else
            {
                HorizontalLayout.Visibility = Visibility.Visible;
                VerticalLayout.Visibility = Visibility.Collapsed;
            }
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();

            // Проверка на null элементов управления
            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
            {
                return;
            }

            // Приводим Size к нижнему регистру для унификации
            switch (Size.ToLower())
            {
                case "tiny":
                    BorderTotalSizeString.Height = 1;
                    BorderTotalSizeString.Width = 1;
                    SetElementVisibility(false, 0, 0);
                    break;
                case "extra small":
                    BorderTotalSizeString.Height = 1;
                    BorderTotalSizeString.Width = 1;
                    SetElementVisibility(false, 0, 0);
                    break;
                case "small":
                    BorderTotalSizeString.Height = 1;
                    BorderTotalSizeString.Width = 1;
                    SetElementVisibility(false, 0, 0);
                    break;
                case "medium":
                    BorderTotalSizeString.Height = 45;
                    BorderTotalSizeString.Width = 40;
                    SetElementVisibility(true, 10, 18);
                    break;
                case "large":
                    BorderTotalSizeString.Height = 75;
                    BorderTotalSizeString.Width = 45;
                    SetElementVisibility(true, 12, 20);
                    break;
                case "extra large":
                    BorderTotalSizeString.Height = 85;
                    BorderTotalSizeString.Width = 50;
                    SetElementVisibility(true, 14, 22);
                    break;
                case "huge":
                    BorderTotalSizeString.Height = 85;
                    BorderTotalSizeString.Width = 50;
                    SetElementVisibility(true, 14, 22);
                    break;
                default:
                    // Обработка неизвестного размера
                    Debug.WriteLine($"Неизвестный размер: {Size}");
                    break;
            }
        }

        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
        {
            // Устанавливаем видимость элементов
            progressBar.Visibility = isVisible.ToVisibility();
            BorderTotalSizeString.Visibility = isVisible.ToVisibility();
            GridUsedSpaceString.Visibility = isVisible.ToVisibility();
            tbFreeSpaceString.Visibility = isVisible.ToVisibility();
            tbUsedSpaceSString.Visibility = isVisible.ToVisibility();

            // Устанавливаем минимальный размер шрифта 1 (вместо 0)
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

            // Поиск индикатора с учетом правильного регистра имени
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
