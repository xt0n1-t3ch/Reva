using System.Threading.Tasks;

namespace Reva.App.Navigation;

public interface IRefreshable
{
    Task RefreshAsync();
}
