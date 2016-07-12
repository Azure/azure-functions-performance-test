# azure-functions-performance-test

## Set up Environment

### Tests samples required to run the according tests.

#### Queue-Node-Js-CPU-Intensive

<p>
Function is triggered by queue input. Input ia a number and function generates to random square matixes of input x input size
then in multiplies matrixes, prints "Finish." when calculation is done.
This operation is very heavily using CPU resources, concurrent executions on single container cause function to execute slower
</p>

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-functions-performance-test%2Fmaster%2FSampleUsages%2FTestSamples%2FQueue-NodeJs-CPUIntensive%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-functions-performance-test%2Fmaster%2FSampleUsages%2FTestSamples%2FQueue-NodeJs-CPUIntensive%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>


#### Queue-Node-Js-High-Memory

<p>
Function is designed to allocate big array depending on input size.
</p>

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-functions-performance-test%2Fmaster%2FSampleUsages%2FTestSamples%2FQueue-NodeJs-HighMemory%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-functions-performance-test%2Fmaster%2FSampleUsages%2FTestSamples%2FQueue-NodeJs-HighMemory%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>


#### Queue-Node-Js-Low-Usage

<p>
Function has low usage gets message, reads it count to 1000 and finishes.
</p>

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-functions-performance-test%2Fmaster%2FSampleUsages%2FTestSamples%2FQueue-NodeJs-LowUsage%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fazure-functions-performance-test%2Fmaster%2FSampleUsages%2FTestSamples%2FQueue-NodeJs-LowUsage%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>

