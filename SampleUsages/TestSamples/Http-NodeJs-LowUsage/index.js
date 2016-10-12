module.exports = function (context, req) {
    context.log("Function started with input " + req.rawBody);
    context.done();
}