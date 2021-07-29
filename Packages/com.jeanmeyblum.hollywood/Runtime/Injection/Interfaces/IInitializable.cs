using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Runtime
{
    public interface IInitializable 
    {
        Task Initialize(CancellationToken token);
    }
}