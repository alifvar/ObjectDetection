export async function startCapture(dotNetRef) {
    const video = document.getElementById("localVideo");
    const stream = await navigator.mediaDevices.getUserMedia({ video: true });
    video.srcObject = stream;

    const canvas = document.createElement("canvas");
    const context = canvas.getContext("2d");

    setInterval(() => {
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        context.drawImage(video, 0, 0, canvas.width, canvas.height);
        const dataUrl = canvas.toDataURL("image/jpeg", 0.9);
        const base64 = dataUrl.split(',')[1];

        dotNetRef.invokeMethodAsync("SendFrameToServer", base64);
    }, 1000);
}

window.updateImage = function (base64) {
    document.getElementById("processedImage").src = 'data:image/jpeg;base64,' + base64;
};