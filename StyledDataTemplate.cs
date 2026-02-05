using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions
{
    public class StyledDataTemplate : DataTemplate
    {
        public Style? Style { get; set; }

        public StyledDataTemplate() : base() { }

        public StyledDataTemplate(Type type) : base(type)
        {
            LoadWithStyle();
        }

        public StyledDataTemplate(Func<object> loadTemplate) : base(loadTemplate)
        {
            LoadWithStyle();
        }

        private Func<object>? _UnstyledTemplate;

        private void LoadWithStyle()
        {
            _UnstyledTemplate = LoadTemplate;
            LoadTemplate = () => LoadWithStyle(_UnstyledTemplate);
        }

        private object LoadWithStyle(Func<object> loadTemplate)
        {
            var item = loadTemplate();

            if (item is StyleableElement se)
            {
                se.Style = Style;
            }

            return item;
        }
    }
}
