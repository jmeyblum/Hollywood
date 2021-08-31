using System.Threading;
using System.Threading.Tasks;

namespace Hollywood
{
	/// <summary>
	/// Interface that an injected system can implement which will await the Initialize task 
	/// after all its dependencies are initialized.
	/// </summary>
	public interface IInitializable
	{
		Task Initialize(CancellationToken token);
	}
}