using Reva.Core.Settings;

namespace Reva.Web;

// The single source of truth for product naming and the cockpit tagline. The product name is
// user-customizable in Settings, so it reads from RuntimeSettings; change it once and every
// page title, header, and rail label follows.
public static class Brand
{
    public static string Product => RuntimeSettings.Current.ProductName;
    public static string Cockpit => $"{Product} Cockpit";
    public const string Tagline = "Transforming reinsurance data into trusted intelligence";
}
