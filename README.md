# AI Integrations API

A **.NET 9 Web API** proof-of-concept that integrates with multiple LLM providers.  
Currently, **Claude (Anthropic)** is implemented; **OpenAI** is stubbed out for future support.  

The project demonstrates:
- Provider abstraction (`IChatProvider`, `ChatProviderFactory`) for swapping AI backends
- **Swagger UI** with example requests/responses
- **API key authentication** (owner/guest keys with expiration, GUIDs)
- **Serilog logging** (file sink locally, console/App Insights in Azure)
- Support for **conversational context** (message history) with Claude

---

## Current Implementation

- ✅ **Claude support** — working multi-turn conversations (context retained if you include previous messages in the request)  
- 🚧 **OpenAI provider** — stubbed with `"Not implemented yet"` response  
- 🔑 **API Key auth** — set via `UserSecrets` locally or App Service configuration in Azure  
- 📝 **SessionId and Provider fields** — included for planned frontend integration. Right now, they can be ignored or set manually.  
- 🔄 **Messages array** — in the future, this will be automatically tracked by the backend.  
  For now, you must **manually append previous turns** in Swagger to simulate multi-turn conversations.

---

## Running Locally
-- Project - User Secrets example
{
  "AI:PROVIDERS:CLAUDE:APIKEY": "Enter-your-Claude API-key-here",
  "ApiKeys:OwnerKey": "owner-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ApiKeys:Guests:0:Key": "guest-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ApiKeys:Guests:0:ExpiresUtc": "2025-10-01T00:00:00Z",
  "ApiKeys:Guests:0:Label": "Guest0"
}


### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Anthropic Claude API key (saved in `UserSecrets` or environment variable `AI:PROVIDERS:CLAUDE:APIKEY`)

### Start the API
```bash
dotnet run --project ClaudeAPI


# All Rights Reserved