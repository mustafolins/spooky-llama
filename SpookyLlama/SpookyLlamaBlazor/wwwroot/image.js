let imageUrl;

export function show(element, imageBytes) {
    dispose();
    imageUrl = URL.createObjectURL(new Blob([imageBytes], { type: "image/png" }));
    element.src = imageUrl;
}

export function dispose() {
    if (imageUrl) {
        URL.revokeObjectURL(imageUrl);
        imageUrl = undefined;
    }
}