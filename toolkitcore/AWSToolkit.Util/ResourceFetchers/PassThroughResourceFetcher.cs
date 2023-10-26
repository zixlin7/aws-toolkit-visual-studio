using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    public class PassThroughResourceFetcher : IResourceFetcher
    {
        private readonly Stream _stream;
        public PassThroughResourceFetcher(Stream stream)
        {
            _stream = stream;
        }

        public Task<Stream> GetAsync(string path, CancellationToken token = default)
        {
            return Task.FromResult(_stream);
        }
    }
}
