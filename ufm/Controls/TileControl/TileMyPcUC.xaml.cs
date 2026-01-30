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
    public sealed partial class TileMyPcUC : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;

        public TileMyPcUC()
        {
            this.InitializeComponent();

            // Инициализируем аниматор
            _scaleAnimator = new ScaleAnimator(BorderTileMyPcUC);
        }

        // Реализация абстрактных свойств
        protected override TextBlock GetHorizontalTextBlock() => HorizontalText;
        protected override TextBlock GetVerticalTextBlock() => VerticalText;
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
            // Не нужно скрывать дополнительные элементы, так как их нет
        }

        protected override void OnFinishEditing()
        {
            // Не нужно восстанавливать дополнительные элементы, так как их нет
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
    }
}