// function is designed to simply copy input to output

module.exports = function (context, input) {
    context.log("Function started with input " + input);
    context.bindings.output = input;
    context.done();
}