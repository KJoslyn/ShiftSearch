using System.Collections.Generic;

namespace ShiftSearch.Configs
{
    public class PlivoConfig
    {
        public PlivoConfig(string authId, string authToken, string fromNumber)
        {
            AuthId = authId;
            AuthToken = authToken;
            FromNumber = fromNumber;
        }

        public string AuthId { get; init; }
        public string AuthToken { get; init; }
        public string FromNumber { get; init; }
    }
}
