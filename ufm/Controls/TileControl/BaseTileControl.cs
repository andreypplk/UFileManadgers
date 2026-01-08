using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using Windows.Storage;

namespace ufm
{
    public partial class BaseTileControl : UserControl
    {
        // Свойство зависимости для размера
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(string),
                typeof(BaseTileControl),
                new PropertyMetadata(null, OnSizeChanged));

        // Свойство зависимости для режима отображения
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(
                nameof(DisplayMode),
                typeof(string),
                typeof(BaseTileControl),
                new PropertyMetadata("Horizontal", OnDisplayModeChanged));

        // Свойства зависимости для размера иконки
        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register(
                nameof(IconWidth),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(0));

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register(
                nameof(IconHeight),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(0));

        // Свойства зависимости для размера шрифта и выбора шрифта
        public new static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(0));

        public new static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(
                nameof(FontFamily),
                typeof(FontFamily),
                typeof(BaseTileControl),
                new PropertyMetadata(null));

        //04 11 2025
        public static readonly DependencyProperty MaxIconHeightProperty =
            DependencyProperty.Register(
                nameof(MaxIconHeight),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(100.0));
        // Свойство Size
        public string Size
        {
            get => (string)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        // Свойство DisplayMode
        public string DisplayMode
        {
            get => (string)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        // Свойство IconWidth
        public int IconWidth
        {
            get => (int)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        // Свойство IconHeight
        public int IconHeight
        {
            get => (int)GetValue(IconHeightProperty);
            set => SetValue(IconHeightProperty, value);
        }

        // Свойство FontSize
        public new int FontSize
        {
            get => (int)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        // Свойство FontFamily
        public new FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        //04 11 2025
        public double MaxIconHeight
        {
            get => (double)GetValue(MaxIconHeightProperty);
            set => SetValue(MaxIconHeightProperty, value);
        }

        // Обработчик изменения свойства Size
        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                control.UpdateSize();
            }
        }

        // Обработчик изменения режима отображения
        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                control.OnDisplayModeChanged();
            }
        }

        protected virtual void OnDisplayModeChanged()
        {
            // Переопределяется в наследниках
        }

        // Метод для обновления размеров и параметров
        public void UpdateSize(string selectedSize)
        {
            Size = selectedSize;

            InvalidateArrange();
            InvalidateMeasure();
            UpdateLayout();
        }

        // Метод для обновления размеров на основе текущих значений
        protected virtual void UpdateSize()
        {
            string targetSize = Size;


            // Если нигде не нашли, используем по умолчанию
            if (string.IsNullOrEmpty(targetSize))
            {
                targetSize = "Medium";
                //Debug.WriteLine("[BaseTileControl] Используется размер по умолчанию: Medium");
            }
            else
            {
                //Debug.WriteLine($"[BaseTileControl] Загружен размер из настроек: {targetSize}");
            }

            //Debug.WriteLine($"[BaseTileControl] Обновление размера: {targetSize}");

            // Получаем размеры из менеджера и ПРЯМОЕ ПРИСВОЕНИЕ в свойства
            (Width, Height, IconWidth, IconHeight, FontSize, var fontFamilyString) = SizeManagerTile.GetSize(targetSize);

            // Устанавливаем FontFamily из строки
            FontFamily = new FontFamily(fontFamilyString);

            //04 11 2025
            if (DisplayMode == "Vertical")
            {
                MaxIconHeight = Height * 0.6; // 60% от высоты элемента
            }
            else
            {
                MaxIconHeight = double.PositiveInfinity; // Без ограничений
            }
            // Принудительно обновляем макет
            InvalidateMeasure();
            InvalidateArrange();
            UpdateLayout();
        }

        // Конструктор
        public BaseTileControl()
        {
            this.DefaultStyleKey = typeof(BaseTileControl);
            UpdateSize(); // Инициализация размеров при создании
        }

        // Переопределение метода применения шаблона
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateSize(); // Гарантируем обновление после применения шаблона
        }
    }
}


