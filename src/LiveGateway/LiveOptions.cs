using System.ComponentModel.DataAnnotations;

namespace LiveGateway;

public sealed class LiveOptions
{
    public const string Section = "GptLive";

    [Required]
    public string Model { get; set; } = "gpt-live-1.1";

    [Required]
    public string Instructions { get; set; } =
        "Be concise. Delegate account operations to the backend.";

    // Higher values need stronger evidence of speech to wake inference.
    [Range(0.0, 1.0)]
    public double VadThreshold { get; set; } = 0.55;

    // Seconds without an input or output transcript before standby.
    [Range(5, 60)]
    public int IdleTimeoutSeconds { get; set; } = 10;
}
