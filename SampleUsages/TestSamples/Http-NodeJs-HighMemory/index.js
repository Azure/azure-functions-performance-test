// function is designed to allocate an array depending on input size

module.exports = function (context, req) {
    var sizeMb = parseInt(req.body);
    var b = Buffer.alloc(sizeMb * 1024 * 1024, 1);
    context.done();
};