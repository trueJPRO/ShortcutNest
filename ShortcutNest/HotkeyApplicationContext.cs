using System.Windows.Forms;

namespace ShortcutNest
{
    public class HotkeyApplicationContext : ApplicationContext
    {
        private readonly LauncherPopup _popup;

        public HotkeyApplicationContext()
        {
            _popup = new LauncherPopup();
            _popup.HideOnCloseRequested = false;

            _popup.FormClosed += (_, __) => ExitThread();

            _popup.ShowCentered();
        }
    }
}