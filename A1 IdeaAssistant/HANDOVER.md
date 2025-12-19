# IdeaAssistant Handover Document

## Project Overview

A Blazor Server application demonstrating CSnakes integration with OpenAI Agents SDK. Users can chat with AI agents via text or voice input.

**Location**: `D:\ReposFred\CSnakesCourse\A1 IdeaAssistant`
**URL**: http://localhost:5200

## Current State

### What Works
- Basic chat UI with message history
- Text input and send functionality
- Python/CSnakes integration for OpenAI Agents SDK
- Microphone recording via JavaScript MediaRecorder API
- Full exception display for debugging

### Known Issues

#### 1. Transcription Errors (HIGH PRIORITY)
**Problem**: Speech transcription fails or is very slow.

**Symptoms**:
- "Transcription error" messages
- Long delays during transcription
- Inconsistent results

**Investigation needed**:
- Check `transcribe.py` - may have issues with temp file handling on Windows
- Verify OpenAI Whisper API is being called correctly
- Check if audio format (webm) is compatible with Whisper
- Add better error logging to see actual Python errors

**Files to check**:
- `PythonAgents/Src/transcribe.py` - Whisper transcription logic
- `wwwroot/js/audioRecorder.js` - Browser audio capture
- `Components/Pages/Home.razor` - C# transcription call

#### 2. Complex Mic UI Flow
**Current flow** (too complicated):
1. Click MIC to start recording
2. Click STOP to stop recording
3. Wait for transcription
4. Click SEND to send message

**This is confusing for users.**

---

## Proposed Improvements

### Simplified Voice Flow (PRIORITY)

**New flow**:
1. Click MIC to start recording (button shows "Recording...")
2. Click SEND to stop recording AND send (all in one action)

**Implementation**:
```
When MIC clicked:
  - Start recording
  - Change MIC button to show recording state
  - Enable SEND button

When SEND clicked (while recording):
  - Stop recording
  - Transcribe audio
  - Send transcribed text to AI agent
  - Show response

When SEND clicked (not recording):
  - Send text input as normal
```

**Benefits**:
- One less button click
- More intuitive - SEND always means "send my input"
- Recording state is just "preparing" the input

### UI Changes Needed

1. **Remove STOP button concept** - SEND handles everything
2. **MIC button states**:
   - Default: "MIC" - click to start recording
   - Recording: "REC" with red pulsing indicator
3. **SEND button**:
   - Works for both text AND voice
   - Disabled only when: processing OR (no text AND not recording)
4. **Status messages**:
   - "Click MIC to use voice, or type a message"
   - "Recording... Click Send when done"
   - "Sending..." (covers transcribe + AI call)

### Code Changes Required

**Home.razor**:
```csharp
// SEND button logic
private async Task SendMessage()
{
    if (IsRecording)
    {
        // Stop recording, transcribe, then send
        await StopRecordingAndSend();
    }
    else if (!string.IsNullOrWhiteSpace(CurrentInput))
    {
        // Normal text send
        await SendTextMessage();
    }
}

// MIC button only starts recording
private async Task StartRecording() { ... }
```

**Button states**:
```razor
<button class="send-button"
        @onclick="SendMessage"
        disabled="@(IsProcessing || (!IsRecording && string.IsNullOrWhiteSpace(CurrentInput)))">
    Send
</button>
```

---

## Technical Details

### Project Structure
```
A1 IdeaAssistant/
├── A1.IdeaAssistant.csproj      # Project file
├── Program.cs                    # CSnakes setup
├── Components/
│   ├── Pages/Home.razor         # Main chat UI
│   ├── Layout/MainLayout.razor
│   └── App.razor
├── wwwroot/
│   ├── app.css                  # Styles
│   └── js/audioRecorder.js      # Mic recording
└── PythonAgents/Src/
    ├── idea_agents.py           # OpenAI Agents (renamed from agents.py)
    ├── ideas_tools.py           # Agent tools
    ├── transcribe.py            # Whisper transcription
    └── requirements.txt         # Python deps
```

### Key Fix Applied
**File naming conflict**: Original `agents.py` conflicted with `openai-agents` package. Renamed to `idea_agents.py`.

### Dependencies
- CSnakes.Runtime 1.2.1
- openai-agents >= 0.6.0
- openai >= 1.0.0

### Environment
- Requires `OPENAI_API_KEY` in environment or .env file
- Python 3.12 (via CSnakes redistributable)
- .NET 9.0

---

## Commands

```bash
# Build
cd "D:\ReposFred\CSnakesCourse\A1 IdeaAssistant"
dotnet build "A1.IdeaAssistant.csproj"

# Run
dotnet run --project "A1.IdeaAssistant.csproj"

# Test Python imports
cd bin/Debug/net9.0/PythonAgents/Src
.venv/Scripts/python.exe -c "from idea_agents import process_message_with_history; print('OK')"
```

---

## Next Steps

1. [ ] Debug transcription errors - add logging to transcribe.py
2. [x] Implement simplified MIC flow (click MIC to record, click REC to send)
3. [ ] Test voice flow end-to-end
4. [ ] Consider adding audio level indicator during recording
5. [ ] Add timeout for transcription to prevent infinite waits
