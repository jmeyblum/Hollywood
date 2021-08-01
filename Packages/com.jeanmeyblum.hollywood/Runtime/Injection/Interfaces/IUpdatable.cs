using System.Threading;
using System.Threading.Tasks;

namespace Hollywood.Runtime
{
	public interface IUpdatable
	{
		Task Update(CancellationToken token);
	}
}