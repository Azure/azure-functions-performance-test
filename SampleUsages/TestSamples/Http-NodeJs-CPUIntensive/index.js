// function is triggered by queue input. Input ia a number and function generates to random square matixes of input x input size
// then in multiplies matrixes, prints "Finish." when calculation is done.
// This operation is very heavily using CPU resources, concurrent executions on single container cause function to execute slower

module.exports = function (context, req) {
    main(context, req);
    context.done();
}

var multiple_row_and_column = function(row, column){
    // assume same length of row and column
    var result = 0;
    for(var i = 0; i < row.length; i++)
    {
        result += row[i] * column[i];
    }

    return result;
}

var create_random_matrix = function(size, seed, value_min, value_max) {
    var matrix = [];

    for (var i = 0; i < size; i++) {
        var row = [];
        for (var j = 0; j < size; j++){
            var val = parseInt(Math.random(seed) * (value_max - value_min), 10);
            row.push(val);
        }

        matrix.push(row);
    }

    return matrix;
}

var get_row_from_matrix = function(matrix, i){
    return matrix[i];
}

var get_column_from_matrix = function(matrix, j){
    var column = [];
    for(var i = 0; i < matrix.length; i++){
        column.push(matrix[i][j]);
    }

    return column;
}

var multiple_matrix = function(matrixA, matrixB) {
    var result = [];

    for (var i = 0; i < matrixA.length; i++) {
        var result_row = [];
        for (var j = 0; j < matrixA[0].length; j++) {
            var row = get_row_from_matrix(matrixA, i);
            var column = get_column_from_matrix(matrixB, j);
            result_row.push(multiple_row_and_column(row, column));
        }
        result.push(result_row);
    }

    return result;    
}

var print_matrix = function(matrix){
    for (var i = 0; i < matrix.length; i++) {
        var row = '';
        for (var j = 0; j < matrix[0].length; j++) {
            row += (' ' + matrix[i][j]);
        }

        console.log(row);
    }
}

var main = function (context, req) {
    var seed = 123;
    var value_min = 0;
    var value_max = 101;
    var size = parseInt(req.body);

    var matrix = create_random_matrix(size, seed, value_min, value_max);
    seed = 2 * seed;
    var matrix2 = create_random_matrix(size, seed, value_min, value_max);
    multiple_matrix(matrix, matrix2);
}