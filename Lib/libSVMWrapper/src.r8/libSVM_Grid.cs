/*
 * GNU LESSER GENERAL PUBLIC LICENSE
 * Version 3, 29 June 2007
 * 
 * Copyright 2011 Rafał "R@V" Prasał <rafal.prasal@gmail.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Package for wrapper for libsvm.dll
/// </summary>
namespace libSVMWrapper
{
    /// <summary>
    /// Grid for parameters tobe used in TrainAuto();
    /// @code
    ///         val = grid.min; 
    ///         do 
    ///         { 
    ///             ... 
    ///             val*=grid.step; 
    ///         
    ///         } while(val<grid.max);
    /// @endcode
    /// </summary>
    /// Usage examples
    /// 
    ///     When you do not know the grid you may use default
    /// 
    /// @code
    ///         libSVM_Grid grid_c = libSVM_Grid.C();
    ///         ...
    /// @endcode
    /// 
    ///     or when you know what grid you want to use
    ///     
    /// @code
    ///         libSVM_Grid grid_c = new libSVM_Grid();
    ///     
    ///         c.min = 0.1;
    ///         c.max = 1.0;
    ///         c.step = 10;
    /// @endcode
    /// 

    public class libSVM_Grid
    {
        /// <summary>
        /// lower limit
        /// </summary>
        public double min
        {
            get;
            set;
        }

        /// <summary>
        /// upper limit
        /// </summary>
        public double max
        {
            get;
            set;
        }
        
        /// <summary>
        /// next_val *= step; so step must be >1;
        /// </summary>
        public double step
        {
            get;
            set;
        }

        public libSVM_Grid()
        {
            min=1.0;
            max=1.1;
            step=1.1;
        }

        /// <summary>
        /// defauld grid for c
        /// </summary>
        /// <returns>defauld grid for c</returns>
        public static libSVM_Grid C()
        {
            libSVM_Grid grid = new libSVM_Grid();

            grid.min = 0.1;
            grid.max = 500;
            grid.step = 5;

            return grid;
        }

        /// <summary>
        /// defauld grid for gamma
        /// </summary>
        /// <returns>defauld grid for gamma</returns>
        public static libSVM_Grid gamma()
        {
            libSVM_Grid grid = new libSVM_Grid();

            grid.min = 1e-5;
            grid.max = 0.6;
            grid.step = 15;

            return grid;
        }


        /// <summary>
        /// defauld grid for p
        /// </summary>
        /// <returns>defauld grid for p</returns>  
        public static libSVM_Grid p()
        {
            libSVM_Grid grid = new libSVM_Grid();
            grid.min = 0.001;
            grid.max = 100;
            grid.step = 7;

            return grid;
        }

        /// <summary>
        /// defauld grid for nu
        /// </summary>
        /// <returns>defauld grid for nu</returns>  
        public static libSVM_Grid nu()
        {
            libSVM_Grid grid = new libSVM_Grid();

            grid.min = 0.01;
            grid.max = 0.2;
            grid.step = 3;

            return grid;
        }


        /// <summary>
        /// default grid for coef
        /// </summary>
        /// <returns>defauld grid for coef</returns>  
        public static libSVM_Grid coef()
        {
            libSVM_Grid grid = new libSVM_Grid();

            grid.min = 0.1;
            grid.max = 300;
            grid.step = 14;

            return grid;
        }

        /// <summary>
        /// default grid for degree
        /// </summary>
        /// <returns>defauld grid for degree</returns>
        public static libSVM_Grid degree()
        {
            libSVM_Grid grid = new libSVM_Grid();

            grid.min = 0.01;
            grid.max = 4;
            grid.step = 7;

            return grid;
        }

    }
}
