using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace ufm
{
    public sealed partial class TreePanelPage : Page
    {
        public TreePanelPage()
        {
            this.InitializeComponent();
            FrameWithinGrid.Navigate(typeof(TreePanelPg01));
        }

        private void ButtonTreePage1_OnClick(object sender, RoutedEventArgs e)
        {
            FrameWithinGrid.Navigate(typeof(TreePanelPg01),
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
        }

        private void ButtonTreePage2_OnClick(object sender, RoutedEventArgs e)
        {
            FrameWithinGrid.Navigate(typeof(TreePanelPg02),
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private void ButtonTreePage3_OnClick(object sender, RoutedEventArgs e)
        {
            FrameWithinGrid.Navigate(typeof(TreePanelPg03),
                new DrillInNavigationTransitionInfo());
        }
    }
}

