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
            this.InitializeComponent();

            if (BorderTileFolders != null)
            {
                _scaleAnimator = new ScaleAnimator(BorderTileFolders);
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
            if (ItemsCountText != null) ItemsCountText.Visibility = Visibility.Collapsed;
            if (LastModifiedText != null) LastModifiedText.Visibility = Visibility.Collapsed;
            if (AttributesText != null) AttributesText.Visibility = Visibility.Collapsed;
        }

        protected override void OnFinishEditing()
        {
            UpdateDetailsVisibility();
        }

        protected override void OnCancelChanges()
        {
        }

        protected override void OnSaveChanges(string newText)
        {
        }

        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
        {
        }

        protected override void OnDisplayModeChanged()
        {
            try
            {
                if (HorizontalLayout == null || VerticalLayout == null || ListLayout == null)
                    return;

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
            catch 
            {
                //Debug.WriteLine($"[TileMyFoldersUc] Œ¯Ë·Í‡ ‚ OnDisplayModeChanged: {ex.Message}");
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
                    if (HorizontalTextBlock != null)
                    {
                        HorizontalTextBlock.VerticalAlignment = VerticalAlignment.Center;
                        HorizontalTextBlock.Margin = new Thickness(10, 0, 8, 0);
                    }
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
            catch  
            {
                //Debug.WriteLine($"[TileMyFoldersUc] Œ¯Ë·Í‡ ‚ SetElementsVisibility: {ex.Message}");
            }
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            UpdateDetailsVisibility();
        }
    }
}