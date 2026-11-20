namespace LiveGateway;

public static class LiveSessionRequest
{
    public static object Create(string sdpOffer, LiveOptions options) => new
    {
        session = new
        {
            model = options.Model,
            instructions = options.Instructions,
            wake_policy = new
            {
                type = "speech",
                vad_threshold = options.VadThreshold
            },
            idle_timeout_s = options.IdleTimeoutSeconds
        },
        transport = new
        {
            type = "webrtc",
            sdp = sdpOffer
        }
    };
}
