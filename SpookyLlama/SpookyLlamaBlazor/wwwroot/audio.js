let audioUrl;

export async function play(element, audioBytes) {
    dispose();
    audioUrl = URL.createObjectURL(new Blob([audioBytes], { type: "audio/wav" }));
    element.src = audioUrl;

    try {
        await element.play();
        return true;
    } catch {
        return false;
    }
}

export function dispose() {
    if (audioUrl) {
        URL.revokeObjectURL(audioUrl);
        audioUrl = undefined;
    }
}