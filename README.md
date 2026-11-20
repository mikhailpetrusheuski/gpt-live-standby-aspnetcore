# GPT-Live Standby for ASP.NET Core

A small ASP.NET Core gateway that creates OpenAI GPT-Live WebRTC sessions with a standby policy, so quiet minutes bill at the standby rate instead of the active one.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8-512BD4)](https://dotnet.microsoft.com/)
[![Stars](https://img.shields.io/github/stars/mikhailpetrusheuski/gpt-live-standby-aspnetcore?style=social)](https://github.com/mikhailpetrusheuski/gpt-live-standby-aspnetcore/stargazers)

## What is here

| Path | Purpose |
| --- | --- |
| `src/LiveGateway/Program.cs` | `POST /live/session`: takes the browser's SDP offer, calls `POST /v1/live/sessions`, returns OpenAI's response |
| `src/LiveGateway/LiveOptions.cs` | Model, instructions, `vad_threshold` (0 to 1) and `idle_timeout_s` (5 to 60), validated at startup |
| `src/LiveGateway/SessionCost.cs` | Per-second cost estimate at $0.05 active and $0.02 standby per minute |
| `tests/` | Request body, pass-through and validation tests against a fake OpenAI handler, plus the cost example |

## Run the tests

```bash
dotnet test
```

The tests don't call OpenAI. They check that the gateway sends `session.wake_policy`, `session.idle_timeout_s` and `transport.type = "webrtc"` with the SDP offer, that OpenAI errors pass through, that an empty offer never reaches OpenAI, and that an idle timeout of 120 seconds stops the app at startup.

## Run the gateway

```bash
export OPENAI_API_KEY=...
dotnet run --project src/LiveGateway
```

The browser posts its SDP offer to `/live/session` and applies the SDP answer from the response to its `RTCPeerConnection`. Before production, add caller authentication, rate limits on session creation and a cap on concurrent sessions.

## References

- [GPT-Live guide](https://developers.openai.com/api/docs/guides/live)
- [Live conversations: managing idle sessions](https://developers.openai.com/api/docs/guides/live-conversations)
- [Pricing](https://developers.openai.com/api/docs/pricing)
- [Changelog, November 19, 2026](https://developers.openai.com/api/docs/changelog)
