using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	public interface IProcessor
	{
		void Process();
		Task ProcessAsync(CancellationToken cancellationToken);
	}
}