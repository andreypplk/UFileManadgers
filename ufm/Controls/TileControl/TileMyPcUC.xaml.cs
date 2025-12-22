using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace ufm
{
    public sealed partial class TileMyPcUC : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;

        public TileMyPcUC()
        {
            this.InitializeComponent();

            // Инициализируем аниматор
            _scaleAnimator = new ScaleAnimator(BorderTileMyPcUC);
        }

        protected override void OnDisplayModeChanged()
        {
            base.OnDisplayModeChanged();

            if (DisplayMode == "Vertical")
            {
                HorizontalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                VerticalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
            else
            {
                HorizontalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                VerticalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}