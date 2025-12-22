using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

            if (FolderNameText == null || ItemsCountText == null || LastModifiedText == null)
                return;

            // Настройка видимости и размеров в зависимости от выбранного размера
            switch (Size.ToLower())
            {
                case "extra small":
                    SetElementsVisibility(false);
                    break;

                case "small":
                    SetElementsVisibility(false);
                    FolderNameText.VerticalAlignment = VerticalAlignment.Center;
                    FolderNameText.Margin = new Thickness(10, 0, 8, 0);
                    break;

                case "medium":
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    ItemsCountText.FontSize = 10;
                    LastModifiedText.FontSize = 10;
                    AttributesText.FontSize = 10;
                    break;

                case "large":
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    ItemsCountText.FontSize = 12;
                    LastModifiedText.FontSize = 12;
                    AttributesText.FontSize = 12;
                    break;

                case "extra large":
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    FolderNameText.FontSize = 16;
                    VerticalFolderNameText.FontSize = 16;
                    ItemsCountText.FontSize = 14;
                    LastModifiedText.FontSize = 14;
                    AttributesText.FontSize = 12;
                    break;
            }
        }

        private void SetElementsVisibility(
            bool isVisible,
            bool showDetails = true,
            bool showAttributes = true)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            ItemsCountText.Visibility = visibility;
            LastModifiedText.Visibility = visibility;
            AttributesText.Visibility = visibility;
            if (!showDetails)
            {
                ItemsCountText.Visibility = Visibility.Collapsed;
                LastModifiedText.Visibility = Visibility.Collapsed;
                AttributesText.Visibility = Visibility.Collapsed;
            }
        }
    }
}