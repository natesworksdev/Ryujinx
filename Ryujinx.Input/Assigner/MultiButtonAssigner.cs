using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Input.Assigner
{
    /// <summary>
    /// Assigner for buttons from multiple input devices.
    /// </summary>
    public class MultiButtonAssigner : IButtonAssigner
    {
        private readonly IEnumerable<IButtonAssigner> _assigners;

        public MultiButtonAssigner(IEnumerable<IButtonAssigner> assigners)
        {
            _assigners = assigners;
        }

        public void Initialize()
        {
            foreach (IButtonAssigner assigner in _assigners)
            {
                assigner.Initialize();
            }
        }

        public void ReadInput()
        {
            foreach (IButtonAssigner assigner in _assigners)
            {
                assigner.ReadInput();
            }
        }

        public bool HasAnyButtonPressed()
        {
            return _assigners.Any(x => x.HasAnyButtonPressed());
        }

        public bool ShouldCancel()
        {
            return _assigners.All(x => x.ShouldCancel());
        }

        public string GetPressedButton()
        {
            foreach (IButtonAssigner assigner in _assigners)
            {
                string pressedButton = assigner.GetPressedButton();

                if (!string.IsNullOrEmpty(pressedButton))
                {
                    return pressedButton;
                }
            }

            return "";
        }

        public IEnumerable<PressedButton> GetPressedButtons()
        {
            foreach (IButtonAssigner assigner in _assigners)
            {
                if (assigner.HasAnyButtonPressed())
                {
                    return assigner.GetPressedButtons();
                }
            }

            return Enumerable.Empty<PressedButton>();
        }
    }
}