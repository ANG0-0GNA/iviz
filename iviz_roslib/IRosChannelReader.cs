using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Iviz.Msgs;

namespace Iviz.Roslib
{
    public interface IRosChannelReader
    {
        Task<bool> WaitToReadAsync(CancellationToken token);
        IMessage Read(CancellationToken token);
        Task<IMessage> ReadAsync(CancellationToken token);
        public Task StartAsync(IRosClient client, string topic, bool requestNoDelay = false);
        public void Start(IRosClient client, string topic, bool requestNoDelay = false);
        public IEnumerable<IMessage> ReadAll(CancellationToken externalToken);
#if !NETSTANDARD2_0
        public IAsyncEnumerable<IMessage> ReadAllAsync(CancellationToken externalToken);
#endif
    }
}