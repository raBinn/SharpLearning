﻿using SharpLearning.Containers.Matrices;

namespace SharpLearning.Learners.Interfaces
{
    /// <summary>
    /// Interface for indexed learner. 
    /// Only the observations from the provided indices in the index array will be used fortraining
    /// </summary>
    /// <typeparam name="TPrediction">The prediction type of the resulting model.</typeparam>
    public interface IIndexedLearner<TPrediction>
    {
        /// <summary>
        /// Only the observations from the provided indices in the index array will be used fortraining
        /// </summary>
        /// <param name="observations"></param>
        /// <param name="targets"></param>
        /// <param name="indices"></param>
        /// <returns></returns>
        IPredictor<TPrediction> Learn(F64Matrix observations, double[] targets, int[] indices);
    }
}