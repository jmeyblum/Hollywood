using System;
using System.Threading.Tasks;

namespace Hollywood.Example
{
	public class LoginController
	{
		public async Task Login()
		{
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
	}
}