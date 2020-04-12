/*
 * GNU LESSER GENERAL PUBLIC LICENSE
 * Version 3, 29 June 2007
 * 
 * Copyright 2011 Rafał "R@V" Prasał <rafal.prasal@gmail.com>
*/

/// <summary>
/// Package for wrapper for libsvm.dll
/// </summary>
namespace libSVMWrapper
{
    /// <summary>
    /// Parameter for training
    /// </summary>
    public class libSVM_Parameter
    {
        /// <summary>
        /// svm_type
        /// </summary>
        public SVM_TYPE svm_type = SVM_TYPE.C_SVC;

        /// <summary>
        /// kernel_type
        /// </summary>
        public KERNEL_TYPE kernel_type = KERNEL_TYPE.RBF;

        /// <summary>
        /// degree;	         for poly 
        /// </summary>
        public int degree = 0;

        /// <summary>
        /// gamma;           for poly/rbf/sigmoid
        /// </summary>
        public double gamma = 0.0;

        /// <summary>
        /// coef0;           for poly/sigmoid
        /// </summary>
        public double coef0 = 0.0;

        /* these are for training only */

        /// <summary>
        /// in MB
        /// </summary>
        public double cache_size = 256.0;

        /// <summary>
        /// stopping criteria
        /// </summary>
        public double eps = 0.001;

        /// <summary>
        /// C;              for C_SVC, ONE_CLASS
        /// </summary>
        public double C = 1.0;

        /// <summary>
        /// weight label corresponding with weight; for C_SVC 
        /// </summary>
        public int[] weight_label = null;

        /// <summary>
        /// weight corresponding to weight_label; for C_SVC
        /// </summary>
        public double[] weight = null;

        /// <summary>
        /// nu;             for NU_SVC, NU_SVR
        /// </summary>
        public double nu = 0;

        /// <summary>
        /// p;              for EPSILON_SVR
        /// </summary>
        public double p = 0.0;

        /// <summary>
        /// use the shrinking heuristics
        /// </summary>
        public int shrinking = 0;

        /// <summary>
        /// do probability estimates
        /// </summary>
        public int probability = 0;
    }
}
