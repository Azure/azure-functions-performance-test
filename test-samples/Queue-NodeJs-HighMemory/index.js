// function is designed to allocate big array depending on input size

module.exports = function (context, size) {
    var array = [];
    var seed = size;

    for(var i = 0; i < size; i++){
        var x = Math.random(seed) * size;
        array.push(x);
    }

    for(var j = 1; j < size; j++){
        var y = Math.random(seed) * size;
        array[j] = array[j - 1] + y;
    }
    
    context.done();
}