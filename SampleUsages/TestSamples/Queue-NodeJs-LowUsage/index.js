// function has low usage gets message, reads it count to 1000 and finishes

module.exports = function (context, input) {
    context.log("Function started with input " + input);
    context.bindings.output = input;
    context.done();
}