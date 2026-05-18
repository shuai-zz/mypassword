using MyPasswordDesktop.Rpc;

namespace MyPasswordDesktop.Core.Data
{
    public sealed class PairRequest
    {
        public string name { get; set; }
        public string device { get; set; }
    }

    public sealed class PairResponse : BaseResponse
    {
        public sealed class PairResponseData
        {
            public long id { get; set; }
            public string seed { get; set; }
        }

        public PairResponseData data { get; set; }
    }

    public sealed class StringResponse : BaseResponse
    {
        public string data { get; set; }
    }
}
