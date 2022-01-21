using System.Threading;
using System.Threading.Tasks;

namespace Hollywood
{
	/// <summary>
	/// Interface that an injected system can implement which will be started 
	/// after the system is fully initialized.
	/// </summary>
	public interface IUpdatable
	{
		Task Update(CancellationToken token);
	}
}