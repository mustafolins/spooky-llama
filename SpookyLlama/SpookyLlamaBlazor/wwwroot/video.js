let videoUrl;

export async function play(element, videoBytes) {
    dispose();
    videoUrl = URL.createObjectURL(new Blob([videoBytes], { type: "video/mp4" }));
    element.src = videoUrl;

    try {
        await element.play();
        return true;
    } catch {
        return false;
    }
}

export function dispose() {
    if (videoUrl) {
        URL.revokeObjectURL(videoUrl);
        videoUrl = undefined;
    }
}