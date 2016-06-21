using System;
using System.Drawing;
using System.Security.Principal;

namespace Wox.Plugin.Choco
{
    static class Helper
    {
        private static bool? isElevated;

        public static bool IsElevated()
        {
            if (isElevated == null)
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return (bool)isElevated;
        }

        public static Icon ElevateIcon() => SystemIcons.Shield;
    }
}
