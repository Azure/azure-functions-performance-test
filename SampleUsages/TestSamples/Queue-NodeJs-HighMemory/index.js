// function is designed to allocate an array depending on input size

module.exports = function (context, sizeMb) {
    var b = Buffer.alloc(sizeMb * 1024 * 1024, 1);
    context.bindings.output = sizeMb;
    context.done();
}