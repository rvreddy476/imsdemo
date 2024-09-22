var signaturePadWrappers = document.querySelectorAll('.signature-pad');

Array.prototype.forEach.call(signaturePadWrappers, function (wrapper) {
    debugger;
    var canvas = wrapper.querySelector('canvas');
    var clearButton = wrapper.querySelector('.btn-clear-canvas');
    var hiddenInput = wrapper.querySelector('input[type="hidden"]');
    var addsignbtn = wrapper.querySelector('#btn-place-signature');   
    var signaturePad = new SignaturePad(canvas, {
        penColor: "#0B0B45"

    });

    signaturePad.onEnd = function () {
        debugger;
        // Returns signature image as data URL and set it to hidden input
        //base64str = signaturePad.toDataURL().split(',')[1];        
        //hiddenInput.value = btoa(base64str);
        // code for image preview
        debugger
        base64str = signaturePad.toDataURL();
        var gpid = $("#Gatepass_id").val();
        $('#basesavetarget').attr("src", base64str);

        $.ajax({
            type: 'POST',
            url: '/Add_Gatepass/PreviewImage',
            data: { "ImgStr": base64str, "GPID": gpid },
            //dataType: 'text',
            async: 'false',
            cache: false,          
            //processData: false,
            success: function (data) {
                debugger;
                //var converteddata = JSON.parse(data);
                console.log(data);
                if (data.data == 00) {
                    $('#savetarget').attr("src", data.url);
                }
   
            }

        })
    };

    if (addsignbtn != null) {
        addsignbtn.addEventListener('click', function () {
            debugger;
            signaturePad.clear();
            $('#signaturePath').val('');
            $('#savetarget').attr("src", "");
            $('#basesavetarget').attr("src", "");
            signaturePad.off();

        });
    }
    clearButton.addEventListener('click', function () {
        debugger;
        // Clear the canvas and hidden input
        signaturePad.clear();
        hiddenInput.value = '';
        $('#savetarget').attr("src", "");
        $('#basesavetarget').attr("src", "");
    });

});