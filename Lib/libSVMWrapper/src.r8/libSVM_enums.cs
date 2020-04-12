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
    /// svm types
    /// </summary>
    public enum SVM_TYPE
    { 
        /// <summary>
        /// C_SVC           Uses parameter C
        /// </summary>
        C_SVC=(int)0, 

        /// <summary>
        /// NU_SVC          Uses parameter nu
        /// </summary>
        NU_SVC=(int)1, 

        /// <summary>
        /// ONE_CLASS       Uses parameter C
        /// </summary>
        ONE_CLASS=2, 

        /// <summary>
        /// EPSILON_SVR     Uses parameter p
        /// </summary>
        EPSILON_SVR=(int)3, 

        /// <summary>
        /// NU_SVR          uses parameter nu
        /// </summary>
        NU_SVR=(int)4 
    };

    /// <summary>
    /// kernel types
    /// </summary>
    public enum KERNEL_TYPE 
    { 
        /// <summary>
        /// u'*v\
        /// </summary>
        LINEAR = (int)0, 

        /// <summary>
        /// gamma*u'*v + coef0)^degree
        /// </summary>
        POLY = (int)1,

        /// <summary>
        /// exp(-gamma*|u-v|^2)
        /// </summary>
        RBF = (int)2, 

        /// <summary>
        /// gamma*u'*v + coef0
        /// </summary>
        SIGMOID = (int)3, 

        /// <summary>
        /// kernel values in training_set_file
        /// </summary>
        PRECOMPUTED = (int)4 
    };
}
