let video = document.getElementById("video");
let canvas = document.getElementById("canvas");
let imageInput = document.getElementById("ProfileImageBase64");

navigator.mediaDevices.getUserMedia({ video: true })
    .then(stream => video.srcObject = stream);

function capture() {
    canvas.getContext("2d").drawImage(video, 0, 0, 300, 300);
    imageInput.value = canvas.toDataURL("image/png");
}
