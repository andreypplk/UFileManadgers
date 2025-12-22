using System;

namespace ufm
{
    public class AnimationManager
    {
        private static AnimationManager _instance;
        private bool _isAnimationEnabled;

        private AnimationManager()
        {
            _isAnimationEnabled = true; // По умолчанию анимация включена
        }

        public static AnimationManager Instance => _instance ??= new AnimationManager();

        public bool IsAnimationEnabled
        {
            get => _isAnimationEnabled;
            set
            {
                if (_isAnimationEnabled != value)
                {
                    _isAnimationEnabled = value;
                    NotifyAnimationStateChanged();
                }
            }
        }

        public event EventHandler AnimationStateChanged;

        private void NotifyAnimationStateChanged()
        {
            AnimationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }


}
