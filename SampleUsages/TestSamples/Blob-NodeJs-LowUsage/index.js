// function has low usage gets message, reads it count to 1000 and finishes

module.exports = function (context, myBlob) {
    context.log("Function started with input " + myBlob);

    for(var i = 0; i < 1000; i++){
        context.log(i);
    }
    
    context.done();
}