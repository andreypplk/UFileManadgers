using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using Microsoft.UI.Xaml.Input;

namespace ufm
{
    public class ScaleAnimator
    {
        private readonly Visual _visual;

        public ScaleAnimator(FrameworkElement element)
        {
            _visual = ElementCompositionPreview.GetElementVisual(element);
            element.PointerEntered += Element_PointerEntered;
            element.PointerExited += Element_PointerExited;

            // Подписываемся на изменение состояния анимации
            AnimationManager.Instance.AnimationStateChanged += OnAnimationStateChanged;
        }

        private void OnAnimationStateChanged(object sender, EventArgs e)
        {
            if (!AnimationManager.Instance.IsAnimationEnabled)
            {
                // Если анимация выключена, сбрасываем масштаб элемента
                StartScaleAnimation(1.0f);
            }
        }

        private void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (AnimationManager.Instance.IsAnimationEnabled)
            {
                StartScaleAnimation(1.1f);
            }
        }

        private void Element_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (AnimationManager.Instance.IsAnimationEnabled)
            {
                StartScaleAnimation(1.0f);
            }
        }

        private void StartScaleAnimation(float scale)
        {
            var compositor = _visual.Compositor;
            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(200);
            scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scale));
            _visual.StartAnimation("Scale", scaleAnimation);
        }
        public static void AnimateScale(FrameworkElement element, float scale)
        {
            if (element == null) return;

            var visual = ElementCompositionPreview.GetElementVisual(element);
            if (visual == null) return;

            var compositor = visual.Compositor;
            var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
            scaleAnimation.Duration = TimeSpan.FromMilliseconds(200);
            scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scale));

            // Центр масштабирования — середина элемента
            visual.CenterPoint = new Vector3((float)element.ActualWidth / 2, (float)element.ActualHeight / 2, 0);

            visual.StartAnimation("Scale", scaleAnimation);
        }
        //ПРУЖИНИСТОЕ РАБОТАЕТ
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(600);

        //    // Spring easing - очень заметно
        //    var springEasing = compositor.CreateSpringVector3Animation();
        //    springEasing.FinalValue = new Vector3(scale);
        //    springEasing.DampingRatio = 0.5f; // Меньше = больше "пружинности"
        //    springEasing.Period = TimeSpan.FromMilliseconds(100);

        //    _visual.StartAnimation("Scale", springEasing);
        //}

        //Многошаговая с превышением на любителя
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(500);

        //    if (scale > 1.0f)
        //    {
        //        // Явное превышение и возврат
        //        scaleAnimation.InsertKeyFrame(0.3f, new Vector3(scale * 1.3f)); // Сильно увеличился
        //        scaleAnimation.InsertKeyFrame(0.7f, new Vector3(scale * 0.9f)); // Чуть уменьшился
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scale));        // Вернулся к норме
        //    }
        //    else
        //    {
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scale));
        //    }

        //    _visual.StartAnimation("Scale", scaleAnimation);
        //}

        // Анимация с вращением и прозрачностью Типа смешная 
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;

        //    // Масштаб
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(400);
        //    scaleAnimation.InsertKeyFrame(1.0f, new Vector3(scale));

        //    // Вращение (в градусах)
        //    var rotationAnimation = compositor.CreateScalarKeyFrameAnimation();
        //    rotationAnimation.Duration = TimeSpan.FromMilliseconds(400);

        //    if (scale > 1.0f)
        //    {
        //        rotationAnimation.InsertKeyFrame(0.5f, 8.0f);  // Поворот в середине анимации
        //        rotationAnimation.InsertKeyFrame(1.0f, 0.0f); // Возврат к 0
        //    }

        //    // Запуск обеих анимаций
        //    _visual.StartAnimation("Scale", scaleAnimation);
        //    _visual.StartAnimation("RotationAngleInDegrees", rotationAnimation);
        //}

        // Эластичная анимация
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;

        //    // Используем SpringAnimation для эластичного эффекта
        //    var springAnimation = compositor.CreateSpringVector3Animation();
        //    springAnimation.FinalValue = new Vector3(scale);
        //    springAnimation.DampingRatio = 0.3f;    // Сильная эластичность (0.0-1.0)
        //    springAnimation.Period = TimeSpan.FromMilliseconds(150); // Частота колебаний

        //    _visual.StartAnimation("Scale", springAnimation);
        //}

        //Комбинированная анимация с цветом
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;

        //    // Масштаб
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(500);

        //    if (scale > 1.0f)
        //    {
        //        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.0f));
        //        scaleAnimation.InsertKeyFrame(0.7f, new Vector3(1.15f)); // Пиковое значение
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.08f)); // Финальное значение
        //    }
        //    else
        //    {
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f));
        //    }

        //    _visual.StartAnimation("Scale", scaleAnimation);
        //}

        //Быстрая пульсация
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(300);
        //    scaleAnimation.IterationCount = 1;

        //    if (scale > 1.0f)
        //    {
        //        // Двойная пульсация
        //        scaleAnimation.InsertKeyFrame(0.25f, new Vector3(1.15f));
        //        scaleAnimation.InsertKeyFrame(0.5f, new Vector3(1.05f));
        //        scaleAnimation.InsertKeyFrame(0.75f, new Vector3(1.12f));
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.08f));
        //    }
        //    else
        //    {
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f));
        //    }

        //    _visual.StartAnimation("Scale", scaleAnimation);
        //}

        //Волнообразная анимация
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(700);

        //    if (scale > 1.0f)
        //    {
        //        // Волнообразное движение
        //        scaleAnimation.InsertKeyFrame(0.2f, new Vector3(1.06f));
        //        scaleAnimation.InsertKeyFrame(0.4f, new Vector3(1.12f));
        //        scaleAnimation.InsertKeyFrame(0.6f, new Vector3(1.08f));
        //        scaleAnimation.InsertKeyFrame(0.8f, new Vector3(1.14f));
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.1f));
        //    }
        //    else
        //    {
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f));
        //    }

        //    _visual.StartAnimation("Scale", scaleAnimation);
        //}

        //Медленная плавающая анимация
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(1000); // Дольше

        //    if (scale > 1.0f)
        //    {
        //        // Плавное "дыхание"
        //        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.0f));
        //        scaleAnimation.InsertKeyFrame(0.3f, new Vector3(1.07f));
        //        scaleAnimation.InsertKeyFrame(0.7f, new Vector3(1.03f));
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.05f));
        //    }
        //    else
        //    {
        //        // Медленный возврат
        //        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.05f));
        //        scaleAnimation.InsertKeyFrame(0.5f, new Vector3(1.02f));
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f));
        //    }

        //    _visual.StartAnimation("Scale", scaleAnimation);
        //}

        // Резкая быстрая анимация
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(150); // Очень быстро

        //    if (scale > 1.0f)
        //    {
        //        // Мгновенное увеличение с небольшим отскоком
        //        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.0f));
        //        scaleAnimation.InsertKeyFrame(0.1f, new Vector3(1.2f)); // Быстрый скачок
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.1f)); // Быстрый возврат
        //    }
        //    else
        //    {
        //        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.1f));
        //        scaleAnimation.InsertKeyFrame(0.3f, new Vector3(0.9f)); // Проскакивание
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f));
        //    }

        //    _visual.StartAnimation("Scale", scaleAnimation);
        //}

        // Ступенчатая анимация
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateStepEasingFunction();
        //    scaleAnimation.StepCount = 3; // 3 шага

        //    var keyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    keyFrameAnimation.Duration = TimeSpan.FromMilliseconds(400);

        //    if (scale > 1.0f)
        //    {
        //        keyFrameAnimation.InsertKeyFrame(1.0f, new Vector3(1.1f), scaleAnimation);
        //    }
        //    else
        //    {
        //        keyFrameAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f), scaleAnimation);
        //    }

        //    _visual.StartAnimation("Scale", keyFrameAnimation);
        //}

        //Анимация с разными осями как желе прикольное
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;
        //    var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //    scaleAnimation.Duration = TimeSpan.FromMilliseconds(500);

        //    if (scale > 1.0f)
        //    {
        //        // Разный масштаб по осям (эффект "растяжения")
        //        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f));
        //        scaleAnimation.InsertKeyFrame(0.3f, new Vector3(1.15f, 0.9f, 1.0f));  // Шире, уже
        //        scaleAnimation.InsertKeyFrame(0.7f, new Vector3(0.95f, 1.15f, 1.0f)); // Уже, выше
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.1f, 1.1f, 1.0f));   // Равномерно
        //    }
        //    else
        //    {
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f));
        //    }

        //    _visual.StartAnimation("Scale", scaleAnimation);
        //}

        // Эффект "пульсации" тоже гуд
        //private void StartScaleAnimation(float scale)
        //{
        //    var compositor = _visual.Compositor;

        //    if (scale > 1.0f)
        //    {
        //        // Бесконечная пульсация пока курсор на элементе
        //        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        //        scaleAnimation.Duration = TimeSpan.FromMilliseconds(800);
        //        scaleAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

        //        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.08f));
        //        scaleAnimation.InsertKeyFrame(0.5f, new Vector3(1.12f));
        //        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.08f));

        //        _visual.StartAnimation("Scale", scaleAnimation);
        //    }
        //    else
        //    {
        //        // Останавливаем анимацию и возвращаем масштаб
        //        _visual.StopAnimation("Scale");
        //        _visual.Scale = new Vector3(1.0f);
        //    }
        //}
    }

}
