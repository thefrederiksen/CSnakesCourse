"""
Voice Transcription using OpenAI Whisper

Transcribes audio input to text for the Idea Assistant.
"""

from openai import OpenAI
import os
import tempfile


def transcribe_audio(audio_bytes: bytes, api_key: str) -> str:
    """
    Transcribe audio to text using OpenAI Whisper.

    Args:
        audio_bytes: Raw audio data (webm, mp3, wav, etc.)
        api_key: OpenAI API key

    Returns:
        Transcribed text
    """
    client = OpenAI(api_key=api_key)

    # Write audio to a temporary file (Whisper API needs a file)
    temp_path = None
    try:
        with tempfile.NamedTemporaryFile(suffix=".webm", delete=False) as temp_file:
            temp_file.write(audio_bytes)
            temp_path = temp_file.name

        with open(temp_path, "rb") as audio_file:
            transcript = client.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
                response_format="text"
            )

        # response_format="text" returns string directly
        if isinstance(transcript, str):
            return transcript.strip()
        else:
            return str(transcript).strip()
    finally:
        # Clean up temp file
        if temp_path and os.path.exists(temp_path):
            try:
                os.remove(temp_path)
            except:
                pass  # Ignore cleanup errors on Windows


def transcribe_audio_file(file_path: str, api_key: str) -> str:
    """
    Transcribe an audio file to text.

    Args:
        file_path: Path to the audio file
        api_key: OpenAI API key

    Returns:
        Transcribed text
    """
    client = OpenAI(api_key=api_key)

    with open(file_path, "rb") as audio_file:
        transcript = client.audio.transcriptions.create(
            model="whisper-1",
            file=audio_file,
            response_format="text"
        )

    return transcript.strip()
