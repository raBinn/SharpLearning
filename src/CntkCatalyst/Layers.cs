using System;
using System.Linq;
using CNTK;

namespace CntkCatalyst
{
    /// <summary>
    /// Layers operations for CNTK
    /// </summary>
    public static class Layers
    {
        public static Function ReLU(this Function x)
        {
            return CNTKLib.ReLU(x);
        }

        public static Function Sigmoid(this Function x)
        {
            return CNTKLib.Sigmoid(x);
        }

        public static Function Softmax(this Function x)
        {
            return CNTKLib.Softmax(x);
        }

        public static Function Input(NDShape inputShape, DataType d)
        {
            return Variable.InputVariable(inputShape, d); ;
        }

        /// <summary>
        /// Based on Dense from: https://github.com/Microsoft/CNTK/blob/master/bindings/python/cntk/layers/layers.py
        /// </summary>
        /// <param name="x"></param>
        /// <param name="units"></param>
        /// <param name="weightInitializer"></param>
        /// <param name="biasInitializer"></param>
        /// <param name="bias"></param>
        /// <param name="inputRank"></param>
        /// <param name="mapRank"></param>
        /// <returns></returns>
        public static Function Dense(this Function x, int units,
            DataType d, DeviceDescriptor device,
            Initializer weightInitializer = Initializer.GlorotUniform,
            Initializer biasInitializer = Initializer.Zeros,
            bool bias = true, int inputRank = 0, int mapRank = 0)
        {
            return Dense(x, units,
                d, device,
                Initializers.Create(weightInitializer),
                Initializers.Create(biasInitializer),
                bias, inputRank, mapRank);
        }

        /// <summary>
        /// Based on Dense from: https://github.com/Microsoft/CNTK/blob/master/bindings/python/cntk/layers/layers.py
        /// </summary>
        /// <param name="x"></param>
        /// <param name="units"></param>
        /// <param name="weightInitializer"></param>
        /// <param name="biasInitializer"></param>
        /// <param name="bias"></param>
        /// <param name="inputRank"></param>
        /// <param name="mapRank"></param>
        /// <returns></returns>
        public static Function Dense(this Function x, int units, 
            DataType d, DeviceDescriptor device,
            CNTKDictionary weightInitializer, CNTKDictionary biasInitializer, 
            bool bias = true, int inputRank = 0, int mapRank = 0)
        {
            if (inputRank != 0 && mapRank != 0)
            {
                throw new ArgumentException("Dense: inputRank and mapRank cannot be specified at the same time.");
            }

            var outputShape = NDShape.CreateNDShape(new int[] { units });
            var outputRank = outputShape.Dimensions.Count;

            var inputRanks = (inputRank != 0) ? inputRank : 1;
            var dimensions = Enumerable.Range(0, inputRanks).Select(v => NDShape.InferredDimension).ToArray(); // infer all dimensions.
            var inputShape = NDShape.CreateNDShape(dimensions);

            int inferInputRankToMap;

            if (inputRank != 0)
            {
                inferInputRankToMap = -1; // means map_rank is not specified; input_rank rules.
            }
            else if (mapRank == 0)
            {
                inferInputRankToMap = 0;  // neither given: default to 'infer W to use all input dims'.
            }
            else
            {
                inferInputRankToMap = mapRank;  // infer W to use all input dims except the first static 'map_rank' ones.
            }

            var wDimensions = outputShape.Dimensions.ToList();
            wDimensions.AddRange(inputShape.Dimensions);
            var wShape = NDShape.CreateNDShape(wDimensions);

            var w = new Parameter(wShape, d, weightInitializer, device, "w");

            // Weights and input is in reversed order compared to the original python code.
            // Same goes for the dimensions. This is because the python api reverses the dimensions internally.
            // The python API was made in this way to be similar to other deep learning toolkits. 
            // The C# and the C++ share the same column major layout.
            var r = CNTKLib.Times(w, x, (uint)outputRank, inferInputRankToMap);

            if (bias)
            {
                var b = new Parameter(outputShape, d, biasInitializer, device, "b");
                r = r + b;
            }

            return r;
        }

        public static Function Conv2D(this Function input, int filterW, int filterH, int filterCount,
             DataType d, DeviceDescriptor device,
             int strideW = 1, int strideH = 1, uint seed = 34, string outputName = "")
        {
            if (input.Output.Shape.Rank != 3)
            {
                throw new ArgumentException("Conv2D layer requires shape rank 3, got rank " + input.Output.Shape.Rank);
            }

            var inputChannels = input.Output.Shape[2];

            var convWScale = 0.26;
            var convParams = new Parameter(new int[] { filterW, filterH, inputChannels, filterCount }, d,
                CNTKLib.GlorotUniformInitializer(convWScale, -1, 2, seed), device);

            return CNTKLib.Convolution(convParams, input, new int[] { strideW, strideH, inputChannels });
        }

        public static Function Pool2D(this Function input, int poolW, int poolH,
            PoolingType poolingType = PoolingType.Max,
            int strideW = 2, int strideH = 2, string outputName = "")
        {
            if (input.Output.Shape.Rank != 3)
            {
                throw new ArgumentException("Pool2D layer requires shape rank 3, got rank " + input.Output.Shape.Rank);
            }

            return CNTKLib.Pooling(input, PoolingType.Max,
                new int[] { poolW, poolH }, new int[] { strideW, strideH });
        }

        public static Function BatchNormalizationLayer(this Function input, bool spatial,
            DataType d, DeviceDescriptor device,
            double initialScaleValue = 1, double initialBiasValue = 0, int bnTimeConst = 5000)
        {
            var biasParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)initialBiasValue, device, "");
            var scaleParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)initialScaleValue, device, "");
            var runningMean = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningInvStd = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningCount = Constant.Scalar(0.0f, device);

            return CNTKLib.BatchNormalization(input, scaleParams, biasParams, runningMean, runningInvStd, runningCount,
                spatial, (double)bnTimeConst, 0.0, 1e-5 /* epsilon */);
        }
    }
}