/*
 * GNU LESSER GENERAL PUBLIC LICENSE
 * Version 3, 29 June 2007
 * 
 * Copyright 2011 Rafał "R@V" Prasał <rafal.prasal@gmail.com>
*/

using System;
using System.Text;


/// <summary>
/// Package for wrapper for libsvm.dll
/// </summary>
namespace libSVMWrapper
{
    /// <summary>
    /// Wrapper Class for libsvm.dll
    /// </summary>
    /// Usage examples:
    /// 
    ///     Loading model:
    /// @code
    ///             libSVM svm = new libSVM(); 
    ///             svm.Reload("model_file");       //see Reload()
    /// @endcode
    /// 
    ///         or 
    ///         
    /// @code
    ///             libSVM svm = libSVM.Load("model_file");     //see Load()
    /// @endcode
    /// 
    ///     Teaching model
    ///     
    ///         When you know parameters you want to use
    /// @code
    ///             libSVM_Problem Problem;                 //see libSVM_Problem
    ///             libSVM_Parameter Parameter;             //see libSVM_Parameter
    ///             libSVM svm = new libSVM(); 
    ///         
    ///             svm.Train(Problem, Parameter);
    /// @endcode
    /// 
    ///         or  When you have no idea what parameters to use
    ///         
    /// @code
    ///             libSVM_Problem Problem;                 //see libSVM_Problem
    ///             libSVM_Parameter Parameter;             //see libSVM_Parameter
    ///             libSVM svm = new libSVM(); 
    ///         
    ///             svm.TrainAuto(fold ,Problem, Parameter);      //see TrainAuto()
    /// @endcode
    /// 
    ///         or  When you know search grids for parameters
    ///         
    /// @code
    ///             libSVM_Problem Problem;                 //see libSVM_Problem
    ///             libSVM_Parameter Parameter;             //see libSVM_Parameter
    ///             libSVM_Grid grid_c;                     //see libSVM_Grid
    ///             ...
    /// 
    ///             libSVM svm = new libSVM(); 
    ///         
    ///             svm.TrainAuto(fold, Problem, Parameter, grid_c, ...);      //see TrainAuto()
    /// @endcode
    /// 
    ///     Saving model:
    /// 
    /// @code
    ///             libSVM svm;
    ///         
    ///             svm.Save("model_file");     //see Save()
    /// @endcode
    /// 
    ///     Using model for prediction:
    ///     
    /// @code
    ///             double[] sample;
    ///             libSVM svm;
    ///         
    ///             double label = svm.Predict(x);          //see Predict()
    /// @endcode
    /// 
    ///     Removing model from memmory
    ///     
    /// @code
    ///             libSVM svm;
    ///             
    ///             svm.Dispose();             //see dispose()
    /// @endcode
    /// 
               
    public partial class libSVM : IDisposable
    {
        /// <summary>
        /// Wrapper version
        /// </summary>
        public const string version = "3.0.1";

        /// <summary>
        /// internal pointers
        /// </summary>
        IntPtr __model_ptr = IntPtr.Zero;
        IntPtr __problem_ptr = IntPtr.Zero;
        IntPtr __parameter_ptr = IntPtr.Zero;

        /// <summary>
        /// Train model
        /// </summary>
        /// <param name="_problem">samples and labels</param>
        /// <param name="_parameter">model parameters</param>
        public void Train(libSVM_Problem _problem, libSVM_Parameter _parameter)
        {
            Dispose();

            __problem_ptr = libSVM_Problem_to_svm_problem_ptr(_problem);
            __parameter_ptr = libSVM_Parameter_to_svm_parameter_ptr(_parameter);

            IntPtr error_ptr = svm_check_parameter(__problem_ptr, __parameter_ptr);

            if (error_ptr != IntPtr.Zero)
                throw new Exception(ptr_to_string(error_ptr));

            __model_ptr = svm_train(__problem_ptr, __parameter_ptr);
        }

        /// <summary>
        /// Trains model automatically, using default Grids for parameter value search
        /// Similar to OpenCV
        /// </summary>
        /// <param name="_fold">folds for samples >=2 </param>
        /// <param name="_problem">samples and labels</param>
        /// <param name="_parameter">model parameters</param>
        public void TrainAuto(int _fold, libSVM_Problem _problem, libSVM_Parameter _parameter)
        {
            TrainAuto(_fold, _problem, _parameter, libSVM_Grid.C(), libSVM_Grid.gamma(), libSVM_Grid.p(), libSVM_Grid.nu(), libSVM_Grid.coef(), libSVM_Grid.degree());
        }

        /// <summary>
        /// Trains model automatically using users grids. if your svm_type/kernel_type does not need it then set it to null
        /// </summary>
        /// <param name="_fold">folds for samples >=2 </param>
        /// <param name="_problem">samples and labels</param>
        /// <param name="_parameter">model parameters</param>
        /// <param name="_grid_c">grid for C</param>
        /// <param name="_grid_gamma">grid for gamma</param>
        /// <param name="_grid_p">grid for p</param>
        /// <param name="_grid_nu">grid for nu</param>
        /// <param name="_grid_coef0">grid for coef</param>
        /// <param name="_grid_degree">grid for degree</param>
        public void TrainAuto(int _fold, libSVM_Problem _problem, libSVM_Parameter _parameter, libSVM_Grid _grid_c, libSVM_Grid _grid_gamma, libSVM_Grid _grid_p, libSVM_Grid _grid_nu, libSVM_Grid _grid_coef0, libSVM_Grid _grid_degree)
        {
            Dispose();

            if (_problem == null) throw new Exception("libSVMProblem not initialized");
            if (_problem.samples == null) throw new Exception("libSVMProblem.samples = null");
            if (_problem.labels == null) throw new Exception("libSVMProblem.labels = null");
            if (_problem.samples.Length != _problem.labels.Length) throw new Exception("libSVMProblem.samples.Length != libSVMProblem.labels.length");
            if (_parameter == null) throw new Exception("libSVMParameter not initialized");
            if (_parameter.weight == null && _parameter.weight_label != null) throw new Exception("libSVMParameter.weight = null and libSVMParameter.weight_label != null");
            if (_parameter.weight != null && _parameter.weight_label == null) throw new Exception("libSVMParameter.weight_label = null and libSVMParameter.weight != null");
            if (_parameter.weight != null && _parameter.weight_label != null && _parameter.weight_label.Length != _parameter.weight.Length) throw new Exception("libSVMParameter.weight_label.Length != libSVMParameter.weight.Length");
            if (_fold < 2 || _fold > _problem.samples.Length) throw new Exception("fold < 2 || fold > nr_samples");

            libSVM_Grid grid_gamma = _grid_gamma; 
            libSVM_Grid grid_coef0 = _grid_coef0;
            libSVM_Grid grid_degree = _grid_degree;            

            if (_parameter.kernel_type == KERNEL_TYPE.LINEAR 
                || _parameter.kernel_type == KERNEL_TYPE.PRECOMPUTED)
            {
                grid_gamma = new libSVM_Grid();
                grid_coef0 = new libSVM_Grid();
                grid_degree = new libSVM_Grid();                
            }
            else if (_parameter.kernel_type == KERNEL_TYPE.POLY)
            {
                if(grid_gamma==null) throw new Exception("grid_gamma not set");
                if(grid_coef0==null) throw new Exception("grid_coef0 not set");
                if(grid_degree==null) throw new Exception("grid_degree not set");

            }
            else if (_parameter.kernel_type == KERNEL_TYPE.RBF)
            {
                if(grid_gamma==null) throw new Exception("grid_gamma not set");

                grid_coef0 = new libSVM_Grid();
                grid_degree = new libSVM_Grid();                
            }
            else if (_parameter.kernel_type == KERNEL_TYPE.SIGMOID)
            {
                if(grid_gamma==null) throw new Exception("grid_gamma not set");
                if(grid_coef0==null) throw new Exception("grid_coef not set");
                grid_degree = new libSVM_Grid();
            }
            else throw new Exception("unknown kernel_type");

            libSVM_Grid grid_c = _grid_c;
            libSVM_Grid grid_p = _grid_p;
            libSVM_Grid grid_nu = _grid_nu;             

            if (_parameter.svm_type == SVM_TYPE.C_SVC 
                || _parameter.svm_type == SVM_TYPE.ONE_CLASS)
            {
                if(grid_c== null) throw new Exception("grid_C not set");

                grid_nu = new libSVM_Grid();
                grid_p = new libSVM_Grid();
            }
            else if (_parameter.svm_type == SVM_TYPE.NU_SVC || _parameter.svm_type == SVM_TYPE.NU_SVR)
            {
                if(grid_nu== null) throw new Exception("grid_nu not set");

                grid_c = new libSVM_Grid();
                grid_p = new libSVM_Grid();                
            }
            else if (_parameter.svm_type == SVM_TYPE.EPSILON_SVR)
            {
                if (grid_p == null) throw new Exception("grid_p not set");

                grid_c = new libSVM_Grid();
                grid_nu = new libSVM_Grid();                               
            }
            else throw new Exception("unknown svm_type");

            double c=grid_c.min;
            double p=grid_p.min;
            double nu=grid_nu.min;

            double gamma = grid_gamma.min;
            double coef0 = grid_coef0.min;
            double degree = grid_degree.min;
            int f=0;

            int nr_test_samples = _problem.labels.Length/(int)_fold;
            int nr_train_samples = _problem.labels.Length - nr_test_samples;

            double test_probe=0.0;

            libSVM_Parameter train_parameter = new libSVM_Parameter();

            //copy parameters set by user;
            train_parameter.weight = _parameter.weight;
            train_parameter.weight_label = _parameter.weight_label;
            train_parameter.shrinking = _parameter.shrinking;
            train_parameter.svm_type = _parameter.svm_type;
            train_parameter.kernel_type = _parameter.kernel_type;
            train_parameter.probability = _parameter.probability;
            train_parameter.cache_size = _parameter.cache_size;

            libSVM_Problem train_problem = new libSVM_Problem();
            libSVM_Problem test_problem = new libSVM_Problem();

            //fold
            do{
                train_problem.labels = new double[nr_train_samples];
                train_problem.samples = new double[nr_train_samples][];

                test_problem.labels = new double[nr_test_samples];
                test_problem.samples = new double[nr_test_samples][];

                int j = 0;
                int k = 0;
                for (int i = 0; i < nr_train_samples + nr_test_samples; i++)
                    if (i % _fold == 0)
                    {
                        test_problem.labels[j] = _problem.labels[i];
                        test_problem.samples[j] = _problem.samples[i];
                        j++;
                    }
                    else
                    {
                        train_problem.labels[k] = _problem.labels[i];
                        train_problem.samples[k] = _problem.samples[i];
                    }

                //p
                p = grid_p.min;
                do{
                    //nu
                    nu = grid_nu.min;
                    do{
                        //gamma
                        gamma = grid_gamma.min;
                        do{
                            //coef0
                            coef0 = grid_coef0.min;
                            do{
                                //degree

                                degree = grid_degree.min;
                                do{
                                    //c

                                    c = grid_c.min;
                                    do{
                                        //svm_train alters problem_ptr so it's necessary to create it every time;
                                        IntPtr this_problem_ptr = libSVM_Problem_to_svm_problem_ptr(train_problem);
                                                                                    
                                        //set generated parameters
                                        train_parameter.C = c;
                                        train_parameter.p = p;
                                        train_parameter.nu = nu;
                                        train_parameter.gamma = gamma;
                                        train_parameter.degree = (int)degree;
                                        train_parameter.coef0 = coef0;

                                        IntPtr this_parameter_ptr = libSVM_Parameter_to_svm_parameter_ptr(train_parameter);

                                        IntPtr error_ptr = svm_check_parameter(__problem_ptr, __parameter_ptr);

                                        if (error_ptr != IntPtr.Zero)
                                            throw new Exception(ptr_to_string(error_ptr));
                                        
                                        IntPtr this_model_ptr = svm_train(this_problem_ptr,this_parameter_ptr);

                                        double this_test_probe=0.0;
                                        
                                        //count propperly recognized test_problem.samples
                                        for(int i=0; i<test_problem.labels.Length; i++)
                                        {
                                            IntPtr svm_nodes_ptr = sample_to_svm_nodes_ptr(test_problem.samples[i]);

                                            if(test_problem.labels[i]==svm_predict(__model_ptr, svm_nodes_ptr))
                                                this_test_probe++;

                                            Free_ptr(ref svm_nodes_ptr);
                                        }

                                        //if first run then just copy this
                                        if(__model_ptr==IntPtr.Zero) 
                                        {
                                            __model_ptr = this_model_ptr;
                                            __parameter_ptr = this_parameter_ptr;
                                            __problem_ptr = this_problem_ptr;

                                            test_probe = this_test_probe;
                                        }
                                        else
                                            //if model was better than previous then free previous data and copy this
                                            if (this_test_probe > test_probe)
                                            {
                                                Free_svm_parameter_ptr(ref __parameter_ptr);
                                                Free_svm_problem_ptr(ref __problem_ptr);
                                                svm_free_and_destroy_model(ref __model_ptr);

                                                __parameter_ptr = this_parameter_ptr;                                                
                                                __problem_ptr = this_problem_ptr;
                                                __model_ptr = this_model_ptr;

                                                test_probe = this_test_probe;
                                            }
                                            //if not then free this model
                                            else
                                                {
                                                    Free_svm_problem_ptr(ref this_problem_ptr);
                                                    Free_svm_parameter_ptr(ref this_parameter_ptr);
                                                    svm_free_and_destroy_model(ref this_model_ptr);
                                                }

                                        c *= grid_c.step;
                                    }while(c<grid_c.max);

                                    degree *= grid_degree.step;
                                }while(degree<grid_degree.max);

                                coef0 *= grid_coef0.step;
                            }while(coef0<grid_coef0.max);

                            gamma *= grid_degree.step;
                        }while(gamma<grid_gamma.max);

                        nu*=grid_nu.step;
                    }while(nu<grid_nu.max);

                    p *= grid_p.step;
                }while(p<grid_p.max);

                f++;
            }while(f<_fold);
        }

        /// <summary>
        /// Save model to file
        /// </summary>
        /// <param name="filename">model file</param>
        public void Save(string filename)
        {
            if (__model_ptr == IntPtr.Zero) throw new Exception("model neither loaded nor trained");
            svm_save_model(filename, __model_ptr);
        }

        /// <summary>
        /// Reload libSVM model
        /// </summary>
        /// <param name="_filename">name of model file</param>
        public void Reload(string _filename)
        {
            Dispose();

            __model_ptr = svm_load_model(_filename);

            if (__model_ptr == IntPtr.Zero) throw new Exception("bad model file");
        }

        /// <summary>
        ///  Load model from file
        /// </summary>
        /// <param name="_filename">Model file</param>        
        public static libSVM Load(string _filename)
        {            
            libSVM svm = new libSVM();

            svm.Reload(_filename);

            return svm;
        }

        /// <summary>
        /// Predict Label for sample
        /// </summary>
        /// <param name="sample">sample</param>
        /// <returns>label</returns>
        public double Predict(double[] sample)
        {
            if (__model_ptr == IntPtr.Zero) throw new Exception("model neither loaded nor trained");

            IntPtr svm_nodes_ptr = sample_to_svm_nodes_ptr(sample);

            double ret = svm_predict(__model_ptr, svm_nodes_ptr);

            Free_ptr(ref svm_nodes_ptr);

            return ret;
        }

        /// <summary>
        /// Free memmory
        /// </summary>
        public void Dispose()
        {
            if (__problem_ptr != IntPtr.Zero) Free_svm_problem_ptr(ref __problem_ptr);
            if (__parameter_ptr != IntPtr.Zero) Free_svm_parameter_ptr(ref __parameter_ptr);
            if (__model_ptr != IntPtr.Zero) svm_free_and_destroy_model(ref __model_ptr);
        }
    }
}
