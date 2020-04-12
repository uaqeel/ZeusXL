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
    /// Not used. Only for referrence
    /// </summary>
    internal class libSVM_Model
    {
        /// <summary>
        /// parameter 
        /// </summary>
        public libSVM_Parameter param = null;

        /// <summary>
        /// SVs (SV[nr_SV])
        /// </summary>
        public double[][] SV = null;

        /// <summary>
        /// coefficients for SVs in decision functions (sv_coef[k-1][l])
        /// </summary>    
        public double[] sv_coef = null;

        /// <summary>
        /// constants in decision functions (rho[k*(k-1)/2])
        /// </summary>        
        public double[] rho = null;

        /// <summary>
        /// probA;          pariwise probability information
        /// </summary>    
        public double[] probA = null;

        /// <summary>
        /// probB;          pariwise probability information        
        /// </summary>
        public double[] probB = null;

        /// <summary>
        /// label;                     label of each class (label[k])
        /// </summary>
        public int[] labels = null;

        /// <summary>
        /// nSV;                   number of SVs for each class (nSV[k]), nSV[0] + nSV[1] + ... + nSV[k-1] = l 
        /// </summary>
        public double[][] nSV = null;

        /// <summary>
        /// free_sv;                    1 if svm_model is created by svm_load_model and 0 if trained
        /// </summary>
        public int free_sv = 0;
    }
}
