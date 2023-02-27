
var fileInput = document.getElementById("file");
var inputtext = document.getElementById("input-text");
var upinput = document.getElementById("inputbtn");
var userId = document.getElementById("UserId");
var description = document.getElementById("Description");
var isDefault = document.getElementById("default");


const queryString = window.location.search;
const urlParams = new URLSearchParams(queryString);


fileInput.addEventListener("change", function (e) {

    // Get the selected file from the input element
    var file = e.target.files[0]

    

    // Create a new tus upload
    var upload = new tus.Upload(file, {
        // Endpoint is the upload creation URL from your tus server
        endpoint: `${window.location.origin}/files/`,
        // Retry delays will enable tus-js-client to automatically retry on errors
        retryDelays: [0, 3000, 5000, 10000, 20000],
        // Attach additional meta data about the file for the server
        metadata: {
            filename: file.name,
            filetype: file.type,
            isDefault: isDefault.value,
            description: description.value,
            galleryId: urlParams.get("id"),
            userId: userId.value,

        },
        // Callback for errors which cannot be fixed using retries
        onError: function (error) {
            console.log("Failed because: " + error)
        },
        // Callback for reporting upload progress
        onProgress: function (bytesUploaded, bytesTotal) {
            var percentage = (bytesUploaded / bytesTotal * 100).toFixed(2)
            console.log(bytesUploaded, bytesTotal, percentage + "%")
            inputtext.innerText = "File is being uploaded!"
            upinput.disabled = true;
            upinput.style.cursor = "not-allowed";
        },

        // Callback for once the upload is completed
        onSuccess: function () {
            console.log("Download %s from %s", upload.file.name, upload.url)
            inputtext.innerText = "File selected!"
            upinput.disabled = false;
            upinput.style.cursor = "pointer";
        }
    })
    if (file.length != 0) {
        console.log(file.type)
        if (file.type != "image/png" && file.type != "image/jpeg") {
            inputtext.innerText = "Not an image!"
            upinput.disabled = true;
            upinput.style.cursor = "not-allowed";
        }
        else {
            // Check if there are any previous uploads to continue.
            upload.findPreviousUploads().then(function (previousUploads) {
                // Found previous uploads so we select the first one. 

                // Start the upload
                upload.start()
            })
            
        }

    }
    
})
