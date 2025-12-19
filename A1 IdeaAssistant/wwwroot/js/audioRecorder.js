// Audio Recorder and UI helpers for Idea Assistant

// Scroll chat to bottom
window.scrollChatToBottom = function() {
    const chat = document.getElementById('chatContainer');
    if (chat) {
        chat.scrollTop = chat.scrollHeight;
    }
};

// Audio Recording - Uses MediaRecorder API
let mediaRecorder = null;
let audioChunks = [];
let recordingStream = null;

window.audioRecorder = {
    isSupported: function() {
        return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
    },

    startRecording: async function() {
        try {
            audioChunks = [];

            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            recordingStream = stream;

            const options = { mimeType: 'audio/webm' };
            if (!MediaRecorder.isTypeSupported(options.mimeType)) {
                mediaRecorder = new MediaRecorder(stream);
            } else {
                mediaRecorder = new MediaRecorder(stream, options);
            }

            mediaRecorder.ondataavailable = function(event) {
                if (event.data.size > 0) {
                    audioChunks.push(event.data);
                }
            };

            mediaRecorder.start(250);
            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    stopRecording: function() {
        return new Promise((resolve) => {
            if (!mediaRecorder || mediaRecorder.state === 'inactive') {
                resolve({ success: false, error: 'No active recording' });
                return;
            }

            mediaRecorder.onstop = function() {
                if (recordingStream) {
                    recordingStream.getTracks().forEach(track => track.stop());
                    recordingStream = null;
                }

                if (audioChunks.length === 0) {
                    resolve({ success: false, error: 'No audio recorded' });
                    return;
                }

                const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
                if (audioBlob.size === 0) {
                    resolve({ success: false, error: 'Empty recording' });
                    return;
                }

                resolve({ success: true, blob: audioBlob });
            };

            if (mediaRecorder.state === 'recording') {
                mediaRecorder.requestData();
            }
            mediaRecorder.stop();
        });
    },

    // Stop recording and send to server for transcription
    stopAndTranscribe: async function() {
        const result = await this.stopRecording();
        if (!result.success) {
            return { success: false, error: result.error };
        }

        try {
            const response = await fetch('/api/audio/transcribe', {
                method: 'POST',
                body: result.blob,
                headers: {
                    'Content-Type': 'audio/webm'
                }
            });

            if (!response.ok) {
                const err = await response.json();
                return { success: false, error: err.error || 'Transcription failed' };
            }

            const data = await response.json();
            return { success: true, text: data.text };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    cancelRecording: function() {
        if (mediaRecorder && mediaRecorder.state !== 'inactive') {
            mediaRecorder.stop();
        }
        if (recordingStream) {
            recordingStream.getTracks().forEach(track => track.stop());
            recordingStream = null;
        }
        audioChunks = [];
        mediaRecorder = null;
        return { success: true };
    }
};
