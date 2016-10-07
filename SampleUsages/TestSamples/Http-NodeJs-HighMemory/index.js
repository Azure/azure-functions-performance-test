// function is designed to allocate an array depending on input size

module.exports = function (context, req) {
    var sizeMb = parseInt(req.body);
    var arrSize = sizeMb * 1024 * 1024;
    var bytes = Buffer.alloc(arrSize);
    for (var i = 0; i < arrSize; i++) {
        bytes[i] = i % 255;
    }
    context.done();
};