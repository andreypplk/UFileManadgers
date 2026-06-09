using System;
using Microsoft.UI.Xaml;
using SettingManager;
using System.Collections.Generic;
using System.Diagnostics;

namespace ufm
{
    public class SplitterManager
    {
        private readonly SettingsManager _settingsManager;

        public SplitterManager(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        // Сохранение размеров для всех конфигураций
        public void SaveAllSplitterSizes(ViewPage viewPage)
        {
            var sizes = new Dictionary<string, double>();

            // Вертикальное разделение
            if (viewPage.VerticalViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.VerticalViewGrid.ColumnDefinitions.Count >= 3)
                {
                    sizes["Vertical_Left"] = viewPage.VerticalViewGrid.ColumnDefinitions[0].ActualWidth;
                    sizes["Vertical_Right"] = viewPage.VerticalViewGrid.ColumnDefinitions[2].ActualWidth;
                }
            }

            // Горизонтальное разделение
            if (viewPage.HorizontalViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.HorizontalViewGrid.RowDefinitions.Count >= 3)
                {
                    sizes["Horizontal_Top"] = viewPage.HorizontalViewGrid.RowDefinitions[0].ActualHeight;
                    sizes["Horizontal_Bottom"] = viewPage.HorizontalViewGrid.RowDefinitions[2].ActualHeight;
                }
            }

            // Тройное вертикальное
            if (viewPage.TripleVerticalViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.TripleVerticalViewGrid.ColumnDefinitions.Count >= 5)
                {
                    sizes["TripleVertical_Left"] = viewPage.TripleVerticalViewGrid.ColumnDefinitions[0].ActualWidth;
                    sizes["TripleVertical_Center"] = viewPage.TripleVerticalViewGrid.ColumnDefinitions[2].ActualWidth;
                    sizes["TripleVertical_Right"] = viewPage.TripleVerticalViewGrid.ColumnDefinitions[4].ActualWidth;
                }
            }

            // Тройное горизонтальное
            if (viewPage.TripleHorizontalViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.TripleHorizontalViewGrid.RowDefinitions.Count >= 5)
                {
                    sizes["TripleHorizontal_Top"] = viewPage.TripleHorizontalViewGrid.RowDefinitions[0].ActualHeight;
                    sizes["TripleHorizontal_Center"] = viewPage.TripleHorizontalViewGrid.RowDefinitions[2].ActualHeight;
                    sizes["TripleHorizontal_Bottom"] = viewPage.TripleHorizontalViewGrid.RowDefinitions[4].ActualHeight;
                }
            }

            // TripleTopBottom (2 сверху, 1 снизу)
            if (viewPage.TripleTopBottomViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.TripleTopBottomViewGrid.ColumnDefinitions.Count >= 3)
                {
                    sizes["TripleTopBottom_Left"] = viewPage.TripleTopBottomViewGrid.ColumnDefinitions[0].ActualWidth;
                    sizes["TripleTopBottom_Right"] = viewPage.TripleTopBottomViewGrid.ColumnDefinitions[2].ActualWidth;
                }
                if (viewPage.TripleTopBottomViewGrid.RowDefinitions.Count >= 3)
                {
                    sizes["TripleTopBottom_Top"] = viewPage.TripleTopBottomViewGrid.RowDefinitions[0].ActualHeight;
                    sizes["TripleTopBottom_Bottom"] = viewPage.TripleTopBottomViewGrid.RowDefinitions[2].ActualHeight;
                }
            }

            // TripleBottomTop (1 сверху, 2 снизу)
            if (viewPage.TripleBottomTopViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.TripleBottomTopViewGrid.ColumnDefinitions.Count >= 3)
                {
                    sizes["TripleBottomTop_Left"] = viewPage.TripleBottomTopViewGrid.ColumnDefinitions[0].ActualWidth;
                    sizes["TripleBottomTop_Right"] = viewPage.TripleBottomTopViewGrid.ColumnDefinitions[2].ActualWidth;
                }
                if (viewPage.TripleBottomTopViewGrid.RowDefinitions.Count >= 3)
                {
                    sizes["TripleBottomTop_Top"] = viewPage.TripleBottomTopViewGrid.RowDefinitions[0].ActualHeight;
                    sizes["TripleBottomTop_Bottom"] = viewPage.TripleBottomTopViewGrid.RowDefinitions[2].ActualHeight;
                }
            }

            // TripleLeftRight (1 слева, 2 справа)
            if (viewPage.TripleLeftRightViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.TripleLeftRightViewGrid.ColumnDefinitions.Count >= 3)
                {
                    sizes["TripleLeftRight_Left"] = viewPage.TripleLeftRightViewGrid.ColumnDefinitions[0].ActualWidth;
                }
                if (viewPage.TripleLeftRightViewGrid.RowDefinitions.Count >= 3)
                {
                    sizes["TripleLeftRight_Top"] = viewPage.TripleLeftRightViewGrid.RowDefinitions[0].ActualHeight;
                    sizes["TripleLeftRight_Bottom"] = viewPage.TripleLeftRightViewGrid.RowDefinitions[2].ActualHeight;
                }
            }

            // TripleRightLeft (2 слева, 1 справа)
            if (viewPage.TripleRightLeftViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.TripleRightLeftViewGrid.ColumnDefinitions.Count >= 3)
                {
                    sizes["TripleRightLeft_Right"] = viewPage.TripleRightLeftViewGrid.ColumnDefinitions[2].ActualWidth;
                }
                if (viewPage.TripleRightLeftViewGrid.RowDefinitions.Count >= 3)
                {
                    sizes["TripleRightLeft_Top"] = viewPage.TripleRightLeftViewGrid.RowDefinitions[0].ActualHeight;
                    sizes["TripleRightLeft_Bottom"] = viewPage.TripleRightLeftViewGrid.RowDefinitions[2].ActualHeight;
                }
            }

            // Quad (2x2)
            if (viewPage.QuadViewGrid.Visibility == Visibility.Visible)
            {
                if (viewPage.QuadViewGrid.ColumnDefinitions.Count >= 3)
                {
                    sizes["Quad_Left"] = viewPage.QuadViewGrid.ColumnDefinitions[0].ActualWidth;
                    sizes["Quad_Right"] = viewPage.QuadViewGrid.ColumnDefinitions[2].ActualWidth;
                }
                if (viewPage.QuadViewGrid.RowDefinitions.Count >= 3)
                {
                    sizes["Quad_Top"] = viewPage.QuadViewGrid.RowDefinitions[0].ActualHeight;
                    sizes["Quad_Bottom"] = viewPage.QuadViewGrid.RowDefinitions[2].ActualHeight;
                }
            }

            // Сохраняем в настройки
            foreach (var size in sizes)
            {
                _settingsManager.SaveSetting($"Splitter_{size.Key}", size.Value);
            }
        }

        // Загрузка размеров
        public Dictionary<string, double> LoadAllSplitterSizes()
        {
            var sizes = new Dictionary<string, double>();
            var keys = new[]
            {
                "Vertical_Left", "Vertical_Right",
                "Horizontal_Top", "Horizontal_Bottom",
                "TripleVertical_Left", "TripleVertical_Center", "TripleVertical_Right",
                "TripleHorizontal_Top", "TripleHorizontal_Center", "TripleHorizontal_Bottom",
                "TripleTopBottom_Left", "TripleTopBottom_Right", "TripleTopBottom_Top", "TripleTopBottom_Bottom",
                "TripleBottomTop_Left", "TripleBottomTop_Right", "TripleBottomTop_Top", "TripleBottomTop_Bottom",
                "TripleLeftRight_Left", "TripleLeftRight_Top", "TripleLeftRight_Bottom",
                "TripleRightLeft_Right", "TripleRightLeft_Top", "TripleRightLeft_Bottom",
                "Quad_Left", "Quad_Right", "Quad_Top", "Quad_Bottom"
            };

            foreach (var key in keys)
            {
                sizes[key] = _settingsManager.GetSetting<double>($"Splitter_{key}", 0);
            }

            return sizes;
        }

        // Применение размеров к текущей конфигурации
        public void ApplySplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            try
            {
                // Применяем размеры ко всем видимым Grid'ам
                if (viewPage.VerticalViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyVerticalSplitterSizes(viewPage, sizes);
                }

                if (viewPage.HorizontalViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyHorizontalSplitterSizes(viewPage, sizes);
                }

                if (viewPage.TripleVerticalViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyTripleVerticalSplitterSizes(viewPage, sizes);
                }

                if (viewPage.TripleHorizontalViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyTripleHorizontalSplitterSizes(viewPage, sizes);
                }

                if (viewPage.TripleTopBottomViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyTripleTopBottomSplitterSizes(viewPage, sizes);
                }

                if (viewPage.TripleBottomTopViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyTripleBottomTopSplitterSizes(viewPage, sizes);
                }

                if (viewPage.TripleLeftRightViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyTripleLeftRightSplitterSizes(viewPage, sizes);
                }

                if (viewPage.TripleRightLeftViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyTripleRightLeftSplitterSizes(viewPage, sizes);
                }

                if (viewPage.QuadViewGrid.Visibility == Visibility.Visible)
                {
                    ApplyQuadSplitterSizes(viewPage, sizes);
                }

                // Принудительное обновление layout
                viewPage.InvalidateArrange();
                viewPage.UpdateLayout();
            }
            catch 
            {
            }
        }

        public void ClearAllSavedSizes()
        {
            var keys = new[]
            {
                "Vertical_Left", "Vertical_Right",
                "Horizontal_Top", "Horizontal_Bottom",
                "TripleVertical_Left", "TripleVertical_Center", "TripleVertical_Right",
                "TripleHorizontal_Top", "TripleHorizontal_Center", "TripleHorizontal_Bottom",
                "TripleTopBottom_Left", "TripleTopBottom_Right", "TripleTopBottom_Top", "TripleTopBottom_Bottom",
                "TripleBottomTop_Left", "TripleBottomTop_Right", "TripleBottomTop_Top", "TripleBottomTop_Bottom",
                "TripleLeftRight_Left", "TripleLeftRight_Top", "TripleLeftRight_Bottom",
                "TripleRightLeft_Right", "TripleRightLeft_Top", "TripleRightLeft_Bottom",
                "Quad_Left", "Quad_Right", "Quad_Top", "Quad_Bottom"
            };

            foreach (var key in keys)
            {
                _settingsManager.SaveSetting($"Splitter_{key}", 0.0);
            }
        }

        private void ApplyVerticalSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.VerticalViewGrid.ColumnDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("Vertical_Left", out var leftSize) && leftSize > 0)
                {
                    viewPage.VerticalViewGrid.ColumnDefinitions[0].Width = new GridLength(leftSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("Vertical_Right", out var rightSize) && rightSize > 0)
                {
                    viewPage.VerticalViewGrid.ColumnDefinitions[2].Width = new GridLength(rightSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyHorizontalSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.HorizontalViewGrid.RowDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("Horizontal_Top", out var topSize) && topSize > 0)
                {
                    viewPage.HorizontalViewGrid.RowDefinitions[0].Height = new GridLength(topSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("Horizontal_Bottom", out var bottomSize) && bottomSize > 0)
                {
                    viewPage.HorizontalViewGrid.RowDefinitions[2].Height = new GridLength(bottomSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyTripleVerticalSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.TripleVerticalViewGrid.ColumnDefinitions.Count >= 5)
            {
                if (sizes.TryGetValue("TripleVertical_Left", out var leftSize) && leftSize > 0)
                {
                    viewPage.TripleVerticalViewGrid.ColumnDefinitions[0].Width = new GridLength(leftSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleVertical_Center", out var centerSize) && centerSize > 0)
                {
                    viewPage.TripleVerticalViewGrid.ColumnDefinitions[2].Width = new GridLength(centerSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleVertical_Right", out var rightSize) && rightSize > 0)
                {
                    viewPage.TripleVerticalViewGrid.ColumnDefinitions[4].Width = new GridLength(rightSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyTripleHorizontalSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.TripleHorizontalViewGrid.RowDefinitions.Count >= 5)
            {
                if (sizes.TryGetValue("TripleHorizontal_Top", out var topSize) && topSize > 0)
                {
                    viewPage.TripleHorizontalViewGrid.RowDefinitions[0].Height = new GridLength(topSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleHorizontal_Center", out var centerSize) && centerSize > 0)
                {
                    viewPage.TripleHorizontalViewGrid.RowDefinitions[2].Height = new GridLength(centerSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleHorizontal_Bottom", out var bottomSize) && bottomSize > 0)
                {
                    viewPage.TripleHorizontalViewGrid.RowDefinitions[4].Height = new GridLength(bottomSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyTripleTopBottomSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.TripleTopBottomViewGrid.ColumnDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleTopBottom_Left", out var leftSize) && leftSize > 0)
                {
                    viewPage.TripleTopBottomViewGrid.ColumnDefinitions[0].Width = new GridLength(leftSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleTopBottom_Right", out var rightSize) && rightSize > 0)
                {
                    viewPage.TripleTopBottomViewGrid.ColumnDefinitions[2].Width = new GridLength(rightSize, GridUnitType.Pixel);
                }
            }
            if (viewPage.TripleTopBottomViewGrid.RowDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleTopBottom_Top", out var topSize) && topSize > 0)
                {
                    viewPage.TripleTopBottomViewGrid.RowDefinitions[0].Height = new GridLength(topSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleTopBottom_Bottom", out var bottomSize) && bottomSize > 0)
                {
                    viewPage.TripleTopBottomViewGrid.RowDefinitions[2].Height = new GridLength(bottomSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyTripleBottomTopSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.TripleBottomTopViewGrid.ColumnDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleBottomTop_Left", out var leftSize) && leftSize > 0)
                {
                    viewPage.TripleBottomTopViewGrid.ColumnDefinitions[0].Width = new GridLength(leftSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleBottomTop_Right", out var rightSize) && rightSize > 0)
                {
                    viewPage.TripleBottomTopViewGrid.ColumnDefinitions[2].Width = new GridLength(rightSize, GridUnitType.Pixel);
                }
            }
            if (viewPage.TripleBottomTopViewGrid.RowDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleBottomTop_Top", out var topSize) && topSize > 0)
                {
                    viewPage.TripleBottomTopViewGrid.RowDefinitions[0].Height = new GridLength(topSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleBottomTop_Bottom", out var bottomSize) && bottomSize > 0)
                {
                    viewPage.TripleBottomTopViewGrid.RowDefinitions[2].Height = new GridLength(bottomSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyTripleLeftRightSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.TripleLeftRightViewGrid.ColumnDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleLeftRight_Left", out var leftSize) && leftSize > 0)
                {
                    viewPage.TripleLeftRightViewGrid.ColumnDefinitions[0].Width = new GridLength(leftSize, GridUnitType.Pixel);
                }
            }
            if (viewPage.TripleLeftRightViewGrid.RowDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleLeftRight_Top", out var topSize) && topSize > 0)
                {
                    viewPage.TripleLeftRightViewGrid.RowDefinitions[0].Height = new GridLength(topSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleLeftRight_Bottom", out var bottomSize) && bottomSize > 0)
                {
                    viewPage.TripleLeftRightViewGrid.RowDefinitions[2].Height = new GridLength(bottomSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyTripleRightLeftSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.TripleRightLeftViewGrid.ColumnDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleRightLeft_Right", out var rightSize) && rightSize > 0)
                {
                    viewPage.TripleRightLeftViewGrid.ColumnDefinitions[2].Width = new GridLength(rightSize, GridUnitType.Pixel);
                }
            }
            if (viewPage.TripleRightLeftViewGrid.RowDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("TripleRightLeft_Top", out var topSize) && topSize > 0)
                {
                    viewPage.TripleRightLeftViewGrid.RowDefinitions[0].Height = new GridLength(topSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("TripleRightLeft_Bottom", out var bottomSize) && bottomSize > 0)
                {
                    viewPage.TripleRightLeftViewGrid.RowDefinitions[2].Height = new GridLength(bottomSize, GridUnitType.Pixel);
                }
            }
        }

        private void ApplyQuadSplitterSizes(ViewPage viewPage, Dictionary<string, double> sizes)
        {
            if (viewPage.QuadViewGrid.ColumnDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("Quad_Left", out var leftSize) && leftSize > 0)
                {
                    viewPage.QuadViewGrid.ColumnDefinitions[0].Width = new GridLength(leftSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("Quad_Right", out var rightSize) && rightSize > 0)
                {
                    viewPage.QuadViewGrid.ColumnDefinitions[2].Width = new GridLength(rightSize, GridUnitType.Pixel);
                }
            }
            if (viewPage.QuadViewGrid.RowDefinitions.Count >= 3)
            {
                if (sizes.TryGetValue("Quad_Top", out var topSize) && topSize > 0)
                {
                    viewPage.QuadViewGrid.RowDefinitions[0].Height = new GridLength(topSize, GridUnitType.Pixel);
                }
                if (sizes.TryGetValue("Quad_Bottom", out var bottomSize) && bottomSize > 0)
                {
                    viewPage.QuadViewGrid.RowDefinitions[2].Height = new GridLength(bottomSize, GridUnitType.Pixel);
                }
            }
        }
    }
}